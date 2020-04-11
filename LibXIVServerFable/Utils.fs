module LibDmfXiv.Shared.Utils
open System

let formatTimeSpan (ts : TimeSpan) = 
    sprintf "%i天%i时前" ts.Days ts.Hours