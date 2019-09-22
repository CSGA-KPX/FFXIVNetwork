module LibFFXIV.ClientData.Utils
open System.IO
open System.Reflection
open Newtonsoft.Json

let TryGetToOption (x : bool, y: 'Value) = 
    if x then
        Some(y)
    else
        None

type Resource = 
    | Item
    | Recipe
    | CompanyCraftSequence
    | GilShopItem
    | TargetVersion
    | TerritoryType
    | ContentFinderCondition
        
    member x.ReadText() = 
        let assembly = Assembly.GetExecutingAssembly()
        use stream = assembly.GetManifestResourceStream(x.ToString())
        use reader = new StreamReader(stream, System.Text.Encoding.UTF8)
        reader.ReadToEnd()

    member x.ReadBinary<'T>() = 
        JsonConvert.DeserializeObject<'T>(x.ReadText())

    override x.ToString() = 
        "LibFFXIV.ClientData.Data." + (
                match x with
                | Item -> "Item.bin"
                | Recipe -> "Recipe.bin"
                | CompanyCraftSequence -> "CompanyCraftSequence.bin"
                | GilShopItem -> "GilShopItem.bin"
                | TargetVersion -> "TargetVersion.txt"
                | TerritoryType -> "TerritoryType.bin"
                | ContentFinderCondition -> "ContentFinderCondition.bin" )

let ShowAllResources() = 
    let assembly = Assembly.GetExecutingAssembly()
    assembly.GetManifestResourceNames()
    |> Array.iter (printfn "%s")