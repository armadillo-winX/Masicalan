namespace Masicalan.VaultVfs

/// VFS file attribute enum used by VfsIO operations
type VfsAttribute =
    | ReadOnly = 0
    | Editable = 1
    | Executable = 2
