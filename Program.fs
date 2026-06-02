open SignBook.Libs
open System
open SignBook.Models
open SignBook.Funs
open Argu
open FSharpPlus
open Spectre.Console
open  System.Diagnostics;

[<EntryPoint>]

let main args =    
    let infoFileName =IO.Path.Combine(AppContext.BaseDirectory, "_20260521li.enc")
    let parser = ArgumentParser.Create<CommandLineArguments>(programName = "SignBook.exe")
    let results = parser.Parse args 
    let saveLoginInfo infoFile (model:LoginInfo) =  
            RsaProvider.generateKeyPair ()
            |> ToolsProvider.getEncryLoginFile model            
            |> ToolsProvider.saveEncryFile infoFile

    if results.Contains Help then        
        printfn "Usage: SignBook.exe [options]"
        printfn "Options:"
        printfn "  -h, --help     顯示幫助訊息"
        printfn "  -v, --version  顯示版本資訊"
        printfn "  -d, --delete   移除設定檔"
        printfn "  -c, --check    檢查是否有設定檔"
        printfn "  -a, --auto     自動執行簽到（從設定檔讀取登入資訊）"
        printfn "  -s, --sign     輸入登入資訊並儲存到設定檔"
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

    else if results.Contains Sign then
        AnsiConsole.Write (Text("===設定登入資訊===", Style(foreground = Color.Green, decoration = Decoration.Bold)))
        AnsiConsole.WriteLine()

        mainProc.inputSignAndSave (saveLoginInfo infoFileName) IOProvider.inputInfo
        |> function
            | Ok _ -> AnsiConsole.MarkupLine "[green]登入資訊已保存到加密文件[/]"
            | Error e -> AnsiConsole.MarkupLine $"[red]保存登入資訊失敗: {e.Message}[/]"

    else          
        let sw = Stopwatch.StartNew()
        // 設定驅動程式路徑（install 和 CreateAsync 都會讀取此環境變數）
        let playwrightAssemblyDir = AppContext.BaseDirectory
        let platform = 
            if OperatingSystem.IsWindows() then "win32_x64"
            elif OperatingSystem.IsMacOS() then "mac"
            else "linux"
        let driverExe = if OperatingSystem.IsWindows() then "node.exe" else "node"
        let driverPath = IO.Path.Combine(playwrightAssemblyDir, ".playwright", "node", platform, driverExe)
        Environment.SetEnvironmentVariable("PLAYWRIGHT_DRIVER_PATH", driverPath)

        // 確保 Playwright 驅動及瀏覽器已安裝（幂等，已安裝時自動跳過）
        Microsoft.Playwright.Program.Main [| "install"; "chromium" |] |> ignore        

        ToolsProvider.readEncryFile (IO.Path.Combine(AppContext.BaseDirectory, infoFileName))
        |> Result.bind (PlaywrightProvider.start  (saveLoginInfo infoFileName) IOProvider.inputInfo (results.Contains Auto))
        |> function
            | Ok g -> 
                AnsiConsole.WriteLine "Operation completed successfully."                
            | Error e ->                 
                AnsiConsole.WriteException e   

        sw.Stop()
        AnsiConsole.MarkupLine $"[blue]Execution time: {sw.Elapsed.TotalSeconds:F2} seconds[/]"                     
    

  
    0
