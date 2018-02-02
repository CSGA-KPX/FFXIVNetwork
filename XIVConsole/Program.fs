// 在 http://fsharp.org 上了解有关 F# 的详细信息
// 请参阅“F# 教程”项目以获取更多帮助。
open System
open System.Windows.Forms

[<STAThread>]
[<EntryPoint>]
let main argv = 
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    //必须提前调用并写入配置文件，后期调用时线程不是STA会崩溃
    LibFFXIV.Client.Utils.GetXIVGamePath() |> ignore
    Application.Run(new UI.MainForm());
    0 // 返回整数退出代码
