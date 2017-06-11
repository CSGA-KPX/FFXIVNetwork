module LibFFXIV.Database

open System
open System.Collections.Generic
open FSharp.Data
open FSharp.Data.JsonExtensions

type XIVDBItem = 
    {
        ID      : int
        NameChs : string
        NameEng : string
        Patch   : int
    }

    override x.ToString() = 
        let name = 
            if String.IsNullOrEmpty(x.NameChs) then
                x.NameEng
            else
                x.NameChs

        sprintf "%s(%i)" name x.ID
        

type XIVDBItemProvier = JsonProvider<"""[{"id":14797,"name_cns":"\u77e5\u8bc6\u795e\u6cbb\u6108\u51c9\u978b","name_en":"Thaliak\u0027s Sandals of Healing","patch":25}]""">

let XIVItemDict = 
    let col = new Dictionary<int, XIVDBItem>()
    let json = XIVDBItemProvier.Load("xivdb_items.json")
    for item in json do
        let obj = 
            {
                ID = item.Id
                NameChs = item.NameCns
                NameEng = item.NameEn
                Patch   = item.Patch
            }
        col.Add(obj.ID, obj)
    col