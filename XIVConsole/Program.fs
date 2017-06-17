// 在 http://fsharp.org 上了解有关 F# 的详细信息
// 请参阅“F# 教程”项目以获取更多帮助。
open System
open System.Windows.Forms


[<EntryPoint>]
let main argv = 
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    Application.Run(new UI.MainForm());
    (*
    REPL() 

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
    Console.ReadLine() |> ignore*)
    0 // 返回整数退出代码
