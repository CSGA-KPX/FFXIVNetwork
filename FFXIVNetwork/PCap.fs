module PCap
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
    let clientIP = FFXIV.Connections.ServerIP.GetClient()
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
                    PacketHandler.PacketHandler(tcpPacket)
        with
        | e -> 
            printfn "%O" e
            PacketHandler.RawPacketLogger.Fatal(e, "包捕获异常终止")
        device.Close()
    else
        failwith "检测不到适合的适配器，请检查WinPcap等是否正确安装"
    