module LibFFXIV.Database

open System
open System.Collections.ObjectModel
open System.Collections.Generic
open FSharp.Data
open FSharp.Data.JsonExtensions

type SuItemDataProvider   = JsonProvider<Constants.XIVItemDataSample>
type SuRecipeDataProvider =  JsonProvider<Constants.XIVRecipseDataSample>
//
type XIVDBItemProvier = JsonProvider<"""[{"id":1,"lodestone_id":"ea1413db150","name_en":"Gil","name_cns":"\u91d1\u5e01"}]""">

let internal tryGetToOption (x : bool, y: 'Value) = 
    if x then
        Some(y)
    else
        None


type SuItemRecord = 
    {
        LodestoneId : string
        XIVDbId     : int
        NameEng     : string
        NameChs     : string
    }
    static member GetUnknown(lodeId) = 
        {
            LodestoneId = lodeId
            XIVDbId     = -1
            NameEng     = "Unknown"
            NameChs     = "未知"
        }
    member x.GetName () = 
        if not (String.IsNullOrEmpty(x.NameChs)) then
            x.NameChs
        else
            x.NameEng
    override x.ToString() = 
        sprintf "%s(%i)" (x.GetName()) x.XIVDbId

type SuRecipeFlag = 
    | None  = 0
    ///可以制作HQ
    | CanHQ = 1
    ///可以简易制作
    | CanQS = 2
    ///可以制作收藏品
    | CanCo = 4

type SuRecipeRecord = 
    {
        (*
        ///配方属性 a_at
        Attrib : string
        ///作业精度 a_wk
        Craftsmanship : int
        ///加工精度 a_ed
        Control : int
        ///可以制作HQ a_flg
        CanHQ   : bool
        ///可以简易制作 a_flg
        CanQuickSynth : bool 
        ///可以制作收藏品 a_flg
        CanCollectable: bool*)
        ///制作职业 category        ClassName     : string
        ClassName     : string
        ///制作等级.星数
        LevelNum      : float
        ///制作材料 (lodestoneId, 数量)
        Materials     : (SuItemRecord * int) []
        ProductCount  : int

    }

type XIVDBItemData private() = 
    let ver = 201706182047L
    let fromName    = new Dictionary<string, SuItemRecord>()
    let fromXIVDbId = new Dictionary<int   , SuItemRecord>()
    let fromLodeId  = new Dictionary<string, SuItemRecord>()
    do
        let json = XIVDBItemProvier.Load("xivdb_items.json")
        for item in json do 
            if not (String.IsNullOrEmpty(item.LodestoneId)) then
                let obj = {
                    LodestoneId = item.LodestoneId
                    XIVDbId     = item.Id
                    NameEng     = item.NameEn
                    NameChs     = item.NameCns}

                let lodeDup = fromLodeId.ContainsKey(obj.LodestoneId)
                let xividDup= fromXIVDbId.ContainsKey(obj.XIVDbId)
                let nameDup= fromName.ContainsKey(obj.GetName())
                if lodeDup || xividDup || nameDup then
                    printfn "Found dup : lode:%b xivdb:%b name:%b %A" lodeDup xividDup nameDup obj
                fromLodeId.Add(obj.LodestoneId, obj)
                fromXIVDbId.Add(obj.XIVDbId, obj)
                //fromName.Add(obj.GetName(), obj)

    static let instance = XIVDBItemData()
    static member Instance = instance

    member x.Version = ver

    member x.FromName(str)   = fromName.TryGetValue(str)   |> tryGetToOption

    member x.FromLodeId(str) = fromLodeId.TryGetValue(str) |> tryGetToOption

    member x.FromXIVId(str)  = fromXIVDbId.TryGetValue(str)|> tryGetToOption

type SuItemData private() = 
    let mutable ver = 0L
    let fromName    = new Dictionary<string, SuItemRecord>()
    let fromXIVDbId = new Dictionary<int   , SuItemRecord>()
    let fromLodeId  = new Dictionary<string, SuItemRecord>()
    do
        let json = SuItemDataProvider.Load("item_data_3_5_chs3_5_test.json")
        ver <- json.Version
        let data = json.Data.JsonValue.Properties
        for (lsid, obj) in data do
            if not (String.IsNullOrEmpty(lsid)) then
                let obj = {
                    LodestoneId = lsid
                    XIVDbId     = obj?index.AsInteger()
                    NameEng     = obj?ename.AsString()
                    NameChs     = obj?cname.AsString()}
                fromLodeId.Add(lsid, obj)
                fromXIVDbId.Add(obj.XIVDbId, obj)
                if (fromName.ContainsKey(obj.GetName())) then
                    printfn "Found duplicate key new:%A old:%A" (obj) (fromName.[obj.GetName()])
                else
                    fromName.Add(obj.GetName(), obj)
        //fromName.Keys |> Seq.iter (printfn "%s")

    static let instance = SuItemData()
    static member Instance = instance

    member x.Version = ver

    member x.FromName(str)   = fromName.TryGetValue(str)   |> tryGetToOption

    member x.FromLodeId(str) = fromLodeId.TryGetValue(str) |> tryGetToOption

    member x.FromXIVId(str)  = fromXIVDbId.TryGetValue(str)|> tryGetToOption

let ItemProvider = SuItemData.Instance

type SuRecipeData private() = 
    let mutable ver = 0L
    let dict = Dictionary<string, SuRecipeRecord []>()
    do
        let json = SuRecipeDataProvider.Load("recipe_info.jp.3.5.json")
        ver <- json.Version
        let data = json.Data.JsonValue.Properties
        for (lsId, obj) in data do 
            let recipes = 
                [|
                    for recipe in obj.AsArray() do 
                        let materials  = 
                                [|
                                    let objs = recipe?material.AsArray()
                                    for obj in objs do 
                                        let lodeid = obj?id.AsString()
                                        let exists = ItemProvider.FromLodeId(lodeid)
                                        let item   = 
                                            if exists.IsSome then
                                                exists.Value
                                            else
                                                NLog.LogManager.GetCurrentClassLogger().Error("item not found :{0}", lodeid)
                                                SuItemRecord.GetUnknown(lodeid)
                                        yield (item,  obj?count.AsInteger())
                                    let shards = recipe?shard.Properties
                                    for (id, obj) in shards do
                                        let shard = SuRecipeData.ShardsMapping.[id]
                                        let count = obj.AsInteger()
                                        yield (shard, obj.AsInteger())

                                        
                                |]
                        yield
                            {
                                ClassName     = recipe?category.AsString()
                                LevelNum      = recipe?lv_num.AsFloat()
                                Materials     = materials
                                ProductCount  = recipe?product_count.AsInteger()
                            }
                |]
            dict.Add(lsId, recipes)
    static let instance = new SuRecipeData()
    static member Instance = instance
    static member private ShardsMapping : Dictionary<string, SuItemRecord> = 
        let dict = new Dictionary<string, SuItemRecord>()
        let attribs = [| ("fire", "火"); ("wind", "风"); ("ice", "冰"); ("earth", "土"); ("thunder", "雷"); ("water", "水") |]
        let types   = [| ("1", "之碎晶"); ("2", "之水晶"); ("3", "之晶簇"); |]
        for a in attribs do
            for t in types do 
                let keyName = (fst a) + (fst t)
                let realName= (snd a) + (snd t)
                let item    = ItemProvider.FromName(realName)
                dict.Add(keyName, item.Value)
                
        dict
    member x.GetRecipe(str) = dict.TryGetValue(str) |> tryGetToOption

    member x.GetMaterials(str) =
        let recipes = dict.TryGetValue(str) |> tryGetToOption
        if (recipes.IsSome) then
            let recipe = recipes.Value.[0]
            Some(recipe.Materials)
        else
            None

    member x.GetMaterialsRec(str, ?runs : int) = 
        let recipes = dict.TryGetValue(str) |> tryGetToOption
        if (recipes.IsNone) then
            None
        else
            Some("")