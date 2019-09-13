module Database
open System
let dbpath = 
    if Type.GetType("Mono.Runtime") <> null then
        "../static/ffxivserver.db"
    else
        "ffxivserver.db"

let db = new LiteDB.LiteDatabase(dbpath)


