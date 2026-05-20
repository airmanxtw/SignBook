open SignBook.Libs
open System
open SignBook.Models
open SignBook.Funs
open Argu
open FSharpPlus
open Spectre.Console

[<EntryPoint>]

let main args =

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
        printfn "Deleting configuration files..."
    else if results.Contains Check then
        printfn "Checking for configuration files..."
    else    
        ToolsProvider.readEncryFile (IO.Path.Combine(AppContext.BaseDirectory, "loginInfo.enc"))
        |> Result.bind PlaywrightProvider.start
        |> function
            | Ok g -> 
                AnsiConsole.WriteLine "Operation completed successfully."
                //printfn "Operation completed successfully."
            | Error e ->                 
                AnsiConsole.WriteException e                        
    

    // let (privateKey, publicKey) = RsaProvider.generateKeyPair ()
    // let loginInfo = {
    //     signUrl = "https://example.com/sign"
    //     userId = "user123"
    //     passWord = "password123"
    // }
    // let encryptedLoginFile = ToolsProvider.getEncryLoginFile publicKey privateKey loginInfo
    // let decryptedLoginInfo = ToolsProvider.getDecryLoginInfo encryptedLoginFile
    // printfn "Decrypted LoginInfo: signUrl:%A,userId:%A" decryptedLoginInfo.signUrl decryptedLoginInfo.userId


    // let savePath = IO.Path.Combine(AppContext.BaseDirectory, "privateKey.pem")
    // // 將私鑰保存到文件
    // let r = IOProvider.saveToFile savePath privateKey

    // let desc = RsaProvider.encrypt publicKey "Hello, RSA!"
    // let decrypted = RsaProvider.decrypt privateKey desc
    // printfn "Decrypted message: %s" decrypted
    //printfn "Hello from F#"
    0
