namespace Masicalan.Core

module EvaluatorHelper =
    
    let addVal (v1: Value) (v2:Value) =
        match (v1, v2) with
        | (IntVal i1, IntVal i2) ->
            IntVal (i1 + i2)
        | (FloatVal f1, FloatVal f2) ->
            FloatVal (f1 + f2)
        | (StringVal s1, StringVal s2) ->
            StringVal (s1 + s2)
        | (VoidVal, VoidVal) -> 
            failwithf "cannnot add void values."
        |_ -> failwithf "cannot add %A and %A because these types are different." v1 v2

    let subVal (v1: Value) (v2: Value) =
        match (v1, v2) with
        | (IntVal i1, IntVal i2) ->
            IntVal (i1 - i2)
        | (FloatVal f1, FloatVal f2) ->
            FloatVal (f1 - f2)
        | (StringVal s1, StringVal s2) ->
            failwithf "cannot subtract string values."
        | (VoidVal, VoidVal) -> 
            failwithf "cannnot subtract void values"
        |_ -> failwithf "cannot subtract %A and %A because these types are different." v1 v2

    let mulVal (v1: Value) (v2: Value) =
        match (v1, v2) with
        | (IntVal i1, IntVal i2) ->
            IntVal (i1 * i2)
        | (FloatVal f1, FloatVal f2) ->
            FloatVal (f1 * f2)
        | (StringVal s1, StringVal s2) ->
            failwithf "cannot multiple string values."
        | (VoidVal, VoidVal) -> 
            failwithf "cannnot multiple void values"
        |_ -> failwithf "cannot multiple %A and %A because these types are different." v1 v2

    let divVal (v1: Value) (v2: Value) =
        match (v1, v2) with
        | (IntVal i1, IntVal i2) ->
            IntVal (i1 / i2)
        | (FloatVal f1, FloatVal f2) ->
            FloatVal (f1 / f2)
        | (StringVal s1, StringVal s2) ->
            failwithf "cannot divide string values."
        | (VoidVal, VoidVal) -> 
            failwithf "cannnot divide void values"
        |_ -> failwithf "cannot divide %A and %A because these types are different." v1 v2