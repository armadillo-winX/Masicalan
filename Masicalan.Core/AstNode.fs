namespace Masicalan.Core

// AST(抽象構文木) のノードの定義

// Operator : 演算子
type Operator = Add | Sub | Mul | Div | Pow | LessThan | GreaterThan | EqualTo

// Expression : 式: 評価(evaluate)することで値になる
type Expression =
    | Num of int                                            // number literal
    | Var of string                                         // variable reference
    | Binary of Expression * Operator * Expression          // binary operation

// Statement : 文: 実行することで環境を変化させる(値は返さない)
type Statement = 
    | Let of string * Expression                          // define variable <- immutable変数 と mutable変数 を分けたい
    | Assign of string * Expression                       // assign value
    | While of Expression * Statement                     // while loop
    | If of Expression * Statement * Statement option     // if-else
    | Block of Statement list                             // block
    | Print of Expression                                 // print
