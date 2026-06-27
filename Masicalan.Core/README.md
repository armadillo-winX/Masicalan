# Masicalan Core

Masicalan Core は，マクロ記述スクリプト言語 Masicalan のインタプリタライブラリです．

## 開発環境

Microsoft Windows 11 Insider Preview Experimental<br>
Visual Studio 2026<br>
.NET 10.0

## 概要

Masicalan は動的型付けのスクリプト言語で，以下のような機能を備えています：

- **値型**: 整数 (IntVal)，小数 (FloatVal)，文字列 (StringVal)，真偽値 (BoolVal)，配列 (ArrayVal)
- **演算子**: 算術演算 (+, -, *, /, ^)，比較演算 (<, >, ==)，論理演算 (&&, ||)
- **制御構文**: 変数定義，代入，if-else，while ループ，関数定義，戻り値

## 基本的な使い方

### Interpreter.Run - シンプルな実行

最もシンプルな使い方は `Interpreter.Run` を使用することです：

```fsharp
let script = "printn(\"Hello World!\");"
let env = script |> Interpreter.Run
```

`Interpreter.Run` はスクリプトをパースして実行し，実行後の環境状態を `Evaluator.EnvironmentState` 型で返します．

#### EnvironmentState の構造

返り値の `EnvironmentState` には以下の情報が含まれています：

```fsharp
type EnvironmentState = {
	VariablesEnv : Map<string, Value>                    // 変数環境
	FunctionsEnv : Map<string, string list * Statement>  // 関数環境
}
```

スクリプト実行後の変数や関数に以下のようにアクセスできます：

```fsharp
let env = script |> Interpreter.Run

// 変数の参照
let maybeValue = env.VariablesEnv.TryFind("variableName")

// 関数の参照
let maybeFunction = env.FunctionsEnv.TryFind("functionName")
```

### Interpreter.RunWithExt - 拡張機能を使った実行

初期変数や初期関数をインジェクションしてスクリプトを実行する場合は，`Interpreter.RunWithExt` を使用します：

```fsharp
let script = "let result = add(10, 20);"

// 初期変数を定義
let initialVars = Map.ofList [("productName", StringVal "MyProduct")]

// 初期関数を定義
let initialFuncs = Map.ofList [("add", (["a"; "b"], CallNativeF(...)))]

let env = Interpreter.RunWithExt script initialVars initialFuncs
```

このような，初期変数と初期関数をインジェクションする仕組みを **Masicalan Extension** と呼びます．
外部データの利用やアプリケーションが実装した関数をインジェクションすることで，
マクロ言語としての拡張性を提供します．

## 使用しているライブラリ

### FParsec

パーサの実装に [FParsec](https://github.com/stephan-tolksdorf/fparsec) を利用しています．

**Copyright** (c) 2007–2017, Stephan Tolksdorf. All rights reserved.<br>
**ライセンス**: BSD 2-clause License<br>
[FParsec のライセンス詳細](https://www.quanttec.com/fparsec/license.html)
