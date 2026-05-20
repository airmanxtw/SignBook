namespace SignBook.Libs

module ToolsProvider =
    open System
    open System.IO
    open System.Security.Cryptography
    open System.Text
    open SignBook.Models
    open FSharpPlus


    // 將model轉換成JSON字符串,使用System.Text.Json
    let getJSON (model:LoginInfo) =
        System.Text.Json.JsonSerializer.Serialize model

    // 生成LoginFile，包含加密的LoginInfo和私鑰
    let getEncryLoginFile publicKey privateKey model =       
        (getJSON model, Convert.ToBase64String privateKey,RsaProvider.encrypt publicKey (getJSON model))
        |> fun (modelJson, privateKeyStr, LoginInfoEncryption) ->
            {
                privateKey = privateKeyStr
                encryptionLoginInfo = LoginInfoEncryption
            }
            |> System.Text.Json.JsonSerializer.Serialize
            |> Encoding.UTF8.GetBytes

    // 從加密的LoginFile中解密出LoginInfo
    let getDecryLoginInfo (encryLoginFile:byte[]) =        
        let loginFile = System.Text.Json.JsonSerializer.Deserialize<LoginFile> encryLoginFile
        let privateKeyBytes = Convert.FromBase64String loginFile.privateKey
        let encryptedDataBytes = Convert.FromBase64String loginFile.encryptionLoginInfo
        RsaProvider.decrypt privateKeyBytes encryptedDataBytes
        |> System.Text.Json.JsonSerializer.Deserialize<LoginInfo>

    // 將加密的LoginFile保存到指定路徑
    let saveEncryFile (file:string) (content:byte[])=
        Result.protect (fun () -> File.WriteAllBytes(file,content)) ()

    // 從指定路徑讀取加密的LoginFile並解密出LoginInfo
    let readEncryFile (file:string) =
        Result.protect (fun () ->           
            if File.Exists file then 
                File.ReadAllBytes file
                |> getDecryLoginInfo
                |> Some                       
            else
                None
        ) ()
       
        
        

   