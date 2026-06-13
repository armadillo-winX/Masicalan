namespace Masicalan.Core

// 評価器

module Evaluator =
    type EnvironmentState = {
        VariablesEnv : Map<string, Value>                      // 変数環境
        FunctionsEnv : Map<string, string list * Statement>  // 関数環境
    }

    // 文(Statement)の実行結果
    type EvalResult = {
        Environment: EnvironmentState
        ReturnValue: Value option
    }

    // 式(Expression) を評価する -> 値を返す
    let rec evaluateExpression (env: EnvironmentState) (expr: Expression) =
        match expr with
        | ValueLit n -> n
        | ArrayLit a -> 
            let values = List.map (evaluateExpression env) a
            Value.ArrayVal values
        | Var name ->
            match env.VariablesEnv.TryFind(name) with
            | Some v -> v
            | None -> name |> failwithf "undefined value: %s"
        | Binary (left, op, right) ->
            let l = evaluateExpression env left
            let r = evaluateExpression env right
            match op with
            | Add -> EvaluatorHelper.addVal l r
            | Sub -> EvaluatorHelper.subVal l r
            | Mul -> EvaluatorHelper.mulVal l r
            | Div -> EvaluatorHelper.divVal l r
            | Pow -> EvaluatorHelper.powVal l r
            | LessThan -> EvaluatorHelper.isLeftLesserThanRight l r
            | GreaterThan -> EvaluatorHelper.isLeftGreaterThenRight l r
            | EqualTo -> EvaluatorHelper.isLeftEqualToRight l r
            | LogAnd ->
                // 短絡評価
                if evaluateExpression env left = BoolVal true && evaluateExpression env right = BoolVal true then
                    BoolVal true
                else
                    BoolVal false
            | LogOr ->
                // 短絡評価
                if evaluateExpression env left = BoolVal true || evaluateExpression env right = BoolVal true then
                    BoolVal true
                else
                    BoolVal false
        | CallF (funcName, args) ->
            match env.FunctionsEnv.TryFind(funcName) with
            | Some (paramsList, stmts) -> 
                if List.length paramsList < List.length args then
                    failwithf "arguments are too many"
                if List.length paramsList > List.length args then
                    failwithf "arguments are only a few"
                
                // 引数の評価
                let argValues = List.map (evaluateExpression env) args

                // スコープの作成(関数ブロックローカル環境)
                // 初期変数環境は，引数のみ，関数は元の環境を引き継ぐ
                let localVars = List.zip paramsList argValues |> Map.ofList
                let localEnvironment = { VariablesEnv = localVars; FunctionsEnv = env.FunctionsEnv }

                let result = executeStatement localEnvironment stmts

                match result.ReturnValue with
                | Some v -> v
                | None -> VoidVal
                
            | None -> funcName |> failwithf "undefined function: %s"
        | AccessArrIndex (arrExpr, indexExpr) ->
            let arrVal = evaluateExpression env arrExpr
            let indexV = evaluateExpression env indexExpr
            match arrVal, indexV with
            | (Value.ArrayVal a, Value.IntVal i) ->
                if i < 0 || i >= List.length a then
                    failwithf "Index out of ranges: %d" i
                a.[i]
            | (Value.ArrayVal a , _) ->
                failwithf "Array index must be an Integer."
            | (_, _) ->
                failwithf "Cannot apply indexing to non-array."
        
    // 文(Statement) を実行する -> env(環境) を更新する
    and executeStatement (env: EnvironmentState) (stmt: Statement) =
        match stmt with
        | Return expr ->
            let value = evaluateExpression env expr

            {
                Environment = env
                ReturnValue = Some value
            }
        | Let (name, expr) ->
            let value = evaluateExpression env expr
            let newVarEnvs = env.VariablesEnv.Add(name, value)

            { 
                Environment = { VariablesEnv = newVarEnvs ; FunctionsEnv = env.FunctionsEnv}
                ReturnValue = None
            }
        | Assign (name, expr) ->
            if env.VariablesEnv.ContainsKey(name) = false then
                name |> failwithf "cannot assign a value to undefined variable '%s'"
            let value = evaluateExpression env expr
            let newVarEnv = env.VariablesEnv.Add(name, value)
            
            {
                Environment = { VariablesEnv = newVarEnv; FunctionsEnv = env.FunctionsEnv }
                ReturnValue = None
            }
        | Inspect expr ->
            evaluateExpression env expr 
            |> EvaluatorHelper.translateToDebugStr 
            |> printfn "%s" |> ignore

            { Environment = env; ReturnValue = None }
        | Function (funcName, paramsList, stmts) ->
            let newFuncEnv = env.FunctionsEnv.Add(funcName, (paramsList, stmts))
            
            {
                Environment = { VariablesEnv = env.VariablesEnv; FunctionsEnv = newFuncEnv }
                ReturnValue = None
            }
        | Block stmts ->
            // ブロック内の文を，環境を引き継ぎ順番に実行
            let rec runInnerBlock currentEnv stmtlist =
                match stmtlist with
                | [] -> { Environment = currentEnv; ReturnValue = None}
                | s :: rest ->
                    let eRes = executeStatement currentEnv s
                    match eRes.ReturnValue with
                    | Some v -> eRes
                    | None -> runInnerBlock eRes.Environment rest

            let innerResult = runInnerBlock env stmts

            // ブロック内で追加定義されたローカル変数は破棄
            // 親環境の変数は値を反映
            let newVarEnv 
                = env.VariablesEnv |> Map.map (fun key _ -> if innerResult.Environment.VariablesEnv.ContainsKey(key) then innerResult.Environment.VariablesEnv.[key] else env.VariablesEnv.[key])
            { 
                // FunctionsEnv は元の環境(env)のままにして関数をスコープに閉じ込める
                Environment = { VariablesEnv = newVarEnv ; FunctionsEnv = env.FunctionsEnv}
                ReturnValue = innerResult.ReturnValue
            }
        | If (condition, thenstmt, elsestmt) ->
            let conditionV = evaluateExpression env condition
            if conditionV = BoolVal true then
                executeStatement env thenstmt
            else
                match elsestmt with
                | Some eStmt -> executeStatement env eStmt
                | None -> { Environment = env; ReturnValue = None}
        | While (condition, body) ->
            // 条件が true である限り，再帰的に実行し環境を更新
            let rec loop currentEnv =
                let conditionV = evaluateExpression currentEnv condition
                if conditionV = BoolVal true then
                    let evalRes = executeStatement currentEnv body
                    match evalRes.ReturnValue with
                    | Some rv -> { Environment = evalRes.Environment ; ReturnValue = evalRes.ReturnValue }
                    | None -> loop evalRes.Environment
                else
                    { Environment = currentEnv ; ReturnValue = None }

            loop env
        | CallFNotReturn (funcName, args) ->
            evaluateExpression env (CallF(funcName, args)) |> ignore
            { Environment = env; ReturnValue = None}
        | CallNativeF (f, args) ->
            let argValues = List.map (evaluateExpression env) args
            let result = f argValues
            { Environment = env; ReturnValue = result |> Option.ofObj }
