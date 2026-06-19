namespace Masicalan.Extension

open Masicalan.Core
open System.IO
open System.Diagnostics

module ProcessExtension =

    // start(filePath)
    let private startExFunc (args: Value list) =
        match args with
        | [Value.StringVal filePath] ->
            let dir = Path.GetDirectoryName(filePath)
            let psi = new ProcessStartInfo()
            psi.FileName <- filePath
            psi.WorkingDirectory <- dir
            psi.UseShellExecute <- true
            Process.Start(psi)
            |> fun p -> p.Id
            |> Value.IntVal
        |_->
            failwithf "start: arguments error"

    // startWithArgs(filePath, args)
    let private startWithArgsExFunc (args: Value list) =
        match args with
        | [Value.StringVal filePath; Value.StringVal arg] ->
            let dir = Path.GetDirectoryName(filePath)
            let psi = new ProcessStartInfo()
            psi.FileName <- filePath
            psi.WorkingDirectory <- dir
            psi.UseShellExecute <- true
            psi.Arguments <- arg
            Process.Start(psi)
            |> fun p -> p.Id
            |> Value.IntVal
        |_->
            failwithf "start: arguments error"