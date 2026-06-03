open FParsec
open Masicalan.Core
open System
open System.Diagnostics
open System.IO
open System.Reflection
open System.Runtime.InteropServices

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

// The Main Entry as follow:

let assemblyFilePath = Assembly.GetExecutingAssembly().Location
let baseDirectory = AppDomain.CurrentDomain.BaseDirectory
let sampleCodesDirectory = Path.Combine(baseDirectory, "Samples")

let appName = FileVersionInfo.GetVersionInfo(assemblyFilePath).ProductName
let appVersion = FileVersionInfo.GetVersionInfo(assemblyFilePath).ProductVersion
let developerName = FileVersionInfo.GetVersionInfo(assemblyFilePath).CompanyName
let copyright = FileVersionInfo.GetVersionInfo(assemblyFilePath).LegalCopyright
let systemVersion = Environment.OSVersion.ToString()
let runtimeVersion = RuntimeInformation.FrameworkDescription

printfn "---------------------------------------------------------------------------"
printfn "%s ver.%s" appName appVersion
printfn "%s" copyright
printfn "%s" systemVersion
printfn "%s" runtimeVersion
printfn "---------------------------------------------------------------------------"
