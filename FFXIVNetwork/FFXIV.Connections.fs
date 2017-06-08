module FFXIV.Connections
open System
open System.Threading
open System.Diagnostics

let internal getXIVProcessList() = 
    let FFXIV_PROCESS_NAME = [ "ffxiv"; "ffxiv_dx11" ]
    [
        for name in FFXIV_PROCESS_NAME do
            let pes = Process.GetProcessesByName(name)
            for p in pes do
                yield p.Id
    ]

let internal getXIVConnections() = 
    IPHelper.Functions.GetExtendedTcpTable(true,IPHelper.Win32Funcs.TcpTableType.OwnerPidAll)
    |> Seq.filter (fun c -> List.exists (fun x -> x = c.ProcessId) (getXIVProcessList()) )
    |> Seq.toArray


module ServerIP = 
    let infoLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion)
    let mutable serverIP = ""
    let mutable lastRefreshTime : DateTime = DateTime.Now
    let isExpried () = 
        let Timer = 10
        let A = String.IsNullOrEmpty(serverIP)
        let B = (DateTime.Now - lastRefreshTime).Seconds > Timer
        A || B


    let Get() = 
        infoLock.EnterUpgradeableReadLock()
        if isExpried() then
            let cons = 
                getXIVConnections()
                |> Array.map (fun x -> x.RemoteEndPoint.Address.ToString())
                |> Seq.ofArray
                |> Seq.distinct
            if (Seq.length cons) = 0 then
                None
            else
                infoLock.EnterWriteLock()
                serverIP <- Seq.head cons
                lastRefreshTime <- DateTime.Now
                infoLock.ExitWriteLock()
                Some(serverIP)
        else
            Some(serverIP)
