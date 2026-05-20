namespace SignBook.Libs

module RsaProvider =
    open System
    open System.Security.Cryptography
    open System.Text

    let generateKeyPair () =
        let rsa = RSA.Create(2048)
        let privateKey = rsa.ExportRSAPrivateKey()
        let publicKey = rsa.ExportRSAPublicKey()
        (privateKey, publicKey)

    // 用公钥加密数据
    let encrypt (publicKey: byte[]) (data: string) =
        let rsa = RSA.Create()
        let bytesRead = ref 0
        rsa.ImportRSAPublicKey(publicKey, bytesRead) |> ignore
        let dataBytes = Encoding.UTF8.GetBytes(data)
        rsa.Encrypt(dataBytes, RSAEncryptionPadding.Pkcs1)
        |> Convert.ToBase64String

    // 用私钥解密数据
    let decrypt (privateKey: byte[]) (encryptedData: byte[]) =
        let rsa = RSA.Create()
        let bytesRead = ref 0
        rsa.ImportRSAPrivateKey(privateKey, bytesRead) |> ignore

        rsa.Decrypt(encryptedData, RSAEncryptionPadding.Pkcs1)
        |> Encoding.UTF8.GetString
