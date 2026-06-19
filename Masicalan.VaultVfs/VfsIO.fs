namespace Masicalan.VaultVfs

open System
open System.IO
open System.IO.Compression
open System.Text
open System.Security.Cryptography
open System.Xml.Linq

module VfsIO =

    // Constants used for MASIV file format and DPAPI
    let private HeaderMagic = "MASIV"
    let private VersionByte = byte 1
    let private Entropy = Encoding.UTF8.GetBytes("Masicalan.VaultVfs:entropy:v1")

    let private ensureExtension (path:string) =
        if Path.GetExtension path = ".masiv" then path else Path.ChangeExtension(path, ".masiv")

    let private readEncryptedPayload (vaultPath:string) : byte[] =
        use fs = File.Open(vaultPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        let headerBytes = Array.zeroCreate<byte>(HeaderMagic.Length)
        let read = fs.Read(headerBytes, 0, headerBytes.Length)
        if read <> headerBytes.Length then invalidOp "Not a valid MASIV file (short header)."
        let headerStr = Encoding.ASCII.GetString(headerBytes)
        if headerStr <> HeaderMagic then invalidOp "Not a valid MASIV file (magic mismatch)."

        let ver = fs.ReadByte()
        if ver = -1 then invalidOp "Not a valid MASIV file (missing version)."
        // remaining bytes are payload
        let remaining = int (fs.Length - fs.Position)
        let payload = Array.zeroCreate<byte>(remaining)
        let r = fs.Read(payload, 0, remaining)
        if r <> remaining then invalidOp "Failed to read encrypted payload."
        payload

    let private decryptPayload (encrypted:byte[]) : byte[] =
        ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser)

    let private encryptPayload (plain:byte[]) : byte[] =
        ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser)

    let private sha256hex (data:byte[]) : string =
        use sha = SHA256.Create()
        let hash = sha.ComputeHash(data)
        let sb = StringBuilder()
        for b in hash do sb.AppendFormat("{0:x2}", b) |> ignore
        sb.ToString()

    /// Add a script file into an existing .masiv vault.
    /// vaultPath: path to .masiv file
    /// directory: directory inside scripts/ where the file will be placed (single name or nested using '/').
    /// fileName: name of the script file (should include .masis extension if desired)
    /// content: script content as string
    /// attribute: one of "ReadOnly", "Editable", "Executable"
    /// Returns the vaultPath on success.
    let Add (vaultPath:string) (directory:string) (fileName:string) (content:string) (attribute:string) : string =
        if String.IsNullOrWhiteSpace vaultPath then invalidArg "vaultPath" "vaultPath must be provided"
        if String.IsNullOrWhiteSpace fileName then invalidArg "fileName" "fileName must be provided"
        if String.IsNullOrWhiteSpace content then invalidArg "content" "content must be provided"

        let attr =
            match attribute with
            | "ReadOnly" | "Editable" | "Executable" -> attribute
            | _ -> invalidArg "attribute" "attribute must be one of: ReadOnly, Editable, Executable"

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
            let doc = XDocument(XElement(XName.Get("Vault"), XAttribute(XName.Get("name"), "Masicalan Vault VFS"), XAttribute(XName.Get("version"), "1"), XAttribute(XName.Get("createdUtc"), DateTime.UtcNow.ToString("o"))))
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
        let header = Encoding.ASCII.GetBytes(HeaderMagic)
        outFs.Write(header, 0, header.Length)
        outFs.Write([| VersionByte |], 0, 1)
        outFs.Write(newEncrypted, 0, newEncrypted.Length)
        outFs.Flush()

        vault
