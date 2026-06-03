open FParsec
open Masicalan.Core


let printAst script =
    match run Parser.parseProgram script with
        | Success(ast, _, _) ->
            printfn "%A" ast
        | Failure(error, _, _) ->
            printfn "failed: %s" error |>ignore

let runInterpreter script =
    Interpreter.Run script |> ignore

let runInterprAndPrintEnv script =
    let env = Interpreter.Run script
    printfn "%A" env
