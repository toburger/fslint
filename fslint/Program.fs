open Nessos.UnionArgParser
open System


type CLIArguments =
    | [<Mandatory; AltCommandLine("-f")>] Filename of string
    | [<AltCommandLine("-c")>] Color
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Filename _ -> "specify a file to lint."
            | Color -> "colorize the console output."

let jsFiddleError next line _ error =
    printfn " #%i %s" (next()) error
    printfn "%s" line

let jsFiddleErrorColor next line _ error =
    printf " #%i " (next())
    Console.ForegroundColor <- ConsoleColor.DarkYellow
    printfn "%s" error
    Console.ForegroundColor <- ConsoleColor.DarkGray
    printfn "%s" line
    Console.ResetColor()

let onError line (range : Microsoft.FSharp.Compiler.Range.range) error =
    let info = sprintf "[%i,%i]" range.StartLine range.StartColumn
    printfn "%s %s\n> %s" info error line

let onErrorColorized line (range : Microsoft.FSharp.Compiler.Range.range) error =
    let info = sprintf "[%i,%i]" range.StartLine range.StartColumn
    printf "%s " info
    Console.ForegroundColor <- ConsoleColor.Yellow
    printfn "%s" error
    Console.ResetColor()
    printf "> "
    Console.ForegroundColor <- ConsoleColor.DarkGray
    printfn "%s" line
    Console.ResetColor()

[<EntryPoint>]
let main argv =
    let parser = UnionArgParser.Create<CLIArguments>()
    let results = parser.ParseCommandLine(argv, ProcessExiter())
    let file = results.GetResult <@ Filename @>

    let nrinc =
        let nr = ref 0
        fun () ->
        incr nr
        !nr

    let onError =
        if results.Contains <@ Color @> then jsFiddleErrorColor nrinc
        else jsFiddleError nrinc
    //    let onError =
    //        if results.Contains <@ Color @> then onErrorColorized
    //        else onError

    FsLinter.asyncFsLintFile onError file |> Async.RunSynchronously
    0
