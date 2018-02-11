module LibFFXIV.Client.Utils
open System
open System.Configuration
open System.IO

let internal TryGetToOption (x : bool, y: 'Value) = 
    if x then
        Some(y)
    else
        None

type ClientSetting private () = 
    inherit ApplicationSettingsBase()

    static let instance = new ClientSetting()
    static member Instance = instance

    [<UserScopedSetting>]
    [<DefaultSettingValue("")>]
    member this.XIVGamePath
        with get() = this.Item("XIVGamePath") :?> string
        and set(value : string) = this.Item("XIVGamePath") <- value

let internal IsXIVGamePath (path) = 
    IO.File.Exists(path + @"\game\ffxiv.exe")

let rec internal GetXIVGamePathUI () = 
        use d = new System.Windows.Forms.FolderBrowserDialog()
        d.Description <- "请选择FF14游戏路径（包含game文件夹的）"
        d.ShowNewFolderButton <- false

        let result = d.ShowDialog()
        let isOK = 
            result = System.Windows.Forms.DialogResult.OK
            && IsXIVGamePath(d.SelectedPath)
        if isOK then 
            try
                ClientSetting.Instance.XIVGamePath <- d.SelectedPath
                ClientSetting.Instance.Save()
            with
            | e -> printfn "%O" e
            d.SelectedPath
        else
            GetXIVGamePathUI()

let GetXIVGamePath() = 
    if IsXIVGamePath(ClientSetting.Instance.XIVGamePath) then
        ClientSetting.Instance.XIVGamePath
    else
        GetXIVGamePathUI()

type SaintCoinachInstance private() = 
    static let gameDirectory = GetXIVGamePath()
    static let instance = 
            SaintCoinach.ARealmReversed(gameDirectory, SaintCoinach.Ex.Language.ChineseSimplified)
    static member Instance = instance