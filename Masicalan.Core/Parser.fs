namespace Masicalan.Core

open FParsec

module Parser =
    
    // 空白を飛ばすパーサ
    let wspace = skipMany (anyOf [' '; '\t'])

    // 整数値リテラルパーサ
    let parseIntLiteral : Parser<Expression, unit> = 
        pint32 |>> Value.IntVal |>> Expression.ValueLit

    // float リテラルパーサ
    let parseFloatLiteral : Parser<Expression, unit> =
        pfloat |>> Value.FloatVal |>> Expression.ValueLit

    // string リテラルパーサ
    let parseStringLiteral : Parser<Expression, unit> =
        between (pstring "\"") (pstring "\"") (manyChars (noneOf "\""))
        |>> Value.StringVal |>> Expression.ValueLit

    // アンダースコアかどうかを判定
    let isUnderScore c = if c = '_' then true else false

    // アンダースコアかアスキーか判定
    let isAsciiOrUnderSc c = isAsciiLetter c || isUnderScore c

    // char に対して文字か数字かを判定
    let isAsciiOrDigit c = isAsciiLetter c || isDigit c

    // アスキーか数字かアンダースコアか判定
    let isAsciiOrDigitOrUnderSc c = isAsciiOrDigit c || isUnderScore c

    // 変数名パーサ
    let parseIdentText : Parser<string, unit> =
        many1Satisfy2 isAsciiOrUnderSc isAsciiOrDigitOrUnderSc

    // 変数パーサ
    let parseVarible : Parser<Expression, unit> = parseIdentText |>> Expression.Var

    // 優先順位付き演算パーサの初期化
    let operPrecParser = OperatorPrecedenceParser<Expression, unit, unit>()
    let parseExpression = operPrecParser.ExpressionParser

    // 引数リストのパーサ
    let parseArgs : Parser<Expression list, unit> =
        sepBy (parseExpression .>> wspace) (pstring "," .>> wspace)

    // 関数呼び出しパーサ
    let parseCallF : Parser<Expression, unit> =
        parseIdentText .>> wspace .>>. (between (pstring "(" .>> wspace) (pstring ")" .>> wspace) parseArgs)
        |>> fun (funcName, args) -> Expression.CallF(funcName, args)

    // かっこでくくられた式 or リテラル or 変数 のパース
    let parseTerm = choice [
        attempt parseFloatLiteral
        attempt parseIntLiteral
        attempt parseStringLiteral
        attempt parseCallF
        parseVarible
        between (pstring "(" .>> wspace) (pstring ")" .>> wspace) parseExpression
    ]

    operPrecParser.TermParser <- parseTerm .>> wspace


    // 算術，比較演算子の登録
    operPrecParser.AddOperator(InfixOperator("||", wspace, 1, Associativity.Left, fun l r -> Binary(l, LogOr, r)))
    operPrecParser.AddOperator(InfixOperator("&&", wspace, 2, Associativity.Left, fun l r -> Binary(l, LogAnd, r)))
    operPrecParser.AddOperator(InfixOperator("<",  wspace, 3, Associativity.None, fun l r -> Binary(l, LessThan, r)))
    operPrecParser.AddOperator(InfixOperator(">",  wspace, 3, Associativity.None, fun l r -> Binary(l, GreaterThan, r)))
    operPrecParser.AddOperator(InfixOperator("==", wspace, 3, Associativity.None, fun l r -> Binary(l, EqualTo, r)))
    operPrecParser.AddOperator(InfixOperator("+", wspace, 4, Associativity.Left, fun l r -> Binary(l, Add, r)))
    operPrecParser.AddOperator(InfixOperator("-", wspace, 4, Associativity.Left, fun l r -> Binary(l, Sub, r)))
    operPrecParser.AddOperator(InfixOperator("*", wspace, 5, Associativity.Left, fun l r -> Binary(l, Mul, r)))
    operPrecParser.AddOperator(InfixOperator("/", wspace, 5, Associativity.Left, fun l r -> Binary(l, Div, r)))
    operPrecParser.AddOperator(InfixOperator("**", wspace, 6, Associativity.Left, fun l r -> Binary(l, Pow, r)))
    
    // 末尾セミコロンパーサ
    let parseSemicolon : Parser<unit, unit> = spaces .>> pstring ";"

    // let 文のパーサ
    let parseLet : Parser<Statement, unit> =
        pstring "let" >>. spaces1 >>. parseIdentText .>> wspace .>> pstring "=" .>> wspace .>>. parseExpression 
        .>> parseSemicolon
        |>> fun (varName, expr) -> Statement.Let(varName, expr)

    // 再代入 <- のパーサ
    let parseAssign : Parser<Statement, unit> =
        parseIdentText .>> wspace .>> pstring "<-" .>> wspace .>>. parseExpression
        .>> parseSemicolon
        |>> fun (varName, expr) -> Statement.Assign(varName, expr)

    // print 文のパーサ
    let parsePrint : Parser<Statement, unit> =
        pstring "print" >>. wspace >>. parseExpression
        .>> parseSemicolon
        |>> fun expr -> Statement.Print(expr)

    // return 文パーサ
    let parseReturn : Parser<Statement, unit> =
        pstring "return" >>. spaces1 >>. parseExpression .>> parseSemicolon
        |>> Statement.Return

    // 改行・空白行パーサ
    let parseLineEnd : Parser<unit, unit> =
        skipMany (anyOf ['\r'; '\n'])

    let parseStatement, parseStatementRef = createParserForwardedToRef<Statement, unit>()

    // 波括弧ブロックのパーサ
    let parseBlock: Parser<Statement, unit> =
        let blockContent = spaces >>. many (parseStatement .>> spaces)
        between (pstring "{" .>> spaces) (pstring "}" .>> spaces) blockContent
        |>> Statement.Block

    // if 文パーサ
    let parseIf: Parser<Statement, unit> =
        let thenBranch = pstring "then" >>. spaces >>. parseBlock
        let elseBranch = opt (pstring "else" >>. spaces >>. parseBlock)

        tuple3
            (pstring "if" >>. spaces1 >>. parseExpression)
            thenBranch
            elseBranch
        |>> fun (condition, thenStatement, elseStatement) -> Statement.If (condition, thenStatement, elseStatement)

    // while 文パーサ
    let parseWhile: Parser<Statement, unit> =
        tuple2
            (pstring "while" >>. spaces1 >>. parseExpression)
            (pstring "do" >>. spaces >>. parseBlock)
        |>> fun (condition, stmts) -> Statement.While (condition, stmts)
        
    // 引数リストのパーサ (関数定義)
    let parseParams : Parser<string list, unit> =
        sepBy (parseIdentText .>> spaces) (pstring "," .>> spaces)
    
    // 関数定義文パーサ
    let parseFunction : Parser<Statement, unit> =
        let fHead = pstring "fun" >>. spaces1 >>. parseIdentText .>> spaces
        let fParams = between (pstring "(" .>> spaces) (pstring ")" .>> spaces) parseParams

        tuple3 fHead fParams (spaces >>. parseBlock)
        |>> fun (funcName, paramsList, stmts) -> Statement.Function (funcName, paramsList, stmts)

    // 返り値代入なし関数呼び出し文パーサ
    let parseCallFNotReturn : Parser<Statement, unit> =
        parseIdentText .>> wspace .>>. (between (pstring "(" .>> wspace) (pstring ")" .>> wspace) parseArgs) .>> parseSemicolon
        |>> fun (funcName, args) -> Statement.CallFNotReturn(funcName, args)

    // すべての文を統合するパーサ
    parseStatementRef.Value <-
        choice [
            attempt parseFunction
            attempt parseReturn
            attempt parseLet
            attempt parseAssign
            attempt parsePrint
            attempt parseIf
            attempt parseWhile
            attempt parseBlock
            attempt parseCallFNotReturn
        ]

    // プログラム全体のパーサ
    let parseProgram : Parser<Statement list, unit> =
        spaces >>. sepEndBy parseStatement spaces .>> eof
