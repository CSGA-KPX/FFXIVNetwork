module UsernameMapping
open System
open LibDmfXiv.Shared.UsernameMapping
open LiteDB.FSharp.Extensions

let mappingDb = Database.db.GetCollection<FabelUsernameMapping>()

let usernameMappingApi : IUsernameMapping = 
    {
        PutMapping = fun m -> async {
            mappingDb.Upsert(m) |> ignore
        }

        PutMappings = fun ms -> async {
            for m in ms do 
                mappingDb.Upsert(m) |> ignore
        }

        GetById = fun id -> async {
            return mappingDb.TryFindById(new LiteDB.BsonValue(id))
        }
        
        GetByName = fun name -> async {
            let query = LiteDB.Query.EQ("UserName", new LiteDB.BsonValue(name))
            return mappingDb.Find(query) |> Seq.toArray
        }

    }

do
    LiteDB.BsonMapper.Global.Entity<FabelUsernameMapping>().Id(fun x -> x.UserId) |> ignore
    //mappingDb.EnsureIndex((fun x -> x.UserId)) |> ignore
    mappingDb.EnsureIndex((fun x -> x.UserName)) |> ignore