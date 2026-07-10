namespace Masicalan.Core

open System
open FParsec

module MetadataParser =

    // ASCII 文字判定（ラップしておく）
    let private isAsciiLetter c = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')

    // メタキーのパーサ（英字とアンダースコアを許可）
    let private parseKey : Parser<string, unit> =
        many1Satisfy (fun c -> isAsciiLetter c || c = '_')

    // 値はダブルクォートで囲まれた任意の文字列（エスケープ未対応）
    let private parseQuotedValue : Parser<string, unit> =
        between (pchar '"') (pchar '"') (manyChars (noneOf "\""))

    // 行単位のメタパーサ：先頭に任意の空白があっても良いが、@が現れた行のみを対象とする
    // 例: @Name="MyScript"
    let private metaLineParser : Parser<string * string, unit> =
        skipMany (anyOf [' '; '\t']) >>. pchar '@' >>. parseKey .>> spaces .>> pchar '=' .>> spaces .>>. parseQuotedValue .>> skipMany (anyOf [' '; '\t']) .>> eof

    // スクリプト全体からメタ情報を収集して Metadata レコードを返す。
    // 見つからないフィールドは空文字列になる。
    let ParseMetadata (scriptText: string) : Metadata =
        let lines = scriptText.Split([|"\r\n"; "\n"; "\r"|], StringSplitOptions.None)

        let pairs =
            lines
            |> Array.choose (fun line ->
                match run metaLineParser line with
                | Success((k, v), _, _) -> Some(k, v)
                | Failure(_, _, _) -> None)
            |> Map.ofArray

        let get k = Map.tryFind k pairs |> Option.defaultValue ""

        {
            Name = get "Name"
            Version = get "Version"
            Author = get "Author"
            Copyright = get "Copyright"
            Description = get "Description"
        }
