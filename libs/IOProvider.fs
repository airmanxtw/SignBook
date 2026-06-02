namespace SignBook.Libs

module IOProvider =
    open System
    open System.IO
    open FSharpPlus
    open Spectre.Console

    let saveToFile (filePath: string) (contentBytes: byte array) =
        Result.protect (fun () -> File.WriteAllBytes(filePath, contentBytes)) ()

    let inputInfo () =
        let mutable signUrl = ""
        let mutable userid = ""
        let mutable password = ""
        signUrl <- AnsiConsole.Ask<string> "輸入簽到網址:"
        userid <- AnsiConsole.Ask<string> "輸入登入帳號:"
        password <- AnsiConsole.Prompt(TextPrompt<string>("輸入登入密碼:").Secret())
        signUrl, userid, password
