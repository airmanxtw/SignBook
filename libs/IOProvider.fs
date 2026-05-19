namespace SignBook.Libs

module IOProvider =
    open System
    open System.IO
    open FSharpPlus

    let saveToFile (filePath: string) (contentBytes: byte array) =
        Result.protect (fun () -> File.WriteAllBytes(filePath, contentBytes)) ()
