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
        LibFFXIV.Client.Database.SaintCoinachItemProvider.GetInstance() |> ignore
        logger.Info("数据加载结束")

        let handler = new FFXIV.PacketHandlerBase.PacketHandler()
        let monitor = new Machina.FFXIV.FFXIVNetworkMonitor()

        monitor.MonitorType <- Machina.TCPNetworkMonitor.NetworkMonitorType.WinPCap

        let sent = 
            fun (id : string) (epoch : int64) (data : byte[]) ->
                handler.HandlePacketMachina(epoch, data, LibFFXIV.Network.TcpPacket.PacketDirection.Out)
        monitor.MessageSent <- new Machina.FFXIV.FFXIVNetworkMonitor.MessageSentDelegate(sent)

        let received = 
            fun (id : string) (epoch : int64) (data : byte[]) ->
                handler.HandlePacketMachina(epoch, data, LibFFXIV.Network.TcpPacket.PacketDirection.In)
        monitor.MessageReceived <- new Machina.FFXIV.FFXIVNetworkMonitor.MessageReceivedDelegate(received)



        monitor.Start()
        logger.Info("Machina.FFXIV已启动")

    with
    | e -> printfn "%s" (e.ToString())
    while true do 
        Console.ReadLine() |> ignore
    0
    