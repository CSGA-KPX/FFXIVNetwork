open System
open PCap
let logger = NLog.LogManager.GetCurrentClassLogger()

let M () = 
    while FFXIV.Connections.ServerIP.GetServer().IsNone do
        logger.Info("没找到游戏连接，10秒后重试")
        Threading.Thread.Sleep(10 * 1000)
    let ip = FFXIV.Connections.ServerIP.GetServer().Value
    if  ip = "116.211.8.43" || ip = "116.211.8.20" then
        PCap.Start()
    else
        logger.Fatal("服务器IP错误，本应用仅限拉诺西亚使用{0}", FFXIV.Connections.ServerIP.GetServer().Value)

let PacketTester() = 
    let random = new Random()
    let testFile = @"Z:\KPX\Documents\Visual Studio 2017\Projects\FFXIVNetwork\FFXIVNetwork\bin\Debug\LoggingRawTCPPacket.txt_"
    let lines    = IO.File.ReadAllLines(testFile)
    lines
    |> Array.map (fun x -> x.[50 ..])
    |> Array.map (fun x -> 
        let arr = x.Split(',')
        let seq = UInt32.Parse(arr.[0])
        let nsq = UInt32.Parse(arr.[1])
        let dat = arr.[2]
        {
            LibFFXIV.TcpPacket.SeqNum  = seq
            LibFFXIV.TcpPacket.NextSeq = nsq
            LibFFXIV.TcpPacket.Data    = Utils.HexString.ToBytes(dat)
        }
        )
    |> Array.iter (fun x -> incomePacketQueue.Enqueue(x))
    incomePacketQueue.GetQueuedKeys()
    |> Seq.iter (printfn "Queued key : %A")
    printfn "Queued count : %A" (incomePacketQueue.GetQueuedKeys())
    

[<EntryPoint>]
let main argv = 
    
    
    PcapDotNet.Core.LivePacketDevice.AllLocalMachine.Count |> ignore
    AppDomain.CurrentDomain.UnhandledException.Add(fun args -> 
        let e = args.ExceptionObject :?> Exception
        NLog.LogManager.GetCurrentClassLogger().Fatal("UnhandledException:{0}", e.ToString())
    )

    M()
    //PacketTester()
    while true do 
        Console.ReadLine() |> ignore
    0 // 返回整数退出代码
    