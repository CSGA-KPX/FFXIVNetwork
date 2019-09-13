module LibFFXIV.ClientData.TerritoryType
open System
type TerritoryTypeRecord =
    {
        Name       : string
        RegionName : string
        ZoneName   : string
        PlaceName  : string
        TerritoryIntendedUse : byte
    }

    override x.ToString() = 
        sprintf "%s>%s>%s" x.RegionName x.ZoneName x.PlaceName

let AllTerritory = new Collections.Generic.HashSet<TerritoryTypeRecord>()

#if COMPILED 
do
    let ra = Utils.Resource.TerritoryType.ReadBinary<TerritoryTypeRecord[]>()
    for r in ra do 
        if not (String.IsNullOrWhiteSpace(r.Name)) then
            AllTerritory.Add(r) |> ignore
    ()
#endif