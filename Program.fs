open SignBook.Libs
open System
open SignBook.Models
open SignBook.Funs
open Argu
open FSharpPlus
open Spectre.Console
open System.Diagnostics

[<EntryPoint>]

(***
    1. https://api.nuget.org/v3-flatcontainer/signbook/index.json
    2.
***)

let main args =
        

    let infoFileName = IO.Path.Combine(AppContext.BaseDirectory, "_20260521li.enc")

    let parser =
        ArgumentParser.Create<CommandLineArguments>(programName = "SignBook.exe")

    let results = parser.Parse args

    let saveLoginInfo infoFile (model: LoginInfo) =
        RsaProvider.generateKeyPair ()
        |> ToolsProvider.getEncryLoginFile model
        |> ToolsProvider.saveEncryFile infoFile

    //
    let getNugetVersion() =
        ToolsProvider.getNugetVersion' (System.IO.Path.Combine(AppContext.BaseDirectory,"appsettings.json"))

    // 取得目前版本號
    let getCurrentVersion() =
        Reflection.Assembly.GetExecutingAssembly().GetName().Version
        |> fun v -> $"{v.Major}.{v.Minor}.{v.Build}" 

    // 顯示版本更新訊息
    let echoVersionInfo (currentVersion:string) (latestVersion:string) =
        if currentVersion <> latestVersion then
            let panel = new Panel $"dotnet tool install --global SignBook --version {latestVersion}"
            AnsiConsole.Write (panel.Header("有新版本").BorderColor(Color.Yellow).DoubleBorder())                        
        else
            AnsiConsole.MarkupLine $"[green]You are using the latest version: {currentVersion}[/]"


    let setPlaywrightEnvir () =
        let playwrightAssemblyDir = AppContext.BaseDirectory

        let platform =
            if OperatingSystem.IsWindows() then "win32_x64"
            elif OperatingSystem.IsMacOS() then "mac"
            else "linux"

        let driverExe = if OperatingSystem.IsWindows() then "node.exe" else "node"

        let driverPath =
            IO.Path.Combine(playwrightAssemblyDir, ".playwright", "node", platform, driverExe)

        Environment.SetEnvironmentVariable("PLAYWRIGHT_DRIVER_PATH", driverPath)

        // 確保 Playwright 驅動及瀏覽器已安裝（幂等，已安裝時自動跳過）
        Microsoft.Playwright.Program.Main [| "install"; "chromium" |] |> ignore


    if results.Contains Help then
        printfn "Usage: SignBook.exe [options]"
        printfn "Options:"
        printfn "  -h, --help     顯示幫助訊息"
        printfn "  -v, --version  顯示版本資訊"
        printfn "  -d, --delete   移除設定檔"
        printfn "  -c, --check    檢查是否有設定檔"
        printfn "  -a, --auto     自動執行簽到（從設定檔讀取登入資訊）"
        printfn "  -r, --showrecord 顯示當月份簽到記錄"
        printfn "  -s, --sign     輸入登入資訊並儲存到設定檔"
        printfn "Usage: SignBook.exe record [options]"
        printfn "Options for record:"
        printfn "  -d <year> <month>  顯示月份簽到記錄 ex. -d 2026 5"


    else if results.Contains Version then
        getCurrentVersion()
        |> fun v -> printfn "SignBook version %s" v

    else if results.Contains Upgrade then
        getNugetVersion()
        |> function
            | Ok(Some version) -> echoVersionInfo (getCurrentVersion()) version                               
            | Ok None -> AnsiConsole.MarkupLine "[yellow]No version information found.[/]"
            | Error e -> AnsiConsole.WriteException e

    else if results.Contains Delete then
        ToolsProvider.delEncryFile (IO.Path.Combine(AppContext.BaseDirectory, infoFileName))
        |> function
            | Ok _ -> AnsiConsole.MarkupLine "[green]Configuration files deleted successfully.[/]"
            | Error e -> AnsiConsole.WriteException e

    else if results.Contains Check then
        if IO.File.Exists(IO.Path.Combine(AppContext.BaseDirectory, infoFileName)) then
            AnsiConsole.MarkupLine "[green]Configuration file exists.[/]"
        else
            AnsiConsole.MarkupLine "[red]Configuration file does not exist.[/]"

    else if results.Contains Sign then
        AnsiConsole.Write(Text("===設定登入資訊===", Style(foreground = Color.Green, decoration = Decoration.Bold)))
        AnsiConsole.WriteLine()

        mainProc.inputSignAndSave (saveLoginInfo infoFileName) IOProvider.inputInfo
        |> function
            | Ok _ -> AnsiConsole.MarkupLine "[green]登入資訊已保存到加密文件[/]"
            | Error e -> AnsiConsole.MarkupLine $"[red]保存登入資訊失敗: {e.Message}[/]"

    else if results.Contains Record then
        let getMonthArgs = results.GetResult Record

        getMonthArgs.GetResults YearAndMonth
        |> List.head
        |> fun (year, month) ->
            ToolsProvider.readEncryFile infoFileName
            |> function
                | Ok(Some model) ->
                    PlaywrightProvider.showMonthRecord model (year, month)
                    |> function
                        | Ok _ -> AnsiConsole.MarkupLine "[green]月份簽到記錄顯示完成[/]"
                        | Error e -> AnsiConsole.MarkupLine $"[red]顯示月份簽到記錄失敗: {e.Message}[/]"
                | Ok None -> AnsiConsole.MarkupLine "[yellow]No configuration file found. Please sign in first.[/]"
                | Error e -> AnsiConsole.WriteException e

        ()


    else
        let sw = Stopwatch.StartNew()
        // 設定驅動程式路徑（install 和 CreateAsync 都會讀取此環境變數）
        do setPlaywrightEnvir ()

        ToolsProvider.readEncryFile infoFileName
        |> Result.bind (
            PlaywrightProvider.start
                (saveLoginInfo infoFileName)
                IOProvider.inputInfo
                (results.Contains Auto)
                (results.Contains ShowRecord)
        )
        |> function
            | Ok g -> AnsiConsole.WriteLine "Operation completed successfully."
            | Error e -> AnsiConsole.WriteException e

        sw.Stop()
        AnsiConsole.MarkupLine $"[blue]Execution time: {sw.Elapsed.TotalSeconds:F2} seconds[/]"

        getNugetVersion()
        |> function
            | Ok(Some version) -> echoVersionInfo (getCurrentVersion()) version                               
            | Ok None -> ()
            | Error e -> ()





    0
