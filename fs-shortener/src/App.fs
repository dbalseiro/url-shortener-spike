module FsShortener.App

open System
open FsShortener
open Microsoft.AspNetCore.Builder

let configure (builder: WebApplicationBuilder) =
    let app = builder.Build()
    app.MapGet("/", Func<string>(fun () -> "Hello World!")) |> ignore
    Health.Routes.map app
    app