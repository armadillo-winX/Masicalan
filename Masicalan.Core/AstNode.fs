namespace Masicalan.Core

// AST(抽象構文木) のノードの定義

// Type: 型 インタプリタが実行時に動的型推論をするため，今のところ不要
//type Type = IntType | FloatType | StringType | VoidType

// Value : 値
type Value =
    | IntVal of int
    | FloatVal of float
    | StringVal of string
    | BoolVal of bool
    | ArrayVal of Value list
    | VoidVal

// Operator : 演算子
type Operator = Add | Sub | Mul | Div | Pow | LessThan | GreaterThan | EqualTo | LogAnd | LogOr

// Expression : 式: 評価(evaluate)することで値になる
type Expression =
    | ValueLit of Value                                     // value literal
    | ArrayLit of Expression list                           // array literal
    | Var of string                                         // variable reference
    | Binary of Expression * Operator * Expression          // binary operation
    | CallF of string * Expression list                     // call function
    | AccessArrIndex of Expression * Expression             // access array index

// Statement : 文: 実行することで環境を変化させる(値は返さない)
type Statement = 
    | Let of string * Expression                          // define variable
    | Assign of string * Expression                       // assign value
    | While of Expression * Statement                     // while loop
    | If of Expression * Statement * Statement option     // if-else
    | Block of Statement list                             // block
    | Inspect of Expression                               // print for debug
    | Function of string * string list * Statement        // function
    | Return of Expression                                // return
    | CallFNotReturn of string * Expression list          // call function without return
