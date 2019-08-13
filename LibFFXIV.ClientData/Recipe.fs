module LibFFXIV.ClientData.Recipe
open System
open System.Collections.Generic
open LibFFXIV.ClientData.Item

type RecipeRecord = 
    {
        ResultItem    : ItemRecord
        Materials     : (ItemRecord * float) []
        ProductCount  : float
    }

type FinalMaterials () = 
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

type IRecipeProvider = 
    abstract TryGetRecipe : ItemRecord -> RecipeRecord option


[<AbstractClassAttribute>]
type RecipeProviderBase() as x = 
    let logger = NLog.LogManager.GetLogger(x.GetType().Name)

    member internal x.Logger = logger


type CraftRecipeProvider() as x = 
    inherit RecipeProviderBase()
    let dict     = new Dictionary<ItemRecord, RecipeRecord>()

    do
        try
            let recipes = Utils.Recipe.ReadBinary<RecipeRecord[]>()
            for recipe in recipes do 
                //部分道具具有多个配方，但材料差不多，略过
                if not <| dict.ContainsKey(recipe.ResultItem) then
                    dict.Add(recipe.ResultItem, recipe)
        with
        | e -> x.Logger.Fatal("CraftRecipeProvider加载失败，异常：%s", e.ToString())

    interface IRecipeProvider with
        member x.TryGetRecipe(item) = dict.TryGetValue(item)  |> Utils.TryGetToOption

type CompanyCraftRecipeProvider() as x = 
    inherit RecipeProviderBase()
    let dict     = new Dictionary<ItemRecord, RecipeRecord>()

    do
        try
            let recipes = Utils.CompanyCraftSequence.ReadBinary<RecipeRecord[]>()
            for recipe in recipes do 
                //部分道具具有多个配方，但材料差不多，略过
                if not <| dict.ContainsKey(recipe.ResultItem) then
                    dict.Add(recipe.ResultItem, recipe)
        with
        | e -> x.Logger.Fatal("CraftRecipeProvider加载失败，异常：%s", e.ToString())

    interface IRecipeProvider with
        member x.TryGetRecipe(item) = dict.TryGetValue(item)  |> Utils.TryGetToOption


type RecipeManager private () = 
    inherit RecipeProviderBase()

    let providers = HashSet<IRecipeProvider>()
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
        rm.AddProvider(new CraftRecipeProvider())
        rm.AddProvider(new CompanyCraftRecipeProvider())
        rm

    static member GetInstance() = instance

    ///获取物品直接材料
    member x.GetMaterials(item : ItemRecord) =
        let recipe = (x :> IRecipeProvider).TryGetRecipe(item)
        [|
            if recipe.IsSome then
                yield! recipe.Value.Materials
        |]

    ///获取物品基本材料
    member x.GetMaterialsRec(item : ItemRecord) =
        let rec getMaterialsRec(acc : Dictionary<ItemRecord, (ItemRecord * float)>, item : ItemRecord, runs : float) = 
            let recipe = (x :> IRecipeProvider).TryGetRecipe(item)
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
            let recipe = (x :> IRecipeProvider).TryGetRecipe(item)
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

    interface IRecipeProvider with
        member x.TryGetRecipe(item : ItemRecord) = 
            findRecipe(providers |> Seq.toList, item)