module LibFFXIV.Network.Constants

type Opcodes = 
    | None        = 0xFFFFus
    | TradeLogInfo = 0x0121us // 4.3
    | TradeLogData = 0x0127us // 4.3
    | Market       = 0x0123us // 4.3
    | MarketList   = 0x012Aus // 4.3
    | MarketListRequest = 0x0103us // 4.3
    | CharacterNameLookupReply = 0x0189us // 4.3
    | Chat         = 0x00F7us // 4.3
    | LinkshellList = 0x00FEus // 4.3

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
  | Kugane       = 0x0007

let TargetClientVersion     = "2018.11.26.0000.0000"

type PacketDirection = 
    | In   = 0
    | Out  = 1