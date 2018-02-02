module LibFFXIV.Client.Utils
open System
open System.Configuration
open System.IO
open System.Text

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
                let file = ConfigurationManager.OpenExeConfiguration(System.Windows.Forms.Application.ExecutablePath)
                let cfg  = file.AppSettings.Settings
                cfg.Remove("XIVGAMEPATH")
                cfg.Add("XIVGAMEPATH", d.SelectedPath)
                file.Save(ConfigurationSaveMode.Modified)
                ConfigurationManager.RefreshSection(file.AppSettings.SectionInformation.Name)
            with
            | e -> printfn "%O" e
            d.SelectedPath
        else
            GetXIVGamePathUI()

///For fsi use
let mutable XIVGamePath = @""

let GetXIVGamePath() = 
    if IsXIVGamePath(XIVGamePath) then
        XIVGamePath
    else
        let key = "XIVGAMEPATH"
        let file = ConfigurationManager.OpenExeConfiguration(System.Windows.Forms.Application.ExecutablePath)
        let cfg  = file.AppSettings.Settings
        let path = cfg.[key]
        let isOK = (not (isNull path)) && IsXIVGamePath(path.Value)
        if isOK then
            path.Value
        else
            GetXIVGamePathUI()
    