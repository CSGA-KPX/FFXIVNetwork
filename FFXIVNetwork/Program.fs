open System


let logger = NLog.LogManager.GetCurrentClassLogger()

        
[<STAThread>]
[<EntryPoint>]
let main argv = 
    AppDomain.CurrentDomain.UnhandledException.Add(fun args -> 
        let e = args.ExceptionObject :?> Exception
        logger.Fatal("UnhandledException:{0}", e.ToString())
    )
    logger.Info("正在加载数据")
    try
        LibFFXIV.Client.Item.SaintCoinachItemProvider.GetInstance() |> ignore
        logger.Info("数据加载结束")

        if LibFFXIV.Client.ClientInfo.ClientVersion <> LibFFXIV.Network.Constants.TargetClientVersion then
            Utils.UploadClientData <- false
            printfn "客户端版本与数据包定义版本不匹配，已禁用数据上传"
            printfn "当前版本 %s" LibFFXIV.Client.ClientInfo.ClientVersion


        let handler = new FFXIV.PacketHandlerBase.PacketHandler()
        let monitor = new Machina.FFXIV.FFXIVNetworkMonitor()

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

    with
    | e -> printfn "%s" (e.ToString())
    while true do 
        Console.ReadLine() |> ignore
    0
    