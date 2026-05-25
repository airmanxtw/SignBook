namespace SignBook.Models

open Argu

type CommandLineArguments =   
    | [<CustomCommandLine("help"); AltCommandLine("-h")>] Help
    | [<AltCommandLine("-a")>] Auto
    | [<AltCommandLine("-v")>] Version
    | [<AltCommandLine("-d")>] Delete
    | [<AltCommandLine("-c")>] Check    

    interface IArgParserTemplate with
        member s.Usage =
            match s with   
            | Help -> "顯示幫助訊息"         
            | Version -> "顯示版本資訊"
            | Delete -> "移除設定檔"
            | Check  -> "檢查是否有設定檔"
            | Auto -> "自動執行簽到（從設定檔讀取登入資訊）"
           