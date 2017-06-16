open System
open PCap
let logger = NLog.LogManager.GetCurrentClassLogger()

let M () = 
    while FFXIV.Connections.ServerIP.Get().IsNone do
        logger.Info("没找到游戏连接，10秒后重试")
        Threading.Thread.Sleep(10 * 1000)
    PCap.Start()

let PacketTester() = 
    let random = new Random()
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
            LibFFXIV.TcpPacket.SeqNum  = seq
            LibFFXIV.TcpPacket.NextSeq = nsq
            LibFFXIV.TcpPacket.Data    = Utils.HexString.ToBytes(dat)
        }
        )
    //|> Array.sortBy (fun _ -> random.Next())
    |> Array.iter (fun x -> queue.Enqueue(x))
    queue.GetQueuedKeys()
    |> Seq.iter (printfn "Queued key : %A")
    //printfn "Queued count : %A" (queue.GetQueuedKeys())
    
let QueryRecipe(lodeId : string) = 
    let recipe = LibFFXIV.Database.SuRecipeData.Instance.GetMaterials("fddfe1ebdbc")
    if (recipe.IsSome) then
        
        ()
    else
        printfn "找不到配方"
    ()

[<EntryPoint>]
let main argv = 
    //M()
    //PacketTester()
    //printfn  "%A" (LibXIVDMF.Market.FetchMarketData(4))
    //printfn  "Test %A" (LibFFXIV.Database.ItemProvider.FromName("金币"))
    let item = "fddfe1ebdbc"
    let cutOff = 25
    let itemObj = LibFFXIV.Database.ItemProvider.FromLodeId(item)
    if (itemObj.IsSome) then
        printfn "=============%s=============" (itemObj.Value.NameChs)
        let materials = LibFFXIV.Database.SuRecipeData.Instance.GetMaterials(item)
        if materials.IsSome then
            printfn "%10s  \t%2s  %10s  %10s" "名称" "数量" "单价" "总价"
            for (material, count) in materials.Value do 
                let itemid = material.XIVDbId
                let market = LibXIVDMF.Market.FetchMarketData(itemid)
                if market.IsSome then
                    let single = LibXIVDMF.Market.GetStdEv(market.Value, cutOff)
                    let final  = single.Plus(count |> float)
                    printfn "%10s %2i %9O  %9O" (material.GetName()) count single final
                else
                    printfn "%10s %2i        暂缺       暂缺" (material.GetName()) count
                //printfn "4std  : %A"(LibXIVDMF.Market.GetStdEv(market, 25))
        else
            printfn "错误：找不到物品%s" item
        //printfn "Test %A" (LibFFXIV.Database.SuRecipeData.Instance.GetMaterials("fddfe1ebdbc"))
        printfn "===================================="
    Console.ReadLine() |> ignore


    0 // 返回整数退出代码
    