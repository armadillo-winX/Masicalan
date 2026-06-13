namespace Masicalan.Core

module PrimitiveBuiltins =

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
        ("printn"), (["s"], Statement.CallNativeF (printFunc, [Var "s"]))
        ]