namespace Masicalan.Extension

open Masicalan.Core
open System.IO

module IO =
    
    // fileExists(string fileName)
    let private fileExistsExtFunc (args: Value list) =
        match args with
        |[Value.StringVal fileName] ->
            File.Exists(fileName) |> Value.BoolVal
        |_-> 
            failwithf "fileExists: arguments error"

    // writeToFile(filePath, data)
    let private writeToFileExtFunc (args: Value list) =
        match args with
        |[Value.StringVal filePath; Value.StringVal data] ->
            File.WriteAllText(filePath, data)
            Value.VoidVal
        |_->
            failwithf "writeToFile: arguments error"