namespace Masicalan.VaultVfs

open System
open System.IO
open System.IO.Compression
open System.Text
open System.Security.Cryptography
open System.Xml.Linq

module VfsIO =

    let private ensureExtension (path:string) =
        if Path.GetExtension path = ".masiv" then path else Path.ChangeExtension(path, ".masiv")

    let private readEncryptedPayload (vaultPath:string) : byte[] =
        use fs = File.Open(vaultPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        let headerBytes = Array.zeroCreate<byte>(VfsConstants.HeaderMagic.Length)
        let read = fs.Read(headerBytes, 0, headerBytes.Length)
        if read <> headerBytes.Length then invalidOp "Not a valid MASIV file (short header)."
        let headerStr = Encoding.ASCII.GetString(headerBytes)
        if headerStr <> VfsConstants.HeaderMagic then invalidOp "Not a valid MASIV file (magic mismatch)."

        let ver = fs.ReadByte()
        if ver = -1 then invalidOp "Not a valid MASIV file (missing version)."
        // remaining bytes are payload
        let remaining = int (fs.Length - fs.Position)
        let payload = Array.zeroCreate<byte>(remaining)
        let r = fs.Read(payload, 0, remaining)
        if r <> remaining then invalidOp "Failed to read encrypted payload."
        payload

    let private decryptPayload (encrypted:byte[]) : byte[] =
        ProtectedData.Unprotect(encrypted, VfsConstants.DefaultEntropy, DataProtectionScope.CurrentUser)

    let private encryptPayload (plain:byte[]) : byte[] =
        ProtectedData.Protect(plain, VfsConstants.DefaultEntropy, DataProtectionScope.CurrentUser)

    let private sha256hex (data:byte[]) : string =
        use sha = SHA256.Create()
        let hash = sha.ComputeHash(data)
        let sb = StringBuilder()
        for b in hash do sb.AppendFormat("{0:x2}", b) |> ignore
        sb.ToString()

    let private tryFindFileElement (doc:XDocument) (normalized:string) : XElement option =
        if isNull doc || isNull doc.Root then None
        else
            let filesEl = doc.Root.Element(XName.Get("Files"))
            if isNull filesEl then None
            else
                filesEl.Elements(XName.Get("File"))
                |> Seq.tryFind (fun e ->
                    let p = (e.Attribute(XName.Get("path")) |> fun a -> if isNull a then null else a.Value)
                    let n = (e.Attribute(XName.Get("name")) |> fun a -> if isNull a then null else a.Value)
                    (not (isNull p) && String.Equals(p, normalized, StringComparison.OrdinalIgnoreCase)) ||
                    (not (isNull n) && String.Equals(n, Path.GetFileName(normalized), StringComparison.OrdinalIgnoreCase)) )

    let private vfsAttributeToString (a: VfsAttribute) : string =
        match a with
        | VfsAttribute.ReadOnly -> "ReadOnly"
        | VfsAttribute.Editable -> "Editable"
        | VfsAttribute.Executable -> "Executable"
        | _ -> invalidOp "Unknown VfsAttribute"

    /// Add a script file into an existing .masiv vault.
    /// vaultPath: path to .masiv file
    /// directory: directory inside scripts/ where the file will be placed (single name or nested using '/').
    /// fileName: name of the script file (should include .masis extension if desired)
    /// content: script content as string
    /// attribute: one of "ReadOnly", "Editable", "Executable"
    /// Returns the vaultPath on success.
    let Add (vaultPath:string) (directory:string) (fileName:string) (content:string) (attribute:VfsAttribute) : string =
        if String.IsNullOrWhiteSpace vaultPath then invalidArg "vaultPath" "vaultPath must be provided"
        if String.IsNullOrWhiteSpace fileName then invalidArg "fileName" "fileName must be provided"
        if String.IsNullOrWhiteSpace content then invalidArg "content" "content must be provided"

        let attr = attribute |> vfsAttributeToString

        let vault = ensureExtension vaultPath

        // Read and decrypt existing vault
        let encrypted = readEncryptedPayload vault
        let zipBytes = decryptPayload encrypted

        // Prepare content bytes and hash
        let contentBytes = Encoding.UTF8.GetBytes(content)
        let hash = sha256hex contentBytes

        // Entry path inside zip
        let dirNormalized =
            if String.IsNullOrEmpty(directory) then String.Empty
            else directory.Replace("\\", "/").Trim('/')

        let entryDirPath = if String.IsNullOrEmpty(dirNormalized) then "scripts/" else sprintf "scripts/%s/" dirNormalized
        let entryFilePath = entryDirPath + fileName

        // Open the zip in update mode and modify
        use ms = new MemoryStream(zipBytes)
        use zip = new ZipArchive(ms, ZipArchiveMode.Update, true)

        // ensure scripts/ and target dir entries exist
        let ensureEntryName (name:string) =
            if zip.GetEntry(name) = null then zip.CreateEntry(name) |> ignore

        ensureEntryName("scripts/")
        if not (String.IsNullOrEmpty dirNormalized) then ensureEntryName(entryDirPath)

        // create or replace the script file entry
        let existing = zip.GetEntry(entryFilePath)
        if not (isNull existing) then existing.Delete()
        let scriptEntry = zip.CreateEntry(entryFilePath, CompressionLevel.Optimal)
        use se = scriptEntry.Open()
        se.Write(contentBytes, 0, contentBytes.Length)

        // update manifest.xml
        let manifestEntry = zip.GetEntry("manifest.xml")
        if isNull manifestEntry then
            // create minimal manifest if missing
            let doc = VfsManager.buildManifestMetaInfo |> XDocument
            doc.Root.Add(XElement(XName.Get("ScriptsDirectory")))
            doc.Root.Add(XElement(XName.Get("Files")))
            let me = zip.CreateEntry("manifest.xml")
            use mstream = me.Open()
            doc.Save(mstream)
        else
            // update existing manifest by replacing it
            use readStream = manifestEntry.Open()
            let doc = XDocument.Load(readStream)
            // remove old manifest entry so we can recreate
            manifestEntry.Delete()
            // ensure Files element exists
            let root = doc.Root
            if root.Element(XName.Get("Files")) = null then root.Add(XElement(XName.Get("Files")))
            let filesEl = root.Element(XName.Get("Files"))
            // add new file element
            let fileEl = XElement(XName.Get("File"), XAttribute(XName.Get("name"), fileName), XAttribute(XName.Get("path"), entryFilePath), XAttribute(XName.Get("hash"), hash), XAttribute(XName.Get("attribute"), attr))
            filesEl.Add(fileEl)
            // recreate manifest entry
            let me2 = zip.CreateEntry("manifest.xml", CompressionLevel.Optimal)
            use ms2 = me2.Open()
            doc.Save(ms2)

        // finalize zip bytes
        zip.Dispose()
        let finalZip =
            ms.Position <- 0L
            ms.ToArray()

        // encrypt and write back
        let newEncrypted = encryptPayload finalZip
        use outFs = new FileStream(vault, FileMode.Create, FileAccess.Write, FileShare.None)
        let header = Encoding.ASCII.GetBytes(VfsConstants.HeaderMagic)
        outFs.Write(header, 0, header.Length)
        outFs.Write([| VfsConstants.VersionByte |], 0, 1)
        outFs.Write(newEncrypted, 0, newEncrypted.Length)
        outFs.Flush()

        vault

    /// Set the attribute for a script file in the vault.
    /// entryPath rules same as Read/Edit. newAttr is the VfsAttribute enum.
    let SetAttribute (vaultPath:string) (entryPath:string) (newAttr: VfsAttribute) : string =
        if String.IsNullOrWhiteSpace vaultPath then invalidArg "vaultPath" "vaultPath must be provided"
        if String.IsNullOrWhiteSpace entryPath then invalidArg "entryPath" "entryPath must be provided"

        let vault = ensureExtension vaultPath
        let encrypted = readEncryptedPayload vault
        let zipBytes = decryptPayload encrypted

        let normalized =
            let p = entryPath.Replace("\\", "/").TrimStart('/')
            if p.StartsWith("scripts/", StringComparison.OrdinalIgnoreCase) then p else sprintf "scripts/%s" p

        let attrStr = vfsAttributeToString newAttr

        use ms = new MemoryStream(zipBytes)
        use zip = new ZipArchive(ms, ZipArchiveMode.Update, true)

        let manifestEntry = zip.GetEntry("manifest.xml")
        if isNull manifestEntry then invalidOp "manifest.xml missing in vault"

        use manifestStream = manifestEntry.Open()
        let doc = XDocument.Load(manifestStream)
        // remove old manifest entry so we can recreate
        manifestEntry.Delete()

        let filesEl =
            if doc.Root = null then invalidOp "Invalid manifest.xml: missing root"
            else
                let fe = doc.Root.Element(XName.Get("Files"))
                if isNull fe then
                    let f = XElement(XName.Get("Files"))
                    doc.Root.Add(f)
                    f
                else fe

        let fileElOpt =
            filesEl.Elements(XName.Get("File"))
            |> Seq.tryFind (fun e ->
                let p = (e.Attribute(XName.Get("path")) |> fun a -> if isNull a then null else a.Value)
                let n = (e.Attribute(XName.Get("name")) |> fun a -> if isNull a then null else a.Value)
                (not (isNull p) && String.Equals(p, normalized, StringComparison.OrdinalIgnoreCase)) ||
                (not (isNull n) && String.Equals(n, Path.GetFileName(normalized), StringComparison.OrdinalIgnoreCase)) )

        match fileElOpt with
        | None -> invalidOp "File entry not found in manifest.xml"
        | Some fe ->
            fe.SetAttributeValue(XName.Get("attribute"), attrStr)

            // recreate manifest
            let me2 = zip.CreateEntry("manifest.xml", CompressionLevel.Optimal)
            use ms2 = me2.Open()
            doc.Save(ms2)

        // finalize and write back
        zip.Dispose()
        let finalZip =
            ms.Position <- 0L
            ms.ToArray()

        let newEncrypted = encryptPayload finalZip
        use outFs = new FileStream(vault, FileMode.Create, FileAccess.Write, FileShare.None)
        let header = Encoding.ASCII.GetBytes(VfsConstants.HeaderMagic)
        outFs.Write(header, 0, header.Length)
        outFs.Write([| VfsConstants.VersionByte |], 0, 1)
        outFs.Write(newEncrypted, 0, newEncrypted.Length)
        outFs.Flush()

        vault

    /// List script files stored under scripts/ in the vault.
    /// Returns an array of paths relative to the scripts/ directory (e.g. "subdir/script.masis").
    let GetScriptFiles (vaultPath:string) : string[] =
        if String.IsNullOrWhiteSpace vaultPath then invalidArg "vaultPath" "vaultPath must be provided"

        let vault = ensureExtension vaultPath
        let encrypted = readEncryptedPayload vault
        let zipBytes = decryptPayload encrypted

        use ms = new MemoryStream(zipBytes)
        use zip = new ZipArchive(ms, ZipArchiveMode.Read, false)

        zip.Entries
        |> Seq.filter (fun e ->
            // skip directories and non-scripts entries
            not (e.FullName.EndsWith("/")) && e.FullName.StartsWith("scripts/", StringComparison.OrdinalIgnoreCase))
        |> Seq.map (fun e ->
            let p = e.FullName
            if p.StartsWith("scripts/", StringComparison.OrdinalIgnoreCase) then p.Substring("scripts/".Length) else p)
        |> Seq.toArray

    /// Delete a script from the vault by its internal path.
    /// entryPath rules same as Read/Edit.
    let Delete (vaultPath:string) (entryPath:string) : string =
        if String.IsNullOrWhiteSpace vaultPath then invalidArg "vaultPath" "vaultPath must be provided"
        if String.IsNullOrWhiteSpace entryPath then invalidArg "entryPath" "entryPath must be provided"

        let vault = ensureExtension vaultPath
        let encrypted = readEncryptedPayload vault
        let zipBytes = decryptPayload encrypted

        let normalized =
            let p = entryPath.Replace("\\", "/").TrimStart('/')
            if p.StartsWith("scripts/", StringComparison.OrdinalIgnoreCase) then p else sprintf "scripts/%s" p

        use ms = new MemoryStream(zipBytes)
        use zip = new ZipArchive(ms, ZipArchiveMode.Update, true)

        let entry = zip.GetEntry(normalized)
        if isNull entry then invalidOp (sprintf "Entry '%s' not found in vault." normalized)

        // load or create manifest
        let manifestEntry = zip.GetEntry("manifest.xml")
        let doc =
            if isNull manifestEntry then
                let d = VfsManager.buildManifestMetaInfo |> XDocument
                d.Root.Add(XElement(XName.Get("ScriptsDirectory")))
                let files = XElement(XName.Get("Files"))
                d.Root.Add(files)
                d
            else
                use msr = manifestEntry.Open()
                let d = XDocument.Load(msr)
                // remove old manifest entry so we can recreate later
                manifestEntry.Delete()
                d

        // ensure Files element
        let filesEl =
            if doc.Root = null then invalidOp "Invalid manifest.xml: missing root"
            else
                let fe = doc.Root.Element(XName.Get("Files"))
                if isNull fe then
                    let f = XElement(XName.Get("Files"))
                    doc.Root.Add(f)
                    f
                else fe

        // find matching file element
        let fileElOpt =
            filesEl.Elements(XName.Get("File"))
            |> Seq.tryFind (fun e ->
                let p = (e.Attribute(XName.Get("path")) |> fun a -> if isNull a then null else a.Value)
                let n = (e.Attribute(XName.Get("name")) |> fun a -> if isNull a then null else a.Value)
                (not (isNull p) && String.Equals(p, normalized, StringComparison.OrdinalIgnoreCase)) ||
                (not (isNull n) && String.Equals(n, Path.GetFileName(normalized), StringComparison.OrdinalIgnoreCase)) )

        match fileElOpt with
        | Some fe ->
            // remove from manifest
            fe.Remove()
        | None -> ()

        // delete the entry
        entry.Delete()

        // recreate manifest entry
        let me2 = zip.CreateEntry("manifest.xml", CompressionLevel.Optimal)
        use ms2 = me2.Open()
        doc.Save(ms2)

        // finalize and write back
        zip.Dispose()
        let finalZip =
            ms.Position <- 0L
            ms.ToArray()

        let newEncrypted = encryptPayload finalZip
        use outFs = new FileStream(vault, FileMode.Create, FileAccess.Write, FileShare.None)
        let header = Encoding.ASCII.GetBytes(VfsConstants.HeaderMagic)
        outFs.Write(header, 0, header.Length)
        outFs.Write([| VfsConstants.VersionByte |], 0, 1)
        outFs.Write(newEncrypted, 0, newEncrypted.Length)
        outFs.Flush()

        vault

    /// Read a script from the vault by its internal path.
    /// entryPath is a path relative to the scripts/ directory, e.g. "subdir/script.masis" or
    /// it may already include the leading "scripts/" prefix.
    let Read (vaultPath:string) (entryPath:string) : string =
        if String.IsNullOrWhiteSpace vaultPath then invalidArg "vaultPath" "vaultPath must be provided"
        if String.IsNullOrWhiteSpace entryPath then invalidArg "entryPath" "entryPath must be provided"

        let vault = ensureExtension vaultPath
        let encrypted = readEncryptedPayload vault
        let zipBytes = decryptPayload encrypted

        let normalized =
            let p = entryPath.Replace("\\", "/").TrimStart('/')
            if p.StartsWith("scripts/", StringComparison.OrdinalIgnoreCase) then p else sprintf "scripts/%s" p

        use ms = new MemoryStream(zipBytes)
        use zip = new ZipArchive(ms, ZipArchiveMode.Read, false)
        let entry = zip.GetEntry(normalized)
        if isNull entry then invalidOp (sprintf "Entry '%s' not found in vault." normalized)

        use s = entry.Open()
        use out = new MemoryStream()
        s.CopyTo(out)
        let bytes = out.ToArray()
        // integrity check against manifest if present
        let manifestEntry = zip.GetEntry("manifest.xml")
        if not (isNull manifestEntry) then
            use msr = manifestEntry.Open()
            let doc = XDocument.Load(msr)
            match tryFindFileElement doc normalized with
            | Some fe ->
                let hAttr = fe.Attribute(XName.Get("hash"))
                if not (isNull hAttr) then
                    let expected = hAttr.Value
                    let actual = sha256hex bytes
                    if not (String.Equals(expected, actual, StringComparison.OrdinalIgnoreCase)) then
                        invalidOp (sprintf "Integrity check failed for '%s': expected hash %s but found %s" normalized expected actual)
            | None -> 
                invalidOp "File entry not found in manifest.xml"
        else
            invalidOp "manifest.xml missing in vault"
        Encoding.UTF8.GetString(bytes)

    /// Edit an existing script inside the vault. entryPath uses same rules as Read.
    /// Will update the file contents and refresh the hash stored in manifest.xml.
    /// Editing is denied when the manifest records attribute="ReadOnly" for the file.
    let Edit (vaultPath:string) (entryPath:string) (newContent:string) : string =
        if String.IsNullOrWhiteSpace vaultPath then invalidArg "vaultPath" "vaultPath must be provided"
        if String.IsNullOrWhiteSpace entryPath then invalidArg "entryPath" "entryPath must be provided"
        if isNull newContent then invalidArg "newContent" "newContent must be provided"

        let vault = ensureExtension vaultPath
        let encrypted = readEncryptedPayload vault
        let zipBytes = decryptPayload encrypted

        let normalized =
            let p = entryPath.Replace("\\", "/").TrimStart('/')
            if p.StartsWith("scripts/", StringComparison.OrdinalIgnoreCase) then p else sprintf "scripts/%s" p

        let contentBytes = Encoding.UTF8.GetBytes(newContent)
        let newHash = sha256hex contentBytes

        use ms = new MemoryStream(zipBytes)
        use zip = new ZipArchive(ms, ZipArchiveMode.Update, true)

        let entry = zip.GetEntry(normalized)
        if isNull entry then invalidOp (sprintf "Entry '%s' not found in vault." normalized)

        // find manifest and corresponding file element
        let manifestEntry = zip.GetEntry("manifest.xml")
        if isNull manifestEntry then invalidOp "manifest.xml missing in vault"

        use manifestStream = manifestEntry.Open()
        let doc = XDocument.Load(manifestStream)
        // remove old manifest entry so it can be recreated
        manifestEntry.Delete()

        let filesEl =
            if doc.Root = null then invalidOp "Invalid manifest.xml: missing root"
            else
                let fe = doc.Root.Element(XName.Get("Files"))
                if isNull fe then
                    let f = XElement(XName.Get("Files"))
                    doc.Root.Add(f)
                    f
                else fe
        // find the file element and perform integrity check before editing
        match tryFindFileElement doc normalized with
        | None -> invalidOp "File entry not found in manifest.xml"
        | Some fe ->
            // compute current entry hash
            use curStream = entry.Open()
            use curMs = new MemoryStream()
            curStream.CopyTo(curMs)
            let curBytes = curMs.ToArray()
            let curHash = sha256hex curBytes
            let hAttr = fe.Attribute(XName.Get("hash"))
            if not (isNull hAttr) then
                let expected = hAttr.Value
                if not (String.Equals(expected, curHash, StringComparison.OrdinalIgnoreCase)) then
                    invalidOp (sprintf "Integrity check failed for '%s': expected hash %s but found %s" normalized expected curHash)

            let attr = fe.Attribute(XName.Get("attribute"))
            let attrVal = if isNull attr then String.Empty else attr.Value
            if String.Equals(attrVal, "ReadOnly", StringComparison.OrdinalIgnoreCase) then invalidOp "Cannot edit read-only file"

            // replace entry contents
            entry.Delete()
            let newEntry = zip.CreateEntry(normalized, CompressionLevel.Optimal)
            use ne = newEntry.Open()
            ne.Write(contentBytes, 0, contentBytes.Length)

            // update hash in manifest and recreate manifest entry
            fe.SetAttributeValue(XName.Get("hash"), newHash)
            let me2 = zip.CreateEntry("manifest.xml", CompressionLevel.Optimal)
            use ms2 = me2.Open()
            doc.Save(ms2)

        // finalize zip and write back
        zip.Dispose()
        let finalZip =
            ms.Position <- 0L
            ms.ToArray()

        let newEncrypted = encryptPayload finalZip
        use outFs = new FileStream(vault, FileMode.Create, FileAccess.Write, FileShare.None)
        let header = Encoding.ASCII.GetBytes(VfsConstants.HeaderMagic)
        outFs.Write(header, 0, header.Length)
        outFs.Write([| VfsConstants.VersionByte |], 0, 1)
        outFs.Write(newEncrypted, 0, newEncrypted.Length)
        outFs.Flush()

        vault
