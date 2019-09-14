module Utils
open System
open System.Reflection

[<Literal>]
let WindowName = "最终幻想XIV"

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
            mon.Value.WindowName <- WindowName
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

let UploadClientData = true

module RuntimeConfig = 
    let mutable LogRawPacketData   = true
    let mutable LogGamePacket      = true
    let mutable VersionCheckPassed = false
    let mutable CurrentWorld       = 0us

    let CanUploadData() = 
        VersionCheckPassed && UploadClientData

    let IsWorldReady() = 
        CurrentWorld <> 0us

module ProcessCheck = 
    open System.Text
    open System.Diagnostics
    open System.Runtime.InteropServices

    //src : https://stackoverflow.com/posts/48319879/revisions
    [<DllImportAttribute("Kernel32.dll")>]
    extern bool internal QueryFullProcessImageName([<In>] IntPtr hProcess, [<In>] uint32 dwFlags, [<Out>] StringBuilder lpExeName, [<In>][<Out>] uint32& lpdwSize)

    let GetMainModuleFileName(p : Process) = 
        let buffer = 4096
        let sb = new StringBuilder(buffer)
        let mutable bufferLen = sb.Capacity + 1 |> uint32
        if QueryFullProcessImageName(p.Handle, 0u, sb, &bufferLen) then
            Some(sb.ToString())
        else
            None