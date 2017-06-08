open System
let logger = NLog.LogManager.GetCurrentClassLogger()

[<EntryPoint>]
let main argv = 
    while FFXIV.Connections.ServerIP.Get().IsNone do
        logger.Info("没找到游戏连接，10秒后重试")
        Threading.Thread.Sleep(10 * 1000)
    PCap.Start()

    Console.ReadLine() |> ignore

    0 // 返回整数退出代码
    