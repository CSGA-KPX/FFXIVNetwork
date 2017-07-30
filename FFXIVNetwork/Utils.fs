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
    (*
    let host = Dns.GetHostEntry(Dns.GetHostName())
    host.AddressList
    |> Seq.filter (fun ip -> ip.AddressFamily = AddressFamily.InterNetwork)
    |> Seq.map (fun x -> 
        let ip = x.ToString()
        logger.Info("找到本地IP:{0}", ip)
        ip
    )
    |> Seq.head*)

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

module FirewallWarpper = 
    open System
    open NetFwTypeLib

    let logger = NLog.LogManager.GetCurrentClassLogger()
    let inline notNull x = not (isNull x)
    let appName = "FFXIVNetwork"

    let netFwMgr = 
        let t = Type.GetTypeFromProgID("HNetCfg.FwMgr", false)
        Activator.CreateInstance(t) :?> INetFwMgr

    let IsFirewallDisabled() = 
        (notNull netFwMgr) && (not (netFwMgr.LocalPolicy.CurrentProfile.FirewallEnabled))

    let IsFirewallApplicationConfigured() = 
        if notNull netFwMgr then
            let authorized = netFwMgr.LocalPolicy.CurrentProfile.AuthorizedApplications
            if isNull authorized then
                false
            else
                authorized
                |> Seq.cast<INetFwAuthorizedApplication>
                |> Seq.exists (fun x -> 
                    (notNull x) && (x.Name = appName) && (x.Enabled))
        else
            false
    let IsFirewallRuleConfigured() = 
        let policy = Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2")) :?> INetFwPolicy2
        if notNull policy then
            let rules = policy.Rules
            if isNull rules then
                false
            else
                rules
                |> Seq.cast<INetFwRule2>
                |> Seq.exists (fun x -> 
                    (notNull x) && (x.Name = appName) && (x.Enabled) && (x.Protocol = 6)
                )
        else
            false

    let AddFirewallApplicationEntry() = 
        try
            if isNull netFwMgr then
                failwith "无法访问防火墙"
            let rule = 
                let t = Type.GetTypeFromProgID("HNetCfg.FwAuthorizedApplication", false)
                Activator.CreateInstance(t) :?> INetFwAuthorizedApplication
            if isNull rule then
                failwith "无法创建防火墙规则(1)"
            rule.Enabled <- true
            rule.IpVersion <- NET_FW_IP_VERSION_.NET_FW_IP_VERSION_ANY
            rule.Name <- "Advanced Combat Tracker"
            rule.ProcessImageFileName <- System.Reflection.Assembly.GetExecutingAssembly().Location
            rule.Scope <- NET_FW_SCOPE_.NET_FW_SCOPE_ALL
            netFwMgr.LocalPolicy.CurrentProfile.AuthorizedApplications.Add(rule)
        with
        | e -> logger.Info("创建防火墙规则失败" + (e.ToString()))