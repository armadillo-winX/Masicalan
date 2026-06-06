namespace Masicalan.Core

// AST(抽象構文木) のノードの定義

// Type: 型
type Type = IntType | FloatType | StringType | VoidType

// Value : 値
type Value =
    | IntVal of int
    | FloatVal of float
    | StringVal of string
    | VoidVal

// Operator : 演算子
type Operator = Add | Sub | Mul | Div | Pow | LessThan | GreaterThan | EqualTo | LogAnd | LogOr

// Expression : 式: 評価(evaluate)することで値になる
type Expression =
    | Num of int                                            // number literal
    | Var of string                                         // variable reference
    | Binary of Expression * Operator * Expression          // binary operation
    | CallF of string * Expression list                     // call function

// Statement : 文: 実行することで環境を変化させる(値は返さない)
type Statement = 
    | Let of string * Expression                          // define variable
    | Assign of string * Expression                       // assign value
    | While of Expression * Statement                     // while loop
    | If of Expression * Statement * Statement option     // if-else
    | Block of Statement list                             // block
    | Print of Expression                                 // print
    | Function of string * string list * Statement        // function
    | Return of Expression                                // return
