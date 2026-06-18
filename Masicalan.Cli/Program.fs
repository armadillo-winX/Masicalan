open FParsec
open Masicalan.Core
open Masicalan.Extension
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

let runIntprtWithStdExt script =
    let ioExt = IOExtension.createExtEnv ()
    Interpreter.RunWithExt script ioExt |> ignore

let runInterprAndPrintEnv script =
    let env = Interpreter.Run script
    printfn "%A" env

let readScriptFile filePath =
    let script = File.ReadAllText(filePath)
    script

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
printfn "OS: %s" systemVersion
printfn "Runtime: %s" runtimeVersion
printfn "---------------------------------------------------------------------------"


while true do
    printfn "[0] Run script from a script file"
    printfn "[2] Run sample script files"
    printfn "[x] Exit"
    printfn "Enter operation:"
    let input = Console.ReadLine()
    match input with
    | "0" -> 
        printfn "Enter script file path:"
        let pathInput = Console.ReadLine()
        try
            readScriptFile pathInput |> runInterpreter
        with
        |_ as ex -> printfn "%s" ex.Message
    | "2" -> 
        let files = Directory.GetFiles(sampleCodesDirectory, "*.masis", SearchOption.TopDirectoryOnly)
        let filesOption = files |> Option.ofObj
        match filesOption with
        | Some fs -> 
            let mutable i = 0
            for file in fs do
                Path.GetFileName file |> printfn "[%d] %s" i
                i <- i + 1
            printfn "Select sample code file:"
            let result, inputInt = Console.ReadLine() |> Int32.TryParse
            if result then
                let fileName = fs.[inputInt]
                let sampleFilePath = Path.Combine(sampleCodesDirectory, fileName)
                try
                    readScriptFile sampleFilePath |> runInterpreter
                with
                |_ as ex -> printfn "%s" ex.Message
            else
                printfn "Invalid selection"
        | None -> printfn "Sample code files are missing."
    | "x" -> exit 0
    |_ -> printfn "Invalid operation.\n"