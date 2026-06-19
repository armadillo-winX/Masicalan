namespace Masicalan.VaultVfs

open System.Text

module VfsConstans =
    // Constants used for MASIV file format and DPAPI
    let internal HeaderMagic = "MASIV"
    let internal VersionByte = byte 1
    let internal Entropy = Encoding.UTF8.GetBytes("Masicalan.VaultVfs:entropy:v1")
