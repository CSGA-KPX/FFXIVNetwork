module Utils
open System
open System.Net
open System.Net.Sockets
let logger = NLog.LogManager.GetCurrentClassLogger()

type HexString = LibFFXIV.Utils.HexString

let DictionaryAddOrUpdate (dict : Collections.Generic.Dictionary<_,_> , key, value) = 
    if dict.ContainsKey(key) then
        dict.[key] <- value
    else
        dict.Add(key, value)

let LobbyServerIP = 
    Net.Dns.GetHostAddresses("ffxivlobby01.ff14.sdo.com").[0].ToString()

let LocalIPAddress = 
    use socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.IP)
    socket.Connect(LobbyServerIP, 80)
    let ep = socket.LocalEndPoint :?> IPEndPoint
    let ip = ep.Address.ToString()
    logger.Info("找到本地IP:{0}", ip)
    ip

module UAC = 
    open System
    open System.Diagnostics
    open System.Security.Principal

    let IsAdministrator() = 
        let identity = WindowsIdentity.GetCurrent()
        let principal = new WindowsPrincipal(identity)
        principal.IsInRole(WindowsBuiltInRole.Administrator)

    let rec RestartWithUAC() = 
        let exeFile = Process.GetCurrentProcess().MainModule.FileName
        let psi = new ProcessStartInfo(exeFile)
        psi.UseShellExecute <- true
        psi.WorkingDirectory <- Environment.CurrentDirectory
        psi.Verb <- "runas"
        try
            Process.Start(psi) |> ignore
        with
        | _ -> RestartWithUAC()
        Environment.Exit(0)
