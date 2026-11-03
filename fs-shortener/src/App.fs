module FsShortener.App

open System
open FsShortener
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http

let configure (builder: WebApplicationBuilder) =
    let app = builder.Build()
    app.MapGet("/", Func<string>(fun () -> "Hello World!")) |> ignore
    app.MapGet("/{code}", Func<string, IResult>(
        fun code -> Results.LocalRedirect (sprintf "/shorten/%s" code)
    )) |> ignore
    Health.Routes.map app

    let useRedis = System.Environment.GetEnvironmentVariable "USE_REDIS" = "true"
    let infra = 
        if useRedis then
            let redisHost = 
                match System.Environment.GetEnvironmentVariable "REDIS_HOST" with
                | null -> "localhost"
                | h -> h
            let redisPort = 
                match System.Int32.TryParse(
                    match System.Environment.GetEnvironmentVariable "REDIS_PORT" with
                    | null -> "6379"
                    | p -> p
                    ) with
                | true, port -> port
                | _ -> 6379
            Infrastructure.Redis.create { Host = redisHost; Port = redisPort }
        else
            Infrastructure.Memory.create()
    
    Shortener.Routes.map infra app
    app