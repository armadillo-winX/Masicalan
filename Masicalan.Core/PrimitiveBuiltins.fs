namespace Masicalan.Core

module PrimitiveBuiltins =
    
    let printFunc (args: Value list) =
        match args with
        |[Value.StringVal s] ->
            printfn "%s" s |> ignore
            Value.VoidVal
        |_-> 
            failwithf "Cannot print non-string value"