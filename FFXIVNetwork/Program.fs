open System
open PCap
let logger = NLog.LogManager.GetCurrentClassLogger()

let M () = 
    while FFXIV.Connections.ServerIP.Get().IsNone do
        logger.Info("没找到游戏连接，10秒后重试")
        Threading.Thread.Sleep(10 * 1000)
    PCap.Start()

let PacketTester() = 
    let testFile = @"Z:\KPX\Documents\Visual Studio 2017\Projects\FFXIVNetwork\FFXIVNetwork\bin\Debug\看市场，水晶页\LoggingRawTCPPacket.txt"
    let lines    = IO.File.ReadAllLines(testFile)
    lines
    |> Array.map (fun x -> x.[50 ..])
    |> Array.map (fun x -> 
        let arr = x.Split(',')
        let seq = UInt32.Parse(arr.[0])
        let nsq = UInt32.Parse(arr.[1])
        let dat = arr.[2]
        {
            SeqNum  = seq
            NextSeq = nsq
            Data    = Utils.HexString.ToBytes(dat)
        })
    |> Array.iter (fun x -> queue.Enqueue(x))
    printfn "Queued count : %i" (queue.GetQueuedItemCount())


[<EntryPoint>]
let main argv = 
    //M()
    PacketTester()
    Console.ReadLine() |> ignore

    0 // 返回整数退出代码
    