﻿module CommonStartup

open System
open System.Diagnostics

let private logger = NLog.LogManager.GetLogger("FFXIVNetwork")
let private monitor = new Utils.FFXIVNetworkMonitorChs()

let private DoStartup(argv : string []) = 
    AppDomain.CurrentDomain.UnhandledException.Add(fun args -> 
        let e = args.ExceptionObject :?> Exception
        logger.Fatal("UnhandledException:{0}", e.ToString())
    )

    Utils.Data.ItemLookupById(1) |> ignore

    let handler = new FFXIV.PacketHandlerBase.PacketHandler()

    if Utils.Firewall.CheckWinPCap() && argv.Length = 0 then
        logger.Info("检测到WinPcap")
        monitor.MonitorType <- Machina.TCPNetworkMonitor.NetworkMonitorType.WinPCap
    else
        logger.Info("未安装WinPcap，使用RawSocket")
        Utils.Firewall.ShowDialog()
        monitor.MonitorType <- Machina.TCPNetworkMonitor.NetworkMonitorType.RawSocket

    let sent = 
        fun (id : string) (epoch : int64) (data : byte[]) ->
            handler.HandlePacketMachina(epoch, data, LibFFXIV.Network.Constants.PacketDirection.Out)
    monitor.MessageSent <- new Machina.FFXIV.FFXIVNetworkMonitor.MessageSentDelegate(sent)

    let received = 
        fun (id : string) (epoch : int64) (data : byte[]) ->
            handler.HandlePacketMachina(epoch, data, LibFFXIV.Network.Constants.PacketDirection.In)
    monitor.MessageReceived <- new Machina.FFXIV.FFXIVNetworkMonitor.MessageReceivedDelegate(received)

    monitor.Start()
    logger.Info("Machina.FFXIV已启动")

    try
        let mutable gameVerChecked = false
        let info = new Machina.ProcessTCPInfo()
        while (not gameVerChecked) do 
            let pid = info.GetProcessIDByWindowName(Utils.WindowName) |> int
            if pid <> 0 then
                let path = Utils.ProcessCheck.GetMainModuleFileName(Process.GetProcessById(pid))
                if path.IsSome then
                    logger.Info("找到游戏进程{0}，准备校验版本", pid)
                    let path = IO.Path.Combine(IO.Path.GetDirectoryName(path.Value), "ffxivgame.ver")
                    let ver = IO.File.ReadAllText(path)
                    let targetVersion = LibFFXIV.Network.Constants.TargetClientVersion
                    if ver = targetVersion then
                        logger.Info("版本校验通过，等待区域信息")
                        Utils.RuntimeConfig.VersionCheckPassed <- true
                    else
                        logger.Error("版本校验失败，本地版本为{0}，定义版本为{1}", ver, targetVersion)
                    gameVerChecked <- true
            else
                logger.Info("没有找到游戏进程")
                Threading.Thread.Sleep(5000)
    with
    | e -> logger.Error("读取游戏版本失败，禁用数据上传\r\n" + e.ToString())

let Startup(argv : string []) = 
    try
        DoStartup(argv)
    with
    | e -> logger.Error("启动失败" + e.ToString())


let Stop() = 
    monitor.Stop()
    