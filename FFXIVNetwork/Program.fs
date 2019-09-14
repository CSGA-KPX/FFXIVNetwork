open System
open System.Diagnostics

let logger = NLog.LogManager.GetLogger("FFXIVNetwork")

        
[<STAThread>]
[<EntryPoint>]
let main argv = 
    AppDomain.CurrentDomain.UnhandledException.Add(fun args -> 
        let e = args.ExceptionObject :?> Exception
        logger.Fatal("UnhandledException:{0}", e.ToString())
    )
    logger.Info("正在加载数据")
    try
        logger.Info("加载LibFFXIV.ClientData，数据定义为{0}", LibFFXIV.ClientData.TargetVersion.Version)

        let handler = new FFXIV.PacketHandlerBase.PacketHandler()
        let monitor = new Utils.FFXIVNetworkMonitorChs()

        monitor.MonitorType <- Machina.TCPNetworkMonitor.NetworkMonitorType.WinPCap

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
                        if ver = LibFFXIV.ClientData.TargetVersion.Version then
                            logger.Info("版本校验通过，启用数据上传")
                            Utils.RuntimeConfig.VersionCheckPassed <- true
                        else
                            logger.Error("版本校验失败，本地版本为{0}，定义版本为{1}", ver, LibFFXIV.ClientData.TargetVersion.Version)
                        gameVerChecked <- true
                else
                    logger.Info("没有找到游戏进程")
                    Threading.Thread.Sleep(5000)
        with
        | e -> logger.Error("读取游戏版本失败，禁用数据上传\r\n" + e.ToString())
    with
    | e -> logger.Error(e, "启动失败")
    while true do 
        let line = Console.ReadLine()
        if line = "1042" then
            Utils.RuntimeConfig.CurrentWorld <- 1042us

    0
    