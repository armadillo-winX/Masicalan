namespace Masicalan.VaultVfs

open System.Text

module VfsConstants =
    // Constants used for MASIV file format and DPAPI
    let internal HeaderMagic = "MASIV"
    let internal VersionByte = byte 1
    let internal DefaultEntropy = Encoding.UTF8.GetBytes("Masicalan.VaultVfs:entropy:v1")
