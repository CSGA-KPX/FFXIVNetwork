module Utils
open System
open System.Collections.Generic
open System.Reflection
open Machina.FFXIV


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
        mon <- Some(Machina.TCPNetworkMonitor())
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

let UploadClientData = true

module RuntimeConfig = 
    let logger = NLog.LogManager.GetCurrentClassLogger()

    let mutable LogGamePacket      = true
    let mutable VersionCheckPassed = false
    let mutable CurrentWorld       = 0us

    let PrintStatus() = 
        if not VersionCheckPassed  then
            logger.Warn("无法提交数据：版本检查失败")
        if CurrentWorld = 0us then
            logger.Warn("无法提交数据：区域不明")

    let CanUploadData() = 
        PrintStatus()
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

module Data = 
    open LibFFXIV.GameData.Raw
    let private col = EmbeddedXivCollection(XivLanguage.ChineseSimplified) :> IXivCollection
    let private items = 
        seq {
            NLog.LogManager.GetCurrentClassLogger().Info("正在从LibFFXIV.GameData.Raw解析数据")
            let sht = col.GetSheet("Item", [|"Name"|])
            for row in sht do 
                yield row.Key.Main, row.As<string>("Name")
        } |> readOnlyDict

    let ItemLookupById(id) = 
        if items.ContainsKey(id) then
            Some (items.[id])
        else
            None

module Firewall = 
    open System.Net
    open System.Runtime.InteropServices

    let ShowDialog() = 
        let ep = new IPEndPoint(0L, 65500)
        let ll = new Sockets.TcpListener(ep)
        ll.Start()
        ll.Stop()

    [<DllImport("kernel32", SetLastError=true)>]
    extern IntPtr LoadLibrary(string lpFileName)

    let CheckWinPCap() = 
        let dllName = "wpcap.dll"
        if LoadLibrary(dllName) = IntPtr.Zero then
            false
        else
            true