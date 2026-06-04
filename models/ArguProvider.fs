namespace SignBook.Models

open Argu

type YearAndMonthArgs =
    | [<AltCommandLine("-d")>] YearAndMonth of int*int
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | YearAndMonth _ -> "顯示月份簽到記錄"

and CommandLineArguments =   
    | [<CustomCommandLine("help"); AltCommandLine("-h")>] Help
    | [<AltCommandLine("-a")>] Auto
    | [<AltCommandLine("-r")>] ShowRecord
    | [<AltCommandLine("-v")>] Version
    | [<AltCommandLine("-d")>] Delete
    | [<AltCommandLine("-c")>] Check
    | [<AltCommandLine("-s")>] Sign
    | [<CliPrefix(CliPrefix.None)>] Record of ParseResults<YearAndMonthArgs>

    interface IArgParserTemplate with
        member s.Usage =
            match s with   
            | Help -> "顯示幫助訊息"                     
            | Version -> "顯示版本資訊"
            | Delete -> "移除設定檔"
            | Check  -> "檢查是否有設定檔"
            | Auto -> "自動執行簽到（從設定檔讀取登入資訊）"
            | ShowRecord -> "自動執行簽到且顯示簽到記錄"
            | Sign -> "輸入登入資訊並儲存到設定檔"
            | Record _ -> "顯示月份簽到記錄"
           