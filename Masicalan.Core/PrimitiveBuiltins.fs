namespace Masicalan.Core

module PrimitiveBuiltins =
    
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