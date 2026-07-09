# Masicalan
Masicalan は，マクロ記述などを目的とした，F# + .NET で動くミニマルな構成のスクリプト言語です．

## 開発環境
Microsoft Windows 11 Insider Preview Experimental<br>
Visual Studio 2026<br>
.NET 10.0

## 言語仕様
Masicalan では，C++やC#がそうであるように，
文末にはセミコロン```;```を必要とし，
また，ブロックは波括弧```{``` ```}```
で**必ず**閉じていなければなりません．
### 変数
変数の宣言には```let```キーワードまたは```mut```キーワードを用います．
```let```キーワードで定義された変数は不変，```mut```キーワードで定義された変数は可変です．
また，可変な変数への再代入には演算子```<-```を用います．
```
// 不変な変数xの定義
let x = 10;

// 可変な変数yの定義
mut y = 20;

// xへの再代入
y <- 30;

// 次のような不変な変数への代入はエラーになります．
x <- 40;
```
### 関数
関数の定義には```fun```キーワードを用います．
```
fun add(x, y) {
    return x + y;
}

// 関数の呼び出し
let z = add(3, 2);
```

関数は，既に定義済みの関数と同名のものを定義しようとすると，
元々の関数を上書きするようになっています．<br>
値を返さない関数で早期に関数から抜ける場合は，`exit`を用います．
```
fun printMessage(option) {
    if (option == false) { 
        exit; 
    }

    printn("Hello World");
}
```

### 基本的な関数
Masicalan には
標準入出力および配列に関するいくつかの基本的な関数が
ビルトインで利用できるようになっています．
```
// printn は文字を出力する関数です．
printn("Hello World");

// readLine は入力を受け付ける関数です．
let n = readLine();

// getLength は配列および文字列の長さを取得します．
let message = "Hello World";
let length = getLength(message);

// toString はあらゆる値を文字列に変換します．
let pi = toString(3.14);
```
### 条件分岐
Masicalan ではif-elseによる条件分岐が可能です．
ただし現時点(ver0.1.0-preview.0)では```if```と```else```しか実装されておらず，
他の言語における```else if```や```elif```
に相当するものは実装されていません．
```
fun putMessage(i){
    if(i == 1) {
        printn("Hello World!");
    }else{
        printn("こんにちは世界!");
    }
}
```

### ループ
Masicalan では```while```キーワードでのループが可能です．
```
let i = 0;
while (i < 10) {
    i <- i + 1;
}
printn(toString(i));
```

## 使用しているライブラリ
パーサの実装には [FParsec](https://github.com/stephan-tolksdorf/fparsec) を利用しています．<br>
### FParsec
Copyright (c) 2007‒2017, Stephan Tolksdorf. All rights reserved.<br>
BSD 2-clause License が適用<br>
[FParsec のライセンス](https://www.quanttec.com/fparsec/license.html)
