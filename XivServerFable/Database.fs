module Database
open System
let dbpath = 
    if Type.GetType("Mono.Runtime") <> null then
        "../static/ffxivserver.db"
    else
        "ffxivserver.db"

let db = new LiteDB.LiteDatabase(dbpath)

type Utils = 
    static member ToDocument(i : uint16) = new LiteDB.BsonValue(i |> int32)
    static member ToDocument(i : uint32) = new LiteDB.BsonValue(i |> int64)
    static member ToDocument(s : string) = new LiteDB.BsonValue(s)