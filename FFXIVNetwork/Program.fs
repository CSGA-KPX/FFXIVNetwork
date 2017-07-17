open System
let logger = NLog.LogManager.GetCurrentClassLogger()

let Start() = 
    if RawPacketSource.PCap.IsAvailable() then
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
    