namespace Masicalan.Extension

open Masicalan.Core
open System.IO

module IOExtension =
    
    // fileExists(fileName)
    let private fileExistsExtFunc (args: Value list) =
        match args with
        |[Value.StringVal fileName] ->
            File.Exists(fileName) |> Value.BoolVal
        |_-> 
            failwithf "fileExists: arguments error"

    // directoryExists(directoryName)
    let private directoryExistsExtFunc (args: Value list) =
        match args with
        |[Value.StringVal directoryName] ->
            Directory.Exists(directoryName) |> Value.BoolVal
        |_->
            failwithf "directoryExists: argyments error"

    // writeToFile(filePath, data)
    let private writeToFileExtFunc (args: Value list) =
        match args with
        |[Value.StringVal filePath; Value.StringVal data] ->
            File.WriteAllText(filePath, data)
            Value.VoidVal
        |_->
            failwithf "writeToFile: arguments error"

    // readFromFile(filePath)
    let private readFromFileExtFunc (args: Value list) =
        match args with
        |[Value.StringVal filePath] ->
            File.ReadAllText(filePath) |> Value.StringVal
        |_->
            failwithf "readFromFile: arguments error"

    // copyFile(sourceFilePath, destFilePath, overwrite)
    let private copyFileExtFunc (args: Value list) =
        match args with
        |[Value.StringVal sourceFilePath; Value.StringVal destFilePath; Value.BoolVal overwrite] ->
            File.Copy(sourceFilePath, destFilePath, overwrite)
            Value.VoidVal
        |_->
            failwithf "copyFile: arguments error"

    // moveFile(sourceFilePath, destFilePath, overwrite)
    let private moveFileExtFunc (args: Value list) =
        match args with
        |[Value.StringVal sourceFilePath; Value.StringVal destFilePath; Value.BoolVal overwrite] ->
            File.Move(sourceFilePath, destFilePath, overwrite)
            Value.VoidVal
        |_->
            failwithf "moveFile: arguments error"

    let createExtEnv () =
        Map[
        ("fileExists"), (["fileName"], Statement.CallNativeF (fileExistsExtFunc, [Var "fileName"]))
        ("directoryExists"), (["directoryName"], Statement.CallNativeF (directoryExistsExtFunc, [Var "directoryName"]))
        ("writeToFile"), (["filePath"; "data"], Statement.CallNativeF (writeToFileExtFunc, [Var "filePath"; Var "data"]))
        ("readFromFile"), (["filePath"], Statement.CallNativeF (readFromFileExtFunc, [Var "filePath"]))
        ("copyFile"), (["sourceFilePath"; "destFilePath"; "overwrite"], Statement.CallNativeF (copyFileExtFunc, [Var "sourceFilePath"; Var "destFilePath"; Var "overwrite"]))
        ("moveFile"), (["sourceFilePath"; "destFilePath"; "overwrite"], Statement.CallNativeF (moveFileExtFunc, [Var "sourceFilePath"; Var "destFilePath"; Var "overwrite"]))
        ]