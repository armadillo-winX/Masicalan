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

    let RunWithExt (script: string) (extEnv: Map<string, (string list * Statement)>) =
        match run Parser.parseProgram script with
        | Success(ast, _, _) ->
            let primEnv = PrimitiveBuiltins.create()
            let initFuncEnv = Map.fold (fun acc key value -> Map.add key value acc) primEnv extEnv
            let initEnv : Evaluator.EnvironmentState = { VariablesEnv = Map.empty; FunctionsEnv = initFuncEnv }
            List.fold
                (fun env stmt ->
                    let evalRes = Evaluator.executeStatement env stmt
                    evalRes.Environment) initEnv ast
        | Failure(error, _, _) ->
            failwithf "failed: %s" error
