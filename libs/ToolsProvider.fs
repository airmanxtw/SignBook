namespace SignBook.Libs

module ToolsProvider =
    open System
    open System.IO
    open System.Security.Cryptography
    open System.Text
    open SignBook.Models
    open FSharpPlus
    open FSharp.Data
    open SignBook.models

    let infoFileName ="_20260521li.enc"

    let compressToZip (content:byte[]) =
        use outputStream = new MemoryStream()
        do
            use zipArchive = new Compression.ZipArchive(outputStream, Compression.ZipArchiveMode.Create, true)
            let zipEntry = zipArchive.CreateEntry infoFileName
            use entryStream = zipEntry.Open()
            entryStream.Write(content, 0, content.Length)
        outputStream.ToArray()

    let decompressFromZip (zipContent:byte[]) =
        use inputStream = new MemoryStream(zipContent)
        use zipArchive = new Compression.ZipArchive(inputStream, Compression.ZipArchiveMode.Read)
        let zipEntry = zipArchive.GetEntry infoFileName
        use entryStream = zipEntry.Open()
        use memoryStream = new MemoryStream()
        entryStream.CopyTo memoryStream
        memoryStream.ToArray()
       

    // 將model轉換成JSON字符串,使用System.Text.Json
    let getJSON (model:LoginInfo) =
        System.Text.Json.JsonSerializer.Serialize model

    // 生成LoginFile，包含加密的LoginInfo和私鑰
    let getEncryLoginFile model (privateKey,publicKey ) =       
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
        Result.protect (fun () -> 
                            compressToZip content
                            |> fun zipContent -> File.WriteAllBytes(file, zipContent)                            
                        ) ()

    // 從指定路徑讀取加密的LoginFile並解密出LoginInfo
    let readEncryFile (file:string) =
        Result.protect (fun () ->           
            if File.Exists file then 
                File.ReadAllBytes file
                |> decompressFromZip
                |> getDecryLoginInfo
                |> Some                       
            else
                None
        ) ()

    let delEncryFile file =
        Result.protect (fun () -> 
            if File.Exists file then 
                File.Delete file
        ) ()

    let getNugetVersion nugetVersionUrl=
        Result.protect(fun () ->
            Http.RequestString nugetVersionUrl
            |> JsonValue.Parse
            |> fun json -> json["versions"].AsArray() |> Array.map (fun v -> v.AsString()) |> List.ofArray
            |> List.tryLast                
        )()

    let getNugetVersionUrl file =
        Result.protect(fun () ->
            File.ReadAllText file
            |> JsonValue.Parse
            |> fun json -> json["nugetVersionUrl"].AsString()
        )()

    let getNugetVersion' file =
        getNugetVersionUrl file
        |> Result.bind (fun url -> getNugetVersion url)
        
        
        
        
        
        
        
        
        // let res = "https://api.nuget.org/v3-flatcontainer/signbook/index.json"
        // type NugetVersion = JsonProvider<"https://api.nuget.org/v3-flatcontainer/signbook/index.json">
        // res

    
        
        

   