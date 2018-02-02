module LibFFXIV.Network.Constants

type Opcodes = 
    | TradeLogInfo = 0x00E1us 
    | TradeLogData = 0x00E6us 
    | Market       = 0x00E2us 
    | MarketList   = 0x00E9us
    | Pong         = 0x0143us //Not updated
    | WorldList    = 0x0015us //Not updated
    | CharaList    = 0x000Dus //Not updated
    | SelectCharaReply = 0x000Fus //Not updated

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