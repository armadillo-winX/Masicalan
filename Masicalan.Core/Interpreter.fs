namespace Masicalan.Core

open FParsec

module Interpreter =
    
    let Run (script: string) =
        match run Parser.parseProgram script with
        | Success(ast, _, _) ->
            let emptyEnv : Evaluator.EnvironmentState = { VariablesEnv = Map.empty; FunctionsEnv = PrimitiveBuiltins.create()}
            List.fold 
                (fun env stmt ->
                    let evalRes = Evaluator.executeStatement env stmt
                    evalRes.Environment) emptyEnv ast
        | Failure(error, _, _) ->
            failwithf "failed: %s" error
