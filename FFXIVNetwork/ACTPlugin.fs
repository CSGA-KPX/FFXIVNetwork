module ACTPlugin

open System
open System.Windows.Forms
open Advanced_Combat_Tracker
open NLog


type FFXIVNetworkPlugin () = 
    inherit UserControl()
    let tb = new TextBox()

    member private x.ConfigNLog() = 
        let config = Config.LoggingConfiguration()
        
        let methodTarget = new Targets.MethodCallTarget("WinFormTarget", (fun e objs -> 
            let msg = objs.[0].ToString() + Environment.NewLine
            if ActGlobals.oFormActMain.InvokeRequired then
                ActGlobals.oFormActMain.Invoke(Action(fun () -> tb.AppendText(msg))) |> ignore
            else
                tb.AppendText(msg)
        ))
        let param = Targets.MethodCallParameter()
        param.Layout <- Layouts.SimpleLayout("${longdate}|${level:uppercase=true}|${logger}|${message}")
        methodTarget.Parameters.Add(param)
        
        
        config.AddRule(LogLevel.Info, LogLevel.Fatal, methodTarget)

        LogManager.Configuration <- config

    override x.CreateParams = 
        let WS_EX_COMPOSITED = 0x02000000
        let cp = base.CreateParams
        cp.ExStyle <- cp.ExStyle ||| WS_EX_COMPOSITED
        cp

    interface IActPluginV1 with
        member x.DeInitPlugin() =
            tb.Dispose()
            LogManager.Shutdown()
            CommonStartup.Stop()

        member x.InitPlugin(tabPage, label) =
            tb.Font <- new Drawing.Font("Consolas", 10.0f)
            tb.Multiline <- true
            tb.ScrollBars <- ScrollBars.Vertical
            tb.TabIndex <- 0

            tb.Anchor <- AnchorStyles.Left ||| AnchorStyles.Top
            tb.Dock <- DockStyle.Fill

            x.Anchor <- AnchorStyles.Left ||| AnchorStyles.Top
            x.Dock <- DockStyle.Fill

            x.Controls.Add(tb)

            x.ConfigNLog()

            x.DoubleBuffered <- true

            tabPage.Controls.Add(x)
            Threading.Tasks.Task.Run(fun () -> CommonStartup.Startup(Array.empty)) |> ignore