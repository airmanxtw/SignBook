open SignBook.Libs
open System
open SignBook.Models
open SignBook.Funs
open Argu
open FSharpPlus
open Spectre.Console

[<EntryPoint>]

let main args =
    let infoFileName ="_20260521li.enc"
    let parser = ArgumentParser.Create<CommandLineArguments>(programName = "SignBook.exe")
    let results = parser.Parse args    
    if results.Contains Help then        
        printfn "Usage: SignBook.exe [options]"
        printfn "Options:"
        printfn "  -h, --help     顯示幫助訊息"
        printfn "  -v, --version  顯示版本資訊"
        printfn "  -d, --delete   移除設定檔"
        printfn "  -c, --check    檢查是否有設定檔"
    else if results.Contains Version then
        Reflection.Assembly.GetExecutingAssembly().GetName().Version
            |> fun v -> printfn "SignBook version %d.%d.%d" v.Major v.Minor v.Build        
    else if results.Contains  Delete then
        ToolsProvider.delEncryFile (IO.Path.Combine(AppContext.BaseDirectory, infoFileName))
        |> function
            | Ok _ -> AnsiConsole.MarkupLine "[green]Configuration files deleted successfully.[/]"
            | Error e -> AnsiConsole.WriteException e
        
    else if results.Contains Check then
        if IO.File.Exists (IO.Path.Combine(AppContext.BaseDirectory, infoFileName)) then
            AnsiConsole.MarkupLine "[green]Configuration file exists.[/]"
        else
            AnsiConsole.MarkupLine "[red]Configuration file does not exist.[/]"
    else   

        // 自動偵測 Playwright 驅動，首次使用時自動安裝
        let playwrightDriver = IO.Path.Combine(AppContext.BaseDirectory, ".playwright")
        if not (IO.Directory.Exists playwrightDriver) then
            AnsiConsole.MarkupLine "[yellow]首次使用，正在安裝 Playwright 瀏覽器驅動程式...[/]"
            Microsoft.Playwright.Program.Main [| "install"; "chromium" |] |> ignore

        let saveLoginInfo (model:LoginInfo) =  
            RsaProvider.generateKeyPair ()
            |> ToolsProvider.getEncryLoginFile model            
            |> ToolsProvider.saveEncryFile (IO.Path.Combine(AppContext.BaseDirectory, infoFileName))          

        ToolsProvider.readEncryFile (IO.Path.Combine(AppContext.BaseDirectory, infoFileName))
        |> Result.bind (PlaywrightProvider.start saveLoginInfo)
        |> function
            | Ok g -> 
                AnsiConsole.WriteLine "Operation completed successfully."                
            | Error e ->                 
                AnsiConsole.WriteException e                        
    

  
    0
