namespace Masicalan.Extension

open Masicalan.Core
open System.IO

module IO =
    
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