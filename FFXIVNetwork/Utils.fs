module Utils
open System
open System.Reflection

let logger = NLog.LogManager.GetCurrentClassLogger()

type HexString = LibFFXIV.Network.Utils.HexString

let mutable UploadClientData = true

let mutable LogRawPacketData = true
let mutable LogGamePacket    = true



type FFXIVNetworkMonitorChs() = 
    inherit Machina.FFXIV.FFXIVNetworkMonitor() 
    
    let mutable mon : Machina.TCPNetworkMonitor option = None

    member x.Start() =
        if mon.IsSome then
            mon.Value.Stop()
            mon <- None
        if isNull x.MessageReceived then
            raise <| new ArgumentException("MessageReceived delegate must be specified.")
        mon <- Some( new Machina.TCPNetworkMonitor() )
        mon.Value.ProcessID <- x.ProcessID
        if mon.Value.ProcessID = 0u then
            mon.Value.WindowName <- "最终幻想XIV"
        mon.Value.MonitorType <- x.MonitorType
        mon.Value.LocalIP <- x.LocalIP
        mon.Value.UseSocketFilter <- x.UseSocketFilter

        mon.Value.DataSent <- new Machina.TCPNetworkMonitor.DataSentDelegate(fun a b -> x.ProcessSentMessage(a,b))
        mon.Value.DataReceived <- new Machina.TCPNetworkMonitor.DataReceivedDelegate(fun a b -> x.ProcessReceivedMessage(a, b))

        mon.Value.Start()

    member x.Stop() = 
        mon.Value.DataSent <- null
        mon.Value.DataReceived <- null
        mon.Value.Stop()
        mon <- None

        let sent = x.GetType().GetField("_sentDecoders", BindingFlags.NonPublic ||| BindingFlags.Instance)
        let recv = x.GetType().GetField("_receivedDecoders", BindingFlags.NonPublic ||| BindingFlags.Instance)

        sent.SetValue(x, box(null))
        recv.SetValue(x, box(null))


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
