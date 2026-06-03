open FParsec
open Masicalan.Core

let script = """
let x = 2;
let y = 3;

let a = x + y;
if a > 10 then 
{
    print 0 ;
}
else {
    let z = 1 ;
    print z ;
}

let b = x * y ;

if b > 10 then { print 0 ; } else { print 1 ; }


let i = 0 ;
while i < 10 do {
    i <- i + 1 ;
}
print i ;


"""

let env = Interpreter.Run script
printfn "%A" env

//match run Parser.parseProgram script with
//    | Success(ast, _, _) ->
//        printfn "%A" ast
//    | Failure(error, _, _) ->
//        printfn "failed: %s" error |>ignore
