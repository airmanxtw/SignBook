open SignBook.Libs
open System

[<EntryPoint>]

let main _ =
    let (privateKey, publicKey) = RsaProvider.generateKeyPair ()
    let savePath = IO.Path.Combine(AppContext.BaseDirectory, "privateKey.pem")
    // 將私鑰保存到文件
    let r = IOProvider.saveToFile savePath privateKey

    let desc = RsaProvider.encrypt publicKey "Hello, RSA!"
    let decrypted = RsaProvider.decrypt privateKey desc
    printfn "Decrypted message: %s" decrypted
    printfn "Hello from F#"
    0
