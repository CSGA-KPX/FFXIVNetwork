#I @"K:\Apps\SaintCoinach\SaintCoinach\bin\Debug\"
#r @"Newtonsoft.Json.dll"
#r @"DotNetZip.dll"
#r @"DotSquish.dll"
#r @"EntityFramework.dll"
#r @"SaintCoinach.dll"
#r @"Mono.cecil\Mono.Cecil.dll"
#r @"Mono.cecil\Mono.Cecil.Mdb.dll"
#r @"Mono.cecil\Mono.Cecil.Pdb.dll"
#r @"Mono.cecil\Mono.Cecil.Rocks.dll"
#r @"bin\Debug\LibFFXIV.ClientData.dll"

open System
open System.IO
open SaintCoinach
open Newtonsoft.Json
open Mono.Cecil

let outputPath = Directory.GetCurrentDirectory()
Directory.SetCurrentDirectory(@"K:\Apps\SaintCoinach\SaintCoinach\bin\Debug\")

let GameDirectory = @"G:\FF14\最终幻想XIV"

printfn "Initializing SaintCoinach at %s" GameDirectory
let realm = new SaintCoinach.ARealmReversed(GameDirectory, SaintCoinach.Ex.Language.ChineseSimplified)


let XivItemToRecord(item : #Xiv.ItemBase) = 
    {LibFFXIV.ClientData.Item.Id = item.Key;LibFFXIV.ClientData.Item.Name = item.Name.ToString()}


printfn "Extracting client data version."
let version = realm.GameVersion

printfn "Extracting client items."
let item = 
    [|
        for item in realm.GameData.Items do 
            yield XivItemToRecord(item)
    |]
    |> JsonConvert.SerializeObject

printfn "Extracting client recipes."
let recipe = 
    [|
        for recipe in realm.GameData.GetSheet<Xiv.Recipe>() do 
            let item  = recipe.ResultItem |> XivItemToRecord
            let count = recipe.ResultCount |> float
            let ingredients = 
                recipe.Ingredients
                |> Seq.map (fun x ->
                    let item = x.Item |> XivItemToRecord
                    let count= x.Count |> float
                    (item, count))
                |> Seq.toArray
            yield {
                LibFFXIV.ClientData.Recipe.ResultItem    = item
                LibFFXIV.ClientData.Recipe.Materials     = ingredients
                LibFFXIV.ClientData.Recipe.ProductCount  = count
            }
    |]
    |> JsonConvert.SerializeObject

printfn "Extracting client CompanyCraftSequence."
let gcs = 
    [|
        for recipe in realm.GameData.GetSheet<Xiv.CompanyCraftSequence>() |> Seq.skip 1 do 
            let item = recipe.ResultItem |> XivItemToRecord
            let m    = new LibFFXIV.ClientData.Recipe.FinalMaterials()
            for part in recipe.CompanyCraftParts do 
                for proc in part.CompanyCraftProcesses do 
                    for request in proc.Requests do 
                        let ri= request.SupplyItem.Item |> XivItemToRecord
                        let rc = request.TotalQuantity |> float
                        m.AddOrUpdate(ri, rc)
            yield {
                LibFFXIV.ClientData.Recipe.ResultItem    = item
                LibFFXIV.ClientData.Recipe.Materials     = m.Get()
                LibFFXIV.ClientData.Recipe.ProductCount  = 1.0
            }
    |]
    |> JsonConvert.SerializeObject

printfn "Extracting client GilShop."
let shopitem = 
    [|
        for shop in realm.GameData.GetSheet<Xiv.GilShop>() do 
            for item in shop.Items do
                let price = item.Item.Ask |> float
                let item = item.Item  |> XivItemToRecord
                yield {
                    LibFFXIV.ClientData.GilShopItem.ShopItem = item
                    LibFFXIV.ClientData.GilShopItem.Price    = price
                }
    |]
    |> JsonConvert.SerializeObject


let p = new ReaderParameters(ReadSymbols = true, InMemory = true)
let output = Path.Combine(outputPath,"LibFFXIV.ClientData.dll")

printfn "Reading %s" output
using (AssemblyDefinition.ReadAssembly(output, p)) (fun assembly ->
    let addRes(enum : LibFFXIV.ClientData.Utils.Resource , data : string) = 
        let bytes = (new System.Text.UTF8Encoding(false)).GetBytes(data)
        assembly.MainModule.Resources.Add(new EmbeddedResource(enum.ToString(), ManifestResourceAttributes.Public, bytes))

    addRes(LibFFXIV.ClientData.Utils.Resource.TargetVersion, version)
    addRes(LibFFXIV.ClientData.Utils.Resource.Item, item)
    addRes(LibFFXIV.ClientData.Utils.Resource.CompanyCraftSequence, gcs)
    addRes(LibFFXIV.ClientData.Utils.Resource.GilShopItem, shopitem)
    addRes(LibFFXIV.ClientData.Utils.Resource.Recipe, recipe)

    printfn "Writing %s" output
    assembly.Write(output, new WriterParameters(WriteSymbols = true))
)