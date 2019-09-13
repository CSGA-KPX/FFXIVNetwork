module LibFFXIV.ClientData.Item
open System
type ItemRecord = 
    {
        Id   : int
        Name : string
    }

    override x.ToString() = 
        sprintf "%s(%i)" x.Name x.Id

    static member GetUnknown(lodeId) = 
        {
            Id      = -1
            Name = "未知"
        }

///部分重名道具列表
let internal itemOverriding = 
    [|
        {Id = 14928; Name = "长颈驼革手套";}    , {Id = 14928; Name = "长颈驼革手套时装";}
        {Id = 16604; Name = "鹰翼手铠";}        , {Id = 16604; Name = "鹰翼手铠时装";}
        {Id = 13741; Name = "管家之王证书";}    , {Id = 13741; Name = "过期的管家之王证书";}
        {Id = 18491; Name = "游牧御敌头盔";}    , {Id = 18491; Name = "游牧御敌头盔时装";}
        {Id = 17915; Name = "迦迦纳怪鸟的粗皮";}, {Id = 17915; Name = "大迦迦纳怪鸟的粗皮";}
        {Id = 20561; Name = "东方装甲";}        , {Id = 20561; Name = "东国装甲";}
        {Id = 24187; Name = "2018年度群狼盛宴区域锦标赛冠军之证";}        , {Id = 24187; Name = "2018年度群狼盛宴区域锦标赛冠军之证24187";}
        {Id = 24188; Name = "2018年度群狼盛宴区域锦标赛冠军之证";}        , {Id = 24188; Name = "2018年度群狼盛宴区域锦标赛冠军之证24188";}
        {Id = 24189; Name = "2018年度群狼盛宴区域锦标赛冠军之证";}        , {Id = 24189; Name = "2018年度群狼盛宴区域锦标赛冠军之证24189";}

    |] |> dict

let internal nameAsKey = new System.Collections.Generic.Dictionary<string, ItemRecord>()
let internal idAsKey   = new System.Collections.Generic.Dictionary<int   , ItemRecord>()

let LookupByName(name)= nameAsKey.TryGetValue(name) |> Utils.TryGetToOption
let LookupById(id)    =   idAsKey.TryGetValue(id) |> Utils.TryGetToOption
let AllItems          = lazy (idAsKey |> Seq.map (fun x -> x.Value) |> Array.ofSeq)

#if COMPILED 
do
    let ra = Utils.Resource.Item.ReadBinary<ItemRecord[]>()
    for r in ra do 
        if not (String.IsNullOrWhiteSpace(r.Name)) then
            let item = 
                if itemOverriding.ContainsKey(r) then
                    itemOverriding.[r]
                else
                    r
            if nameAsKey.ContainsKey(r.Name) then
                let current = nameAsKey.[item.Name]
                failwithf "已存在物品，%O -> %O" current item
            nameAsKey.Add(item.Name,item)
            idAsKey.Add(item.Id, item)
    ()
#endif