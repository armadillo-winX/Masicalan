namespace Masicalan.Core

open System

module PrimitiveExtension =

    let rec private toStringExecute (arg: Value) =
        match arg with
        | Value.StringVal s ->
            s
        | Value.IntVal i ->
            $"{i}"
        | Value.FloatVal f ->
            $"{f}"
        | Value.BoolVal b ->
            $"{b}"
        | Value.ArrayVal a->
            let elements = List.map toStringExecute a |> String.concat ", "
            $"[{elements}]"
        | Value.VoidVal ->
            " "
    
    // toString(a)
    let private toStringFunc (args: Value list) =
        if List.length args = 1 then
            toStringExecute args.[0] |> Value.StringVal
        elif List.length args = 0 then
            failwithf "toString function need an argument."
        else
            failwithf "toString function cannot recieve multiple arguments."

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

    // readLine()
    let private readLineFunc (args: Value list) =
        Console.ReadLine() |> Value.StringVal

    let create () =
        Map[
        ("toString"), (["a"], Statement.CallNativeF (toStringFunc, [Var "a"]))
        ("getLength"), (["a"], Statement.CallNativeF (getLengthFunc, [Var "a"]))
        ("printn"), (["s"], Statement.CallNativeF (printFunc, [Var "s"]))
        ("readLine"), ([], Statement.CallNativeF (readLineFunc, []))
        ]