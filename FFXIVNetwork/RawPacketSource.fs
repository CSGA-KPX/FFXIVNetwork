module RawPacketSource
module PCapFile = 
    open System
    open SharpPcap.LibPcap

module PCap = 
    open LibFFXIV.Constants
    open LibFFXIV.TcpPacket
    open SharpPcap
    open PacketDotNet

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
                let isActive   = x.ToString().Contains(clientIP.Value)
                isActive)        
            |> Seq.mapi (fun i x -> 
                printfn "可用适配器%i %O" i x
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
    open NetFwTypeLib

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

    let StartSocketSniff() = 
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
        UAC.IsAdministrator()


    let CheckFirewallStatus () = 
        let mgr = 
            let t = Type.GetTypeFromProgID("HNetCfg.FwMgr", false)
            Activator.CreateInstance(t) :?> INetFwMgr
        let appList = mgr.LocalPolicy.CurrentProfile.AuthorizedApplications
        printfn ">%s<" (Diagnostics.Process.GetCurrentProcess().MainModule.FileName)
    
        for app in appList do 
            let app = app :?> INetFwAuthorizedApplication
            printfn "%s" app.ProcessImageFileName
    
    let Start () = 
        if IsAvailable() then
            NLog.LogManager.GetCurrentClassLogger().Info("开始使用RawSocket抓包，请确定已添加防火墙例外")
            StartSocketSniff()
        else
            CheckFirewallStatus()
            NLog.LogManager.GetCurrentClassLogger().Info("RawSocket抓包需要提升权限，请按任意键继续")
            Console.ReadKey(true) |> ignore
            UAC.RestartWithUAC()
