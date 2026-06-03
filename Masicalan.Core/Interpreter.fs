namespace Masicalan.Core

open FParsec

module Interpreter =
    
    let Run (script: string) =
        match run Parser.parseProgram script with
        | Success(ast, _, _) ->
            List.fold Evaluator.executeStatement Map.empty ast
        | Failure(error, _, _) ->
            failwithf "failed: %s" error
