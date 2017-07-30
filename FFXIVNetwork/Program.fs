open System
let logger = NLog.LogManager.GetCurrentClassLogger()

let Start() = 
    if   RawPacketSource.Winsock.IsAvailable() then
        RawPacketSource.Winsock.Start()
    elif RawPacketSource.PCap.IsAvailable() then
        RawPacketSource.PCap.Start()
    else
        RawPacketSource.Winsock.Start()
        

[<EntryPoint>]
let main argv = 
    AppDomain.CurrentDomain.UnhandledException.Add(fun args -> 
        let e = args.ExceptionObject :?> Exception
        logger.Fatal("UnhandledException:{0}", e.ToString())
    )
    logger.Info("正在加载数据")
    LibFFXIV.Database.ItemProvider |> ignore
    Utils.LobbyServerIP |> ignore
    let world = 
        {LibFFXIV.SpecializedPacket.World.WorldId   = 0x412us
         LibFFXIV.SpecializedPacket.World.WorldName = "拉诺西亚"}
    GlobalVars.WorldsIdToWorld.Add(0x412us, world)
    GlobalVars.ServerIpToWorld.Add("116.211.8.43", world)
    //一些预定义的数据

    logger.Info("数据加载结束")
    try
        Utils.LocalIPAddress |> ignore
        Start()
    with
    | e -> printfn "%s" (e.ToString())
    while true do 
        Console.ReadLine() |> ignore
    0
    