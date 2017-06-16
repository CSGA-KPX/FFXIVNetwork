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
let XIVItemDataSample = """
{
    "data":{
        "0000105960b":{
            "cname":"\u4ee5\u592a\u767d\u94a2\u8170\u7532",
            "ename":"Aetherial Steel Tassets",
            "index":"4008",
            "jname":"\u30a8\u30fc\u30c6\u30ea\u30a2\u30eb\u30fb\u30b9\u30c1\u30fc\u30eb\u30bf\u30bb\u30c3\u30c8",
            "p":"1.0-1.23"
        },
        "0005a39bebc": {
            "cname": "\u4e9a\u5386\u5c71\u5927\u539f\u578b\u7cbe\u51c6\u624b\u5957",
            "ename": "Prototype Alexandrian Gloves of Aiming",
            "index": 16423,
            "jname": "\u30d7\u30ed\u30c8\u30a2\u30ec\u30ad\u30fb\u30ec\u30f3\u30b8\u30e3\u30fc\u30b0\u30ed\u30fc\u30d6",
            "p": "3.4"
        }
    },
    "version":"201705020002"
}"""

[<LiteralAttribute>]
let XIVRecipseDataSample = """
{
    "data": {
        "0004d2ae0c2": [
            {
                "a_at": "风",
                "a_ed": "255",
                "a_flg": 3,
                "a_ot": "1,2,3",
                "a_qc": "469",
                "a_wk": "123",
                "category": "\u5f6b\u91d1\u5e2b",
                "dur": "80",
                "lv": "50",
                "lv_num": 50.1,
                "mark": "\u2605",
                "material": [
                    {
                        "count": "1",
                        "id": "be818e7f7e6"
                    },
                    {
                        "count": "1",
                        "id": "f5a8fa09ad0"
                    },
                    {
                        "count": "1",
                        "id": "a5be16ac908"
                    }
                ],
                "p_cost": "195",
                "product_count": "1",
                "q": "2646",
                "rid": "1e72e6986b3",
                "shard": {
                    "fire2": "2",
                    "wind2": "3"
                },
                "type": "\u9996\u98fe\u308a"
            }
        ]
    },
    "version": "201703091730"
}
"""