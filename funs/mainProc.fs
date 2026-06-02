namespace SignBook.Funs
module mainProc =
    open SignBook.Models
    let a = 0

    let inputSignAndSave (saveLoginInfo: (LoginInfo) -> Result<unit, exn>) (inputInfo:unit->string*string*string)  =
        inputInfo() |> fun (url, id, pw) ->
            let model = { signUrl = url; userId = id; passWord = pw }
            saveLoginInfo model
        