module FFXIV.Connections
open System
open System.Diagnostics

let FFXIVProcessList = 
    let FFXIV_PROCESS_NAME = [ "ffxiv"; "ffxiv_dx11" ]
    [
        for name in FFXIV_PROCESS_NAME do
            let pes = Process.GetProcessesByName(name)
            for p in pes do
                yield p.Id
    ]

let GetFFXIVConnections = 
    IPHelper.Functions.GetExtendedTcpTable(true,IPHelper.Win32Funcs.TcpTableType.OwnerPidAll)
    |> Seq.filter (fun c -> List.exists (fun x -> x = c.ProcessId) FFXIVProcessList )
    |> Seq.toArray

let GetFFXIVServerIPs = 
    GetFFXIVConnections
    |> Array.map (fun x -> x.RemoteEndPoint.Address.ToString())
    |> Seq.ofArray
    |> Seq.distinct
    |> Seq.head