open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Configuration

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder args
    let scriptPath = match builder.Configuration["rate-limiter:script"] with
                     | null -> failwith "Rate limiter script path not configured"
                     | path -> path
    let app = File.ReadAllText scriptPath
              |> FsShortener.App.configure builder
    app.Run()
    0 // Exit code
