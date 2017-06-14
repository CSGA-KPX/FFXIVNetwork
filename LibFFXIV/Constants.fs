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

let FFXIVBasePacketMagic    = "5252A041FF5D46E27F2A644D7B99C475"
let FFXIVBasePacketMagicAlt = "00000000000000000000000000000000"

[<LiteralAttribute>]
let XIVItemDataSample = """{"data":{"0000105960b":{"cname":"\u4ee5\u592a\u767d\u94a2\u8170\u7532","ename":"Aetherial Steel Tassets","index":"4008","jname":"\u30a8\u30fc\u30c6\u30ea\u30a2\u30eb\u30fb\u30b9\u30c1\u30fc\u30eb\u30bf\u30bb\u30c3\u30c8","p":"1.0-1.23"}},"version":"201705020002"}"""
