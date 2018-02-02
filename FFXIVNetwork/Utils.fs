module Utils
open System
open System.Net
open System.Net.Sockets
let logger = NLog.LogManager.GetCurrentClassLogger()

type HexString = LibFFXIV.Network.Utils.HexString

module UAC = 
    open System
    open System.Diagnostics
    open System.Security.Principal

    let IsAdministrator() = 
        let identity = WindowsIdentity.GetCurrent()
        let principal = new WindowsPrincipal(identity)
        principal.IsInRole(WindowsBuiltInRole.Administrator)

    let rec RestartWithUAC() = 
        let exeFile = Process.GetCurrentProcess().MainModule.FileName
        let psi = new ProcessStartInfo(exeFile)
        psi.UseShellExecute <- true
        psi.WorkingDirectory <- Environment.CurrentDirectory
        psi.Verb <- "runas"
        try
            Process.Start(psi) |> ignore
        with
        | _ -> RestartWithUAC()
        Environment.Exit(0)
