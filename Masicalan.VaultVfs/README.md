# Masicalan Vault Virtual File System

Masicalan Vault Virtual File System (MVVFS) は，Masicalan スクリプトファイル (.masis) を暗号化して安全に管理するための仮想ファイルシステムです．

## イントロダクション

**Masicalan Vault VFS** は，
.NET アプリケーション内にマクロ記述言語 Masicalan を組み込む際の課題である「不正なスクリプト挿入」を防ぐことを目的としています．
ZIP アーカイブ形式と DPAPI を組み合わせることで，擬似的な暗号化ファイルシステムを実現しています．
これによりユーザーが外部のテキストエディタでスクリプトを編集したり，
外部のプログラムが安易にスクリプトを編集，挿入したりすることを防ぎます．

## 仕様

### ファイル形式

Masicalan Vault VFS は **.masiv** ファイルに格納されます．

### 内部構造

.masivファイルは ZIP 形式で圧縮され，DPAPI (Data Protection API) で暗号化されています．内部には以下の構成が含まれます：

```
masiv-archive.masiv
├── manifest.xml          (メタデータ)
├── scripts/              (ルートディレクトリ)
│   ├── sample1.masis
│   └── subdir/
│       └── sample2.masis
```

- **manifest.xml**: メタデータを格納します．
- **scripts/**: スクリプトファイルのルートディレクトリです．
- ファイルパスは相対パス (例: `sample.masis`, `subdir/sample.masis`) 
または絶対パス (例: `scripts/sample.masis`, `scripts/subdir/sample.masis`) で管理します．

### ファイル属性

各スクリプトファイルに権限属性を付与できます：
- **ReadOnly**: ファイルの読み取りのみ
- **Editable**: ファイルの編集を許可
- **Executable**: ファイルの実行を許可

**⚠️ 注意：** これらの属性はあくまでファイルに関連付けられた情報であり，Editable と Executable が付与されたファイルに対する実際の挙動は，
VFS を利用するアプリケーション開発者が独自に実装する必要があります．

### 整合性検証

VFS は追加・編集時に SHA256 ハッシュを計算して記録します．ファイル読み取り時に記録されたハッシュとの比較を行い，改ざんされた場合は例外をスローします．

### 暗号化方式

暗号化方式は Windows の DPAPI を使用しています．
スコープは`DataProtectionScope.CurrentUser`であり，
任意のエントロピーを指定可能です．
エントロピー値によって復号化キーが変わるため，異なるエントロピーで暗号化された MASIV ファイルは復号化できません．

## 基本的な使い方

### VfsManager - Vault の作成と管理

```fsharp
open Masicalan.VaultVfs

// 新しい Vault を作成
let entropyName = "MyApplication.Vault"
let vaultPath = VfsManager.Create("scripts.masiv", entropyName)

// Vault を ZIP に変換
VfsManager.ConvertToZip("scripts.masiv", "export.zip", entropyName)
```

### VfsIO - ファイルの読み書き

#### ファイルを追加

```fsharp
let scriptContent = "printn(\"Hello from Vault!\");"

VfsIO.Add("scripts.masiv",         // Vault ファイルパス
	  "MyApplication.Vault",   // エントロピー
	  "samples",               // ディレクトリ (相対パス, 空でも可)
	  "hello.masis",           // ファイル名
	  scriptContent,           // スクリプト内容
	  VfsAttribute.Executable) // 属性
```

#### ファイルを読み取り

```fsharp
// スクリプトファイル一覧を取得
let files = VfsIO.GetScriptFiles("scripts.masiv", "MyApplication.Vault")
for file in files do
	printfn "File: %s" file

// 特定のファイルを読み取り
let content = VfsIO.Read("scripts.masiv", "MyApplication.Vault", "samples/hello.masis")
```

#### ファイルを編集

```fsharp
let newContent = "printn(\"Updated!\");"

VfsIO.Edit("scripts.masiv",
	   "MyApplication.Vault",
	   "samples/hello.masis",
	   newContent,
	   VfsAttribute.Executable)
```
ReadOnly 属性の付与されたファイルに対して編集を試みると例外をスローします．

#### ファイルを削除

```fsharp
VfsIO.Delete("scripts.masiv", "MyApplication.Vault", "samples/hello.masis")
```

#### ファイル属性を変更

```fsharp
VfsIO.SetFileAttribute("scripts.masiv", "MyApplication.Vault", "samples/hello.masis", VfsAttribute.ReadOnly)
```

## 開発環境

- Microsoft Windows 11 Insider Preview Experimental
- Visual Studio 2026
- .NET SDK 10.0

## 重要な注意点

### セキュリティに関する注意

Masicalan Vault VFS は，**ファイル I/O によるスクリプトの不正な改ざんをできる限り防ぐ**ことを目指した設計であり，
あらゆる脅威に対する完全な保護を目的としたものではありません．

以下の点にご注意ください：

1. **DPAPI の制限**<br>
   DPAPI は Windows の機能であるため，**Masicalan Vault VFS は Windows でのみ動作します**．
   Linux や macOS では利用できません．

2. **Windows のユーザー情報への依存**<br>
   VFS はログイン中のユーザー固有の情報に基づいて暗号化されています．
   **ユーザーアカウントを新規作成した場合や，別のユーザーでログインした場合は VFS を開くことができません**．
   ドメイン管理者によるパスワードリセットなど，**Windows のユーザー情報が破損した場合，VFS の復号化が不可能になる可能性があります**．

### 推奨事項

ユーザーの利便性と安全性のために，必要であれば以下の機能を実装することを推奨します：

- スクリプトファイルのバックアップ機能
- スクリプトファイルの VFS からのエクスポート機能

Masicalan Vault VFS のセキュリティ上の問題が懸念される場合は，MVVFS を使用するのではなく，独自のスクリプトファイル管理機構を実装することを推奨します．

## ライセンス

BSD 2-Clause License が適用されます．
詳しくはリポジトリのルートディレクトリにある LICENSE.txt を参照してください．
