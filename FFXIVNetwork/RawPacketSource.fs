module RawPacketSource
module PCapFile = 
    open System
    open SharpPcap.LibPcap

module PCap = 
    open LibFFXIV.Constants
    open LibFFXIV.TcpPacket
    open SharpPcap
    open PacketDotNet
    let logger = NLog.LogManager.GetCurrentClassLogger()

    let IsAvailable () = 
        try
            CaptureDeviceList.Instance |> ignore
            true
        with
        | e -> false
    

    let Start() = 
        let clientIP = Some (Utils.LocalIPAddress)
        let devices = 
            CaptureDeviceList.Instance
            |> Seq.filter (fun x ->
                logger.Trace("已知适配器{0}" ,x)
                let isActive   = x.ToString().Contains(clientIP.Value)
                isActive)        
            |> Seq.mapi (fun i x -> 
                logger.Info("可用适配器{0}, {1}",i ,x)
                x)
            |> Seq.toArray

        let device = devices |> Array.tryHead
        if device.IsSome then
            let device = device.Value
            device.Open(DeviceMode.Promiscuous, 1000)
            device.Filter <- "ip and tcp"
            try
                while true do 
                    let rawpacket = device.GetNextPacket()
                    if isNull rawpacket then
                        ()
                    else
                        let packet    = Packet.ParsePacket(rawpacket.LinkLayerType, rawpacket.Data)
                        let tcpPacket = packet.Extract(typeof<TcpPacket>) :?> TcpPacket
                        RawPacketHandler.PacketHandler(tcpPacket)
            with
            | e -> 
                printfn "%O" e
                RawPacketHandler.RawPacketLogger.Fatal(e, "包捕获异常终止")
            device.Close()
        else
            failwith "检测不到适合的适配器，请检查WinPcap等是否正确安装"
    
module Winsock = 
    open System
    open System.Net
    open System.Net.Sockets
    open PacketDotNet

    let StartSocketSniff() = 
        NLog.LogManager.GetCurrentClassLogger().Info("开始使用RawSocket抓包")
        let s = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP)
        s.Bind(new IPEndPoint(IPAddress.Parse(Utils.LocalIPAddress), 0))
        s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AcceptConnection, true)
        let byTrue = [| 3uy; 0uy; 0uy; 0uy |]
        let byOut = [| 0uy; 0uy; 0uy; 0uy |]
        s.IOControl(IOControlCode.ReceiveAll, byTrue, byOut) |> ignore
        s.ReceiveBufferSize <- 512000
        let buf = Array.zeroCreate<byte> 131072
        try
            while true do
                //let size = s.Re
                let size = s.Receive(buf)
                let data = buf.[0 .. size - 1]
                if data.[9] = 6uy then
                    let  p = Packet.ParsePacket(LinkLayers.Raw, data)
                    let tcp = p.Extract(typeof<TcpPacket>) :?> TcpPacket
                    RawPacketHandler.PacketHandler(tcp)
        with
        | e -> printfn "%s" (e.ToString())
        ()

    let IsAvailable () = 
        Utils.UAC.IsAdministrator()
    
    let Start () = 
        if IsAvailable() then
            StartSocketSniff()
        else
            NLog.LogManager.GetCurrentClassLogger().Info("RawSocket抓包需要提升权限，请按任意键继续")
            Console.ReadKey(true) |> ignore
            Utils.UAC.RestartWithUAC()

module WinsockACT = 
    open System
    open System.Threading
    open System.Diagnostics
    open System.Net.NetworkInformation
    open System.Timers

    let logger = NLog.LogManager.GetCurrentClassLogger()

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
        |> Seq.filter (fun c ->
            //排除大厅服务器连接
            let isLobby = c.RemoteEndPoint.Address.ToString().Contains(Utils.LobbyServerIP)
            not isLobby
        )
        |> Seq.toArray

    let RefreshWorld() = 
        let connections = getXIVConnections()
        for c in connections do 
            let addr  = c.RemoteEndPoint.Address.ToString()
            let world = {LibFFXIV.SpecializedPacket.World.WorldId = 65534us; LibFFXIV.SpecializedPacket.World.WorldName = "ACT"}
            if not (GlobalVars.ServerIpToWorld.ContainsKey(addr)) then
                lock (GlobalVars.ServerIpToWorld) (fun () -> GlobalVars.ServerIpToWorld.Add(addr, world))
        //logger.Info(sprintf "活动连接：%A" GlobalVars.ServerIpToWorld)

    let Start() = 
        RefreshWorld()
        let timer = new System.Timers.Timer(10000.0)
        timer.Elapsed.Add(fun _ -> RefreshWorld())
        timer.Enabled <- true
        GC.KeepAlive(timer)

        Winsock.Start()
