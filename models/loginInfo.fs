namespace SignBook.Models

type LoginInfo = {
    signUrl:string
    userId:string
    passWord:string    
}

type LoginFile = {
    privateKey:string
    encryptionLoginInfo:string
}

