module FsLinter

module FsLint =

    open FSharpLint.Framework
    open Microsoft.FSharp.Compiler.SourceCodeServices

    let loadPlugins () =
        System.Reflection.Assembly.Load("FSharpLint.Rules")
        |> LoadVisitors.loadPlugins

    let configCheckers =
        System.Reflection.Assembly.Load("FSharpLint.Rules")
        |> LoadVisitors.loadConfigCheckers

    let initVisitorInfo postError =
        let config = Configuration.loadDefaultConfiguration()

        let configFailures =
            configCheckers
            |> (LoadVisitors.checkConfigsForFailures config)

        if configFailures.Length <> 0 then failwith "invalid config"

        let visitorInfo = { Ast.Config = config
                            Ast.PostError = postError }

        visitorInfo

    let plainTextVisitors (plugins: LoadVisitors.VisitorPlugin list) visitorInfo =
        [ for plugin in plugins do
            match plugin.Visitor with
                | LoadVisitors.Ast(_) -> ()
                | LoadVisitors.PlainText(visitor) ->
                    yield visitor visitorInfo ]

    let astVisitors (plugins: LoadVisitors.VisitorPlugin list) visitorInfo =
        [ for plugin in plugins do
            match plugin.Visitor with
                | LoadVisitors.Ast(visitor) ->
                    yield visitor visitorInfo
                | LoadVisitors.PlainText(_) -> () ]

    let visitPlainText file input plugins visitorInfo =
        for visitor in plainTextVisitors plugins visitorInfo do
            visitor file input

    let visitAst file input plugins visitorInfo = async {
        let checker = InteractiveChecker.Create()

        let! projectOptions = checker.GetProjectOptionsFromScript("c:/dummy", "")
        let projectOptions = { projectOptions with ProjectFileNames = [| file |] }

        return
            astVisitors plugins visitorInfo
            |> Ast.parse (fun () -> false) checker projectOptions file input
    }

open FsLint
open System.IO
open Microsoft.FSharp.Compiler

let onErrorWithInput f (input: string) =
    let lines = input.Split('\n')
    fun (rng: Range.range) msg ->
        let line = lines.[rng.StartLine-1]
        f line rng msg

let plugins = lazy loadPlugins()

let asyncFsLintInput onError input = async {
    let file = "dummy.fs"

    let plugins     = plugins.Force()
    let visitorInfo = initVisitorInfo (onErrorWithInput onError input)

    do! visitAst       file input plugins visitorInfo
    do  visitPlainText file input plugins visitorInfo
}

let asyncFsLintFile onError file = async {
    let input = File.ReadAllText(file)

    let plugins     = plugins.Force()
    let visitorInfo = initVisitorInfo (onErrorWithInput onError input)

    do! visitAst       file input plugins visitorInfo
    do  visitPlainText file input plugins visitorInfo
}