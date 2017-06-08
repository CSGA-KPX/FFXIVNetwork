module LibFFXIV.Constants

type Opcodes = 
    | TradeLog = 0x00D8us
    | Market   = 0x00D3us
    | Pong     = 0x0143us

type MarketArea = 
  | LimsaLominsa = 0x0001
  | Gridania     = 0x0002
  | Uldah        = 0x0003
  | Ishgard      = 0x0004 //未确认

let FFXIVBasePacketMagic = "5252A041FF5D46E27F2A644D7B99C475"
let FFXIVBasePacketMagicAlt = "00000000000000000000000000000000"