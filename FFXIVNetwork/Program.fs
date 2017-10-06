open System
let logger = NLog.LogManager.GetCurrentClassLogger()

let Start() = 
    if   RawPacketSource.Winsock.IsAvailable() then
        RawPacketSource.WinsockACT.Start()
    elif RawPacketSource.PCap.IsAvailable() then
        RawPacketSource.PCap.Start()
    else
        RawPacketSource.WinsockACT.Start()
        
[<STAThread>]
[<EntryPoint>]
let main argv = 
    AppDomain.CurrentDomain.UnhandledException.Add(fun args -> 
        let e = args.ExceptionObject :?> Exception
        logger.Fatal("UnhandledException:{0}", e.ToString())
    )
    logger.Info("正在加载数据")
    try
        LibFFXIV.Database.SaintCoinachItemProvider.GetInstance() |> ignore
        Utils.LobbyServerIP |> ignore
        Utils.LocalIPAddress |> ignore
        logger.Info("数据加载结束")
        Start()
    with
    | e -> printfn "%s" (e.ToString())
    while true do 
        Console.ReadLine() |> ignore
    0
    