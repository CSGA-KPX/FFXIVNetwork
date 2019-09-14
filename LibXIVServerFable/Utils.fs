module LibDmfXiv.Shared.Utils
open System

let formatTimeSpan (ts : TimeSpan) = 
    sprintf "%i天%i时%i分%i秒前" ts.Days ts.Hours ts.Minutes ts.Seconds