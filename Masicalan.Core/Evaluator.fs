namespace Masicalan.Core

// 評価器

module Evaluator =
    type EnvironmentState = {
        VariablesEnv : Map<string, int>                      // 変数環境
        FunctionsEnv : Map<string, string list * Statement>  // 関数環境
    }

    // 文(Statement)の実行結果
    type EvalResult = {
        Environment: EnvironmentState
        ReturnValue: int option
    }

    // 式(Expression) を評価する -> 値を返す
    let rec evaluateExpression (env: Map<string, int>) (expr: Expression) =
        match expr with
        | Num n -> n
        | Var name ->
            match env.TryFind(name) with
            | Some v -> v
            | None -> name |> failwithf "undefined value: %s"
        | Binary (left, op, right) ->
            let l = evaluateExpression env left
            let r = evaluateExpression env right
            match op with
            | Add -> l + r
            | Sub -> l - r
            | Mul -> l * r
            | Div -> l / r
            | Pow -> pown l r
            // true -> return 1 ; false -> return 0
            | LessThan -> if l < r then 1 else 0 
            | GreaterThan -> if l > r then 1 else 0
            | EqualTo -> if l = r then 1 else 0
        
    // 文(Statement) を実行する -> env(環境) を更新する
    let rec executeStatement (env: Map<string, int>) (stmt: Statement) =
        match stmt with
        | Let (name, expr) ->
            let value = evaluateExpression env expr
            env.Add(name, value)
        | Assign (name, expr) ->
            if env.ContainsKey(name) = false then
                name |> failwithf "cannot assign a value to undefined variable '%s'"
            let value = evaluateExpression env expr
            env.Add(name, value)
        | Print expr ->
            let value = evaluateExpression env expr
            printfn "%d" value
            env
        | Block stmts ->
            // スコープ制御
            // ブロック内の文を，環境を引き継ぎ順番に実行
            let innerEnv = List.fold (fun currentEnv s -> executeStatement currentEnv s) env stmts

            // ブロック内で追加定義されたローカル変数は破棄
            // 親環境の変数は値を反映
            env |> Map.map (fun key _ -> if innerEnv.ContainsKey(key) then innerEnv.[key] else env.[key])
        | If (condition, thenstmt, elsestmt) ->
            let conditionV = evaluateExpression env condition
            if conditionV <> 0 then
                executeStatement env thenstmt
            else
                match elsestmt with
                | Some eStmt -> executeStatement env eStmt
                | None -> env
        | While (condition, body) ->
            // 条件が true である限り，再帰的に実行し環境を更新
            let rec loop currentEnv =
                let conditionV = evaluateExpression currentEnv condition
                if conditionV <> 0 then
                    let newEnv = executeStatement currentEnv body
                    loop newEnv
                else
                    currentEnv

            loop env
