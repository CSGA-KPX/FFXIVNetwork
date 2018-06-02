module LibFFXIV.Network.Constants

type Opcodes = 
    | None        = 0xFFFFus
    | TradeLogInfo = 0x00E1us // Not used
    | TradeLogData = 0x0110us // 4.2
    | Market       = 0x010Cus // 4.2
    | MarketList   = 0x0113us // 4.2
    | CharacterNameLookupReply = 0x0173us // 4.2
    | Chat         = 0x00E1us // 4.2

type PacketTypes = 
    | None             = 0x0000us
    | ClientHelloWorld = 0x0001us
    | ServerHelloWorld = 0x0002us
    | GameMessage      = 0x0003us
    | KeepAliveRequest = 0x0007us
    | KeepAliveResponse= 0x0008us
    | ClientHandShake  = 0x0009us
    | ServerHandShake  = 0x000Aus       

type MarketArea = 
  | LimsaLominsa = 0x0001
  | Gridania     = 0x0002
  | Uldah        = 0x0003
  | Ishgard      = 0x0004
  | Kugane       = 0x0005


let FFXIVBasePacketMagic    = "5252A041FF5D46E27F2A644D7B99C475"
let FFXIVBasePacketMagicAlt = "00000000000000000000000000000000"
let TargetClientVersion     = "2018.05.17.0000.0000"

type PacketDirection = 
    | In   = 0
    | Out  = 1