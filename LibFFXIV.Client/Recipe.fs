module LibFFXIV.Client.Recipe
open System
open System.Collections.Generic
open LibFFXIV.Client.Item

type RecipeRecord = 
    {
        ///制作材料 (lodestoneId, 数量)
        Materials     : (ItemRecord * float) []
        ProductCount  : float
    }

type IRecipeProvider = 
    abstract TryGetRecipe : ItemRecord -> RecipeRecord option

type internal FinalMaterials () = 
    let m = new Dictionary<ItemRecord, float>()

    member x.AddOrUpdate(item, runs) = 
        if m.ContainsKey(item) then
            m.[item] <- m.[item] + runs
        else
            m.Add(item, runs)

    member x.Get() = 
        [|
            for kv in m do 
                yield (kv.Key, kv.Value)
        |]

type internal SaintCoinachRecipeProvider() = 
    let version  = Utils.SaintCoinachInstance.Instance.GameVersion
    let logger   = NLog.LogManager.GetCurrentClassLogger()
    let dict     = new Dictionary<int, RecipeRecord>()

    do
        let recipes = Utils.SaintCoinachInstance.Instance.GameData.GetSheet<SaintCoinach.Xiv.Recipe>()
        for recipe in recipes do 
            //部分道具具有多个配方，但材料差不多
            if not (dict.ContainsKey(recipe.ResultItem.Key)) then
                let ingredients = 
                    recipe.Ingredients
                    |> Seq.map (fun x ->
                        let item = SaintCoinachItemProvider.GetInstance().FromId(x.Item.Key).Value
                        let count= x.Count |> float
                        (item, count))
                    |> Seq.toArray
                let r = 
                    {
                        RecipeRecord.Materials = ingredients
                        RecipeRecord.ProductCount = recipe.ResultCount |> float
                    }
                dict.Add(recipe.ResultItem.Key, r)
    
    interface IRecipeProvider with
        member x.TryGetRecipe(i) = dict.TryGetValue(i.Id)  |> Utils.TryGetToOption

type internal SaintCoinachCompanyRecipeProvider() = 
    let version  = Utils.SaintCoinachInstance.Instance.GameVersion
    let logger   = NLog.LogManager.GetCurrentClassLogger()
    let dict     = new Dictionary<int, RecipeRecord>()
    do
        let recipes = 
            Utils.SaintCoinachInstance.Instance.GameData.GetSheet<SaintCoinach.Xiv.CompanyCraftSequence>()
            |> Seq.toArray
            |> (fun x -> x.[1..])
        for recipe in recipes do 
            let ip   = SaintCoinachItemProvider.GetInstance()
            let item = recipe.ResultItem.ToString()
            let ikey = recipe.ResultItem.Key
            let m    = new FinalMaterials()

            for part in recipe.CompanyCraftParts do 
                for proc in part.CompanyCraftProcesses do 
                    for request in proc.Requests do 
                        let rin= request.SupplyItem.ToString()
                        let ri = rin |> ip.FromName
                        let rc = request.TotalQuantity
                        if ri.IsNone then
                            logger.Fatal(sprintf "找不到工坊物品%s的材料%s" item rin)
                        else
                            m.AddOrUpdate(ri.Value, rc|> float)
            dict.Add(ikey, {Materials = m.Get(); ProductCount = 1.0})

    interface IRecipeProvider with
        member x.TryGetRecipe(i) = dict.TryGetValue(i.Id)  |> Utils.TryGetToOption

type RecipeManager private () = 
    let providers = System.Collections.Generic.HashSet<IRecipeProvider>()
    let rec findRecipe (list : IRecipeProvider list, item : ItemRecord) = 
        match list with 
        | [] -> None
        | head :: tail -> 
            let h = head.TryGetRecipe(item) 
            if h.IsSome then
                h
            else
                findRecipe(tail, item)

    static let instance = 
        let rm = new RecipeManager()
        rm.AddProvider(new SaintCoinachRecipeProvider())
        rm.AddProvider(new SaintCoinachCompanyRecipeProvider())
        rm

    member x.TryGetRecipe(item : ItemRecord) = 
        findRecipe(providers |> Seq.toList, item)

    ///获取物品直接材料
    member x.GetMaterials(item : ItemRecord) =
        let recipe = x.TryGetRecipe(item)
        [|
            if recipe.IsSome then
                yield! recipe.Value.Materials
        |]

    ///获取物品基本材料
    member x.GetMaterialsRec(item : ItemRecord) =
        let rec getMaterialsRec(acc : Dictionary<ItemRecord, (ItemRecord * float)>, item : ItemRecord, runs : float) = 
            let recipe = x.TryGetRecipe(item)
            if recipe.IsNone then
                if acc.ContainsKey(item) then
                    let (item, count) = acc.[item]
                    acc.[item] <- (item, count + runs)
                else
                    acc.Add(item, (item, runs))
            else
                let realRuns  = runs / recipe.Value.ProductCount
                let materials = recipe.Value.Materials |> Array.map (fun (item, count) -> (item, count * realRuns))
                for (item, count) in materials do
                    getMaterialsRec(acc, item, count)
        [|
            let dict = new Dictionary<ItemRecord, (ItemRecord * float)>()
            getMaterialsRec(dict, item, 1.0)
            let ma = dict.Values |> Seq.toArray
            yield! ma
        |]

    ///获取物品以及子物品的直接材料
    member x.GetMaterialsRecGroup(item : ItemRecord) = 
        let rec getMaterialsRec(acc : Queue<string * (ItemRecord * float) []>, level : string, item : ItemRecord, runs : float) = 
            let recipe = x.TryGetRecipe(item)
            if recipe.IsNone then
                ()
            else
                let realRuns  = runs / recipe.Value.ProductCount
                let self = level + "*" + String.Format("{0:0.###}", 1.0), [| (item, 1.0) |]
                acc.Enqueue(self)
                let materials = recipe.Value.Materials |> Array.map (fun (item, count) -> (item, count * realRuns))
                let countStr = "*" + String.Format("{0:0.###}", 1.0)
                acc.Enqueue(level + countStr + "/", materials)
                for (item, count) in materials do
                    getMaterialsRec(acc, level + "/" + item.Name , item, 1.0)
        [|
            let acc = new Queue<string * (ItemRecord * float) []>()
            getMaterialsRec(acc, item.Name, item, 1.0)
            let test = acc.ToArray()
            yield! acc.ToArray() |> Array.filter (fun (level, arr) -> arr.Length <> 0)
        |]

    member x.AddProvider(p : IRecipeProvider) = 
        providers.Add(p) |> ignore

    static member GetInstance() = instance