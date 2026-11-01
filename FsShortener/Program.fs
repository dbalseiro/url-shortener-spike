open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open FsShortener

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    let app = builder.Build()

    app.MapGet("/", Func<string>(fun () -> "Hello World!")) |> ignore

    Health.Routes.map app

    app.Run()

    0 // Exit code

