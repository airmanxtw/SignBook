namespace SignBook.Libs

module PlaywrightProvider =
    open Microsoft.Playwright
    open Spectre.Console
    open SignBook.Models
    open TesseractOCR
    open System
    open FSharpPlus


    // 啟動一個 Playwright 瀏覽器實例並返回一個新的頁面
    let getPlaywrightPage () =
        let playwright = Playwright.CreateAsync().Result
        let launchOptions = BrowserTypeLaunchOptions()
        launchOptions.Channel <- "chrome"
        let browser = playwright.Chromium.LaunchAsync(launchOptions).Result
        browser.NewPageAsync().Result

    // 登入
    let toLoginByPage (page: IPage) (model: LoginInfo) =
        let signUrl, userid, password = model.signUrl, model.userId, model.passWord
        page.GotoAsync(signUrl).Result |> ignore

        page.FillAsync("#txtUserID", userid)
        |> Async.AwaitTask
        |> Async.RunSynchronously

        page.FillAsync("#txtPasswd", password)
        |> Async.AwaitTask
        |> Async.RunSynchronously

        page.ClickAsync "#Login_Btn" |> Async.AwaitTask |> Async.RunSynchronously
        System.Threading.Thread.Sleep 1000

    let showMonthRecord' (page: IPage) (year: int, month: int) =
        page.Locator("a[href='UserSignLogMonth.aspx']").ClickAsync()
        |> Async.AwaitTask
        |> Async.RunSynchronously

        System.Threading.Thread.Sleep 1000

        page.SelectOptionAsync("#ctl00_ContentPlaceHolder1_ddl_year", year.ToString()).Result
        |> ignore

        page.SelectOptionAsync("#ctl00_ContentPlaceHolder1_ddl_mon", month.ToString()).Result
        |> ignore

        page.ClickAsync "#ctl00_ContentPlaceHolder1_ibtn_sel"
        |> Async.AwaitTask
        |> Async.RunSynchronously

        System.Threading.Thread.Sleep 1000
        let table = Table()
        let rows = page.Locator("center > table").Nth(1).Locator "tr"

        for i in 0 .. rows.CountAsync().Result - 1 do
            if i = 0 then
                let cells = rows.Nth(i).Locator "td"
                let c = cells.CountAsync().Result

                for j in 0 .. cells.CountAsync().Result - 1 do
                    let text = cells.Nth(j).InnerTextAsync().Result
                    table.AddColumn(text) |> ignore
            else
                let cells = rows.Nth(i).Locator "td"

                let getCell index =
                    cells.Nth index
                    |> fun cell ->
                        if
                            cell.GetAttributeAsync "bgcolor" |> Async.AwaitTask |> Async.RunSynchronously = "#FFC0C0"
                        then
                            $"[black on red]{cell.InnerTextAsync().Result}[/]"
                        else
                            cell.InnerTextAsync().Result

                let cellSeq =
                    seq {
                        for j in 0 .. cells.CountAsync().Result - 1 do
                            yield getCell j
                    }

                table.AddRow(cellSeq |> Seq.toArray) |> ignore

        AnsiConsole.Write table

    let start
        (saveLoginInfo: (LoginInfo) -> Result<unit, exn>)
        (inputInfo: unit -> string * string * string)
        (autoSign: bool)
        (showRecord: bool)
        (model: Option<LoginInfo>)
        =
        Result.protect
            (fun () ->

                let figletTitle = new FigletText "SignBook"
                figletTitle.Color <- Color.Green

                AnsiConsole.Write figletTitle
                AnsiConsole.WriteLine()

                let mutable alertMessage = ""
                let mutable page = null
                let mutable signUrl = ""
                let mutable userid = ""
                let mutable password = ""
                let mutable ocrTxt = ""

                AnsiConsole
                    .Status()
                    .Spinner(Spinner.Known.Binary)
                    .Start(
                        "正在啟動中...",
                        fun ctx ->
                            page <- getPlaywrightPage ()
                            ()
                    )

                page.Dialog.Add(fun dialog ->
                    if dialog.Type = "alert" then
                        alertMessage <- dialog.Message

                    dialog.AcceptAsync() |> ignore)

                match model with
                | Some v ->
                    signUrl <- v.signUrl
                    userid <- v.userId
                    password <- v.passWord
                | None ->
                    inputInfo ()
                    |> fun (url, id, pw) ->
                        signUrl <- url
                        userid <- id
                        password <- pw


                AnsiConsole
                    .Status()
                    .Spinner(Spinner.Known.Binary)
                    .Start(
                        "嘗試登入中...",
                        fun ctx ->
                            do
                                toLoginByPage
                                    page
                                    { signUrl = signUrl
                                      userId = userid
                                      passWord = password }
                    )

                if not (String.IsNullOrEmpty alertMessage) then
                    AnsiConsole.MarkupLine $"[red]登入失敗: {alertMessage}[/]"
                else
                    match model with
                    | None ->
                        saveLoginInfo
                            { signUrl = signUrl
                              userId = userid
                              passWord = password }
                        |> function
                            | Ok _ -> AnsiConsole.MarkupLine "[green]登入資訊已保存到加密文件[/]"
                            | Error e -> AnsiConsole.MarkupLine $"[red]保存登入資訊失敗: {e.Message}[/]"
                    | Some _ -> AnsiConsole.MarkupLine "[green]使用加密文件登入[/]"

                    let preOcrTxt =
                        page.QuerySelectorAsync "#ctl00_ContentPlaceHolder1_Image2"
                        |> Async.AwaitTask
                        |> Async.RunSynchronously
                        |> fun imgElement ->
                            imgElement.ScreenshotAsync()
                            |> Async.AwaitTask
                            |> Async.RunSynchronously
                            |> fun imgBytes ->
                                let image = CanvasImage imgBytes
                                image.MaxWidth <- 38
                                AnsiConsole.Write image
                                imgBytes
                            |> fun imgBytes ->
                                // 使用 Tesseract OCR 解析圖片
                                use ocr =
                                    new Engine(
                                        AppContext.BaseDirectory,
                                        Enums.Language.English,
                                        TesseractOCR.Enums.EngineMode.Default
                                    )

                                ocr.SetVariable("tessedit_char_whitelist", "0123456789") |> ignore

                                TesseractOCR.Pix.Image.LoadFromMemory imgBytes
                                |> fun pix ->
                                    pix.XRes <- 388
                                    pix.YRes <- 388
                                    pix
                                |> ocr.Process
                                |> fun p -> p.Text

                    //AnsiConsole.MarkupLine $"驗證碼: [green]{tx}[/]"
                    if autoSign then
                        AnsiConsole.MarkupLine $"[yellow]自動簽到模式已啟用，使用辨識值: {preOcrTxt}[/]"
                        ocrTxt <- preOcrTxt
                    else
                        ocrTxt <- AnsiConsole.Prompt(TextPrompt<string>("輸入驗證碼(按下Enter使用辨識值):").DefaultValue preOcrTxt)

                    // 取得簽到鈕旁的時間
                    let getDateTime (p: IPage) (selector: string) =
                        p.QuerySelectorAsync selector
                        |> Async.AwaitTask
                        |> Async.RunSynchronously
                        |> fun element -> element.InnerTextAsync() |> Async.AwaitTask |> Async.RunSynchronously
                        |> fun t -> DateTime.Parse(String.Format("{0:yyyy/MM/dd} {1}", DateTime.Now, t))

                    let selectBtns =
                        page.QuerySelectorAllAsync
                            "#ctl00_ContentPlaceHolder1_gv_signbook input[type='submit']:not([disabled])"
                        |> Async.AwaitTask
                        |> Async.RunSynchronously
                        |> fun buttons ->
                            buttons
                            |> Seq.filter (fun button ->
                                let id = button.GetAttributeAsync "id" |> Async.AwaitTask |> Async.RunSynchronously

                                let startTime =
                                    getDateTime page $"xpath=//input[@id='{id}']/parent::td/following-sibling::td[1]"

                                let endTime =
                                    getDateTime page $"xpath=//input[@id='{id}']/parent::td/following-sibling::td[2]"

                                true || DateTime.Now >= startTime && DateTime.Now <= endTime)
                            |> Seq.map (fun button ->
                                let id = button.GetAttributeAsync "id" |> Async.AwaitTask |> Async.RunSynchronously

                                let value =
                                    button.GetAttributeAsync "value" |> Async.AwaitTask |> Async.RunSynchronously

                                let startTime =
                                    getDateTime page $"xpath=//input[@id='{id}']/parent::td/following-sibling::td[1]"

                                let endTime =
                                    getDateTime page $"xpath=//input[@id='{id}']/parent::td/following-sibling::td[2]"

                                { id = id
                                  value = value
                                  startTime = startTime
                                  endTime = endTime })
                            |> Seq.toList

                    if selectBtns.Length = 0 then
                        AnsiConsole.MarkupLine "[yellow]沒有可簽到的項目[/]"
                    else
                        let firstBtn = selectBtns.Head

                        let canAutoSign =
                            autoSign
                            && firstBtn.startTime <= DateTime.Now
                            && firstBtn.endTime >= DateTime.Now

                        let selectBtn =
                            if canAutoSign then
                                AnsiConsole.MarkupLine $"[yellow]自動簽到模式已啟用，選擇第一個項目: {firstBtn.value}[/]"
                                firstBtn
                            else
                                AnsiConsole.Prompt(
                                    SelectionPrompt<SelectBtn>()
                                        .Title("選擇要簽到的項目:")
                                        .PageSize(10)
                                        .UseConverter(fun btn -> $"{btn.value}")
                                        .AddChoices
                                        selectBtns
                                )

                        AnsiConsole
                            .Status()
                            .Start(
                                "正在簽到中...",
                                fun ctx ->
                                    page.FillAsync("#ctl00_ContentPlaceHolder1_txt_authimg", ocrTxt)
                                    |> Async.AwaitTask
                                    |> Async.RunSynchronously

                                    page.ClickAsync $"#{selectBtn.id}" |> Async.AwaitTask |> Async.RunSynchronously
                                    page.GotoAsync(signUrl).Result |> ignore
                                    System.Threading.Thread.Sleep 1000
                            )


                        if not (String.IsNullOrEmpty alertMessage) then
                            AnsiConsole.MarkupLine $"[red]簽到失敗: {alertMessage}[/]"
                        else
                            page.QuerySelectorAsync
                                $"xpath=//input[@id='{selectBtn.id}']/parent::td/following-sibling::td[3]/span"
                            |> Async.AwaitTask
                            |> Async.RunSynchronously
                            |> fun element ->
                                if element = null then
                                    AnsiConsole.MarkupLine "[red]查無訊息,有可能簽到失敗,再試一次[/]"
                                else
                                    let text = element.InnerTextAsync() |> Async.AwaitTask |> Async.RunSynchronously
                                    let color = if text.Contains "簽到異常" then "red" else "green"
                                    AnsiConsole.MarkupLine $"[{color}]簽到作業完成，系統訊息:{text}[/]"

                        if showRecord then
                            do showMonthRecord' page (DateTime.Now.Year, DateTime.Now.Month)

                    ()

                ())
            ()



    let showMonthRecord (model: LoginInfo) (year: int, month: int) =
        Result.protect
            (fun () ->
                let systemTitle =
                    Text("===月份簽到記錄===", Style(foreground = Color.Green, decoration = Decoration.Bold))

                AnsiConsole.Write systemTitle
                AnsiConsole.WriteLine()
                let mutable page = null
                let mutable alertMessage = ""

                AnsiConsole
                    .Status()
                    .Start(
                        "正在載入...",
                        fun ctx ->
                            page <- getPlaywrightPage ()
                            ()
                    )

                page.Dialog.Add(fun dialog ->
                    if dialog.Type = "alert" then
                        alertMessage <- dialog.Message

                    dialog.AcceptAsync() |> ignore)

                let signUrl, userid, password = model.signUrl, model.userId, model.passWord

                AnsiConsole
                    .Status()
                    .Start(
                        "登入中...",
                        fun ctx ->
                            do
                                toLoginByPage
                                    page
                                    { signUrl = signUrl
                                      userId = userid
                                      passWord = password }
                    )

                if not (String.IsNullOrEmpty alertMessage) then
                    AnsiConsole.MarkupLine $"[red]登入失敗: {alertMessage}[/]"
                else
                    do showMonthRecord' page (year, month)

                    //以下拍個畫面存檔為 signbook.jpg
                    // let screenOptions = PageScreenshotOptions()
                    // screenOptions.Path <- "signbook.jpg"
                    // page.ScreenshotAsync(screenOptions).Result |> ignore
                    // AnsiConsole.MarkupLine "[red]拍照完成[/]"
                    ()


            )
            ()
