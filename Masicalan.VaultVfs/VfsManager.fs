namespace Masicalan.VaultVfs

open System
open System.IO
open System.IO.Compression
open System.Text
open System.Security.Cryptography
open System.Runtime.InteropServices
open System.Xml.Linq

module VfsManager =

    // Create an in-memory ZIP containing the manifest and an empty scripts/ directory
    let private createZip (manifestBytes: byte[]) =
        use ms = new MemoryStream()
        use zip = new ZipArchive(ms, ZipArchiveMode.Create, true)
        // add manifest
        let m = zip.CreateEntry("manifest.xml", CompressionLevel.Optimal)
        use entryStream = m.Open()
        entryStream.Write(manifestBytes, 0, manifestBytes.Length)

        // add an empty scripts directory entry (name ends with '/')
        zip.CreateEntry("scripts/") |> ignore
        // no content for directory

        zip.Dispose()
        ms.Position <- 0L
        ms.ToArray()

    // Encrypt bytes using DPAPI (CurrentUser). We include a fixed optional
    // entropy so the protection is slightly bound to this application.
    let private protect (plain: byte[]) (entropy: byte[]) =
        ProtectedData.Protect(plain, entropy, DataProtectionScope.CurrentUser)

    // Build minimal manifest XML
    let private buildManifest () =
        let vault =
            XElement(XName.Get("Vault"),
                XAttribute(XName.Get("Name"), "Masicalan Vault VFS"),
                XAttribute(XName.Get("Version"), "1"),
                XAttribute(XName.Get("CreatedTime"), DateTime.Now.ToString("o")) )

        // container elements for future entries
        vault.Add(XElement(XName.Get("ScriptsDirectory")))
        vault.Add(XElement(XName.Get("Files")))
        let doc = XDocument(vault)
        use ms = new MemoryStream()
        doc.Save(ms)
        ms.ToArray()

    /// Create an empty encrypted vault file (.masiv) at the given path.
    /// The produced file contains a ZIP archive (in-memory) with a minimal
    /// manifest.xml and an empty scripts/ directory. The ZIP bytes are
    /// encrypted using DPAPI (CurrentUser) and written with a small
    /// header so the file can be recognized.
    let Create (outputPath: string) (entropyName: string) : string =
        if String.IsNullOrWhiteSpace outputPath then
            invalidArg "outputPath" "outputPath must be a non-empty path."

        let outPath =
            if Path.GetExtension outputPath = ".masiv" then outputPath
            else Path.ChangeExtension(outputPath, ".masiv")

        // Build everything and write to disk with a small header
        let zipBytes = buildManifest() |> createZip
        let encrypted = 
            entropyName |> Encoding.ASCII.GetBytes |> protect zipBytes

        // File layout: ASCII "MASIV" (5 bytes) + version byte (1) + encrypted payload
        let header = Encoding.ASCII.GetBytes(VfsConstants.HeaderMagic)
        let version = [| byte 1 |]

        use outFs = new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.None)
        outFs.Write(header, 0, header.Length)
        outFs.Write(version, 0, version.Length)
        outFs.Write(encrypted, 0, encrypted.Length)

        outFs.Flush()
        outPath

    /// Create an empty encrypted vault file (.masiv) but use default entropy at the given path.
    /// The produced file contains a ZIP archive (in-memory) with a minimal
    /// manifest.xml and an empty scripts/ directory. The ZIP bytes are
    /// encrypted using DPAPI (CurrentUser) and written with a small
    /// header so the file can be recognized.
    let CreateSimple (outputPath: string) =
        Create outputPath VfsConstants.DefaultEntropyName
