module LibFFXIV.Client.Item
open System
open System.Collections.Generic

type ItemRecord = 
    {
        Id      : int
        Name : string
    }

    override x.ToString() = 
        sprintf "%s(%i)" x.Name x.Id

    static member GetUnknown(lodeId) = 
        {
            Id      = -1
            Name = "未知"
        }

type SaintCoinachItemProvider private() = 
    let version  = Utils.SaintCoinachInstance.Instance.GameVersion
    let fromName = new Dictionary<string, ItemRecord>()
    let fromId   = new Dictionary<int   , ItemRecord>()
    let logger   = NLog.LogManager.GetCurrentClassLogger()
    let manualMapping =
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
    do
        for item in Utils.SaintCoinachInstance.Instance.GameData.Items do 
            if not item.Name.IsEmpty then
                let ir = 
                    let t = {ItemRecord.Id = item.Key; ItemRecord.Name = item.Name.ToString()}
                    if manualMapping.ContainsKey(t) then
                        manualMapping.[t]
                    else
                        t
                
                fromId.Add(ir.Id, ir)
                if fromName.ContainsKey(ir.Name) then
                    let current = fromName.[ir.Name]
                    logger.Fatal(sprintf "存在已物品，%O -> %O" current ir)
                else
                    fromName.Add(ir.Name, ir)

    static let instance = new SaintCoinachItemProvider()

    static member GetInstance() = instance
    
    member x.Version = version
    member x.FromName (name) = fromName.TryGetValue(name) |> Utils.TryGetToOption
    member x.FromId (id)     = fromId.TryGetValue(id) |> Utils.TryGetToOption