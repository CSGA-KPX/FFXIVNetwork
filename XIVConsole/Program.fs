// 在 http://fsharp.org 上了解有关 F# 的详细信息
// 请参阅“F# 教程”项目以获取更多帮助。
open System
open System.Windows.Forms


[<STAThread>]
[<EntryPoint>]
let main argv = 
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    Application.Run(new UI.MainForm());

    //let item = LibFFXIV.Database.SuItemData.Instance.FromName("加隆德大地皮帽")
    //let item = LibFFXIV.Database.ItemProvider.TryGetItem("加隆德大地皮帽")
    //let ma = LibFFXIV.Database.SuRecipeData.Instance.GetMaterialsRecGroup(item.Value)
    //printfn "%A" ma
    //Console.ReadLine() |> ignore
    0 // 返回整数退出代码
