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
        | (BoolVal b1, BoolVal b2) ->
            failwithf "cannot add bool values"
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
        | (BoolVal b1, BoolVal b2) ->
            failwithf "cannot subtract bool values"
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
        | (BoolVal b1, BoolVal b2) ->
            failwithf "cannot multiple bool values"
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
        | (BoolVal b1, BoolVal b2) ->
            failwithf "cannot divide bool values"
        | (VoidVal, VoidVal) -> 
            failwithf "cannnot divide void values"
        |_ -> failwithf "cannot divide %A and %A because these types are different." v1 v2

    let powVal (v1: Value) (v2: Value) =
        match (v1, v2) with
        | (IntVal i1, IntVal i2) ->
            pown i1 i2 |> IntVal 
        | (FloatVal f1, FloatVal f2) ->
            FloatVal (f1 ** f2)
        | (StringVal s1, StringVal s2) ->
            failwithf "cannot raise \"%s\" to power of \"%s\" because they are string values." s1 s2
        | (BoolVal b1, BoolVal b2) ->
            failwithf "cannot raise %b to power of %b because they are bool values." b1 b2
        | (VoidVal, VoidVal) -> 
            failwithf "cannnot do exponentiation caluculation for void values"
        |_ -> failwithf "cannot raise %A to power of %A because these types are different." v1 v2

    let isLeftLesserThanRight (l: Value) (r: Value) =
        match (l, r) with
        | (IntVal li, IntVal ri) ->
            BoolVal (li < ri)
        | (FloatVal lf, FloatVal rf) ->
            BoolVal (lf < rf)
        | (StringVal s1, StringVal s2) ->
            failwithf "cannnot compare string values"
        | (BoolVal b1, BoolVal b2) ->
            failwithf "cannot compare bool values"
        | (VoidVal, VoidVal) ->
            failwithf "cannot compare void values"
        |_-> failwithf "cannot different type values"

    let isLeftGreaterThenRight (l: Value) (r: Value) =
        match (l, r) with
        | (IntVal li, IntVal ri) ->
            BoolVal (li > ri)
        | (FloatVal lf, FloatVal rf) ->
            BoolVal (lf > rf)
        | (StringVal s1, StringVal s2) ->
            failwithf "cannnot compare string values"
        | (BoolVal b1, BoolVal b2) ->
            failwithf "cannot compare bool values"
        | (VoidVal, VoidVal) ->
            failwithf "cannot compare void values"
        |_-> failwithf "cannot different type values"

    let isLeftEqualToRight (l: Value) (r:Value) =
        match (l, r) with
        | (IntVal li, IntVal ri) ->
            BoolVal (li = ri)
        | (FloatVal lf, FloatVal rf) ->
            BoolVal (lf = rf)
        | (StringVal s1, StringVal s2) ->
            failwithf "cannnot compare string values"
        | (BoolVal b1, BoolVal b2) ->
            failwithf "cannot compare bool values"
        | (VoidVal, VoidVal) ->
            failwithf "cannot compare void values"
        |_-> failwithf "cannot different type values"

    let rec translateToDebugStr (v:Value) =
        match v with
        | Value.IntVal i -> $"Int[{i}]"
        | Value.FloatVal f -> $"Float[{f}]"
        | Value.StringVal s -> $"String[{s}]"
        | Value.BoolVal b -> $"Bool[{b}]"
        | Value.ArrayVal a ->
            let arrayElements = List.map translateToDebugStr a |> String.concat ", "
            $"Array[{arrayElements}]"
        | Value.VoidVal -> "Void"
