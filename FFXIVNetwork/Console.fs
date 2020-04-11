module ConsoleStartup

open System
open NLog

[<STAThread>]
[<EntryPoint>]
let main argv = 
    let gamePacket = new Targets.FileTarget("GamePacket")
    let mainLogging = new Targets.FileTarget("MainLogging")
    let parserLog  = new Targets.FileTarget("Parser")
    let logConsole = new Targets.ConsoleTarget("console")

    gamePacket.FileName <- Layouts.Layout.op_Implicit "Logging_GamePacket.txt"
    mainLogging.FileName <- Layouts.Layout.op_Implicit "Logging_Main.txt"
    parserLog.FileName <- Layouts.Layout.op_Implicit "Logging_Parser.txt"

    gamePacket.DeleteOldFileOnStartup <- true
    mainLogging.DeleteOldFileOnStartup <- true
    parserLog.DeleteOldFileOnStartup <- true

    gamePacket.KeepFileOpen <- true
    mainLogging.KeepFileOpen <- true
    parserLog.KeepFileOpen <- true


    let config = Config.LoggingConfiguration()
    config.AddRule(LogLevel.Info, LogLevel.Fatal, logConsole)
    config.AddRule(LogLevel.Trace, LogLevel.Fatal, gamePacket, "FFXIV.PacketHandlerBase*", true)
    config.AddRule(LogLevel.Trace, LogLevel.Fatal, parserLog, "Parser:*", true)
    config.AddRule(LogLevel.Trace, LogLevel.Fatal, mainLogging)

    LogManager.Configuration <- config
    CommonStartup.Startup(argv)

    while true do 
        let line = Console.ReadLine()
        if line = "1042" then
            Utils.RuntimeConfig.CurrentWorld <- 1042us

    0
    