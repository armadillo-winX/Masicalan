namespace Masicalan.Core

module PrimitiveBuiltins =

    // toString(a)
    let rec private toStringFunc (args: Value list) =
        match args with
        |[Value.StringVal s] ->
            s
        |[Value.IntVal i] ->
            $"{i}"
        |[Value.FloatVal f] ->
            $"{f}"
        |[Value.BoolVal b] ->
            $"{b}"
        |[Value.ArrayVal a]->
            let elements = List.map toStringFunc [a] |> String.concat ", "
            $"[{elements}]"
        |[Value.VoidVal] ->
            " "
        |_ -> failwithf "toString function cannot recieive multiple arguments."

    // getLength(a)
    let private getLengthFunc (args: Value list) =
        match args with
        |[Value.StringVal s] ->
            IntVal s.Length
        |[Value.ArrayVal a] ->
            a |> List.length |> IntVal
        |_->
            failwithf "\"Length\" is defined only for strings or arrays."
    
    // printn(s)
    let private printFunc (args: Value list) =
        match args with
        |[Value.StringVal s] ->
            printfn "%s" s |> ignore
            Value.VoidVal
        |_-> 
            failwithf "Cannot print non-string value"

    let create () =
        Map[
        ("toString"), (["a"], Statement.CallNativeF (toStringFunc, [Var "a"]))
        ("getLength"), (["a"], Statement.CallNativeF (getLengthFunc, [Var "a"]))
        ("printn"), (["s"], Statement.CallNativeF (printFunc, [Var "s"]))
        ]