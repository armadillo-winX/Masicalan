namespace Masicalan.Core

open FParsec

module Interpreter =
    
    let Run (script: string) =
        match run Parser.parseProgram script with
        | Success(ast, _, _) ->
            let emptyEnv : Evaluator.EnvironmentState = { VariablesEnv = Map.empty; FunctionsEnv = PrimitiveExtension.create()}
            List.fold 
                (fun env stmt ->
                    let evalRes = Evaluator.executeStatement env stmt
                    evalRes.Environment) emptyEnv ast
        | Failure(error, _, _) ->
            failwithf "failed: %s" error

    let RunWithExt (script: string) (extVarEnv: Map<string, Value>) (extFunEnv: Map<string, (string list * Statement)>) =
        match run Parser.parseProgram script with
        | Success(ast, _, _) ->
            let primEnv = PrimitiveExtension.create()
            let initFuncEnv = Map.fold (fun acc key value -> Map.add key value acc) primEnv extFunEnv
            let initEnv : Evaluator.EnvironmentState = { VariablesEnv = extVarEnv; FunctionsEnv = initFuncEnv }
            List.fold
                (fun env stmt ->
                    let evalRes = Evaluator.executeStatement env stmt
                    evalRes.Environment) initEnv ast
        | Failure(error, _, _) ->
            failwithf "failed: %s" error
