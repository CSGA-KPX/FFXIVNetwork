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
    let mutable clientIP = ""
    let mutable lastRefreshTimeClient : DateTime = DateTime.Now
    let mutable lastRefreshTimeServer : DateTime = DateTime.Now
    let isExpried () = 
        let Timer = 10
        let A = String.IsNullOrEmpty(serverIP)
        let B = (DateTime.Now - lastRefreshTimeServer).Seconds > Timer
        A || B

    let GetClient() = 
        infoLock.EnterUpgradeableReadLock()
        let ret = 
            if clientIP = "" || isExpried() then
                let cons = 
                    getXIVConnections()
                    |> Array.map (fun x -> x.LocalEndPoint.Address.ToString())
                    |> Seq.ofArray
                    |> Seq.distinct
                if (Seq.length cons) = 0 then
                    None
                else
                    infoLock.EnterWriteLock()
                    clientIP <- Seq.head cons
                    lastRefreshTimeClient <- DateTime.Now
                    infoLock.ExitWriteLock()
                    Some(clientIP)
            else
                Some(clientIP)
        infoLock.ExitUpgradeableReadLock()
        ret
        

    let GetServer() = 
        infoLock.EnterUpgradeableReadLock()
        let ret = 
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
                    lastRefreshTimeServer <- DateTime.Now
                    infoLock.ExitWriteLock()
                    Some(serverIP)
            else
                Some(serverIP)
        infoLock.ExitUpgradeableReadLock()
        ret

