module FsShortener.App

open FsShortener
open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection

let private getEnvOrDefault key defaultValue =
    match System.Environment.GetEnvironmentVariable key with
    | null -> defaultValue
    | value -> value

let private truthy (value: string) =
    match value.ToLower().Trim() with
    | "true" | "1" | "yes" -> true
    | _ -> false

let private scriptArgs (cfg: IConfiguration): string list = 
    let stringOrDefault defaultValue key =
        match cfg.[key] with
        | null -> defaultValue
        | value -> value
    [ 
      stringOrDefault "10" "rate-limiter:params:maxTokens"
      stringOrDefault "1" "rate-limiter:params:tokensPerSecond"
      DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
      stringOrDefault "60" "rate-limiter:params:windowSeconds"
    ] 


let configure (builder: WebApplicationBuilder) (scriptCode: string) =
    let useRedis = getEnvOrDefault "USE_REDIS" "false" |> truthy
    let infra = 
        if useRedis then
            let redisHost = getEnvOrDefault "REDIS_HOST" "localhost"
            let redisPort = 
                match System.Int32.TryParse(getEnvOrDefault "REDIS_PORT" "6379") with
                | true, port -> port
                | _ -> 6379
            Infrastructure.Redis.create { Host = redisHost; Port = redisPort }
        else
            Infrastructure.Memory.create()
    
    let app = builder.Build()

    let args = scriptArgs builder.Configuration
    let script: string -> Infrastructure.Model.Script = fun clientIp -> { 
          Infrastructure.Model.ScriptCode = scriptCode
          Infrastructure.Model.ScriptKeys = [ $"rate-limit:tokens:{clientIp}"; $"rate-limit:timestamp:{clientIp}" ]
          Infrastructure.Model.ScriptArgs = args
        }

    // print rate-limiting config
    // TODO: REMOVE
    let logger = app.Services.GetRequiredService<ILogger<_>>()

    logger.LogInformation("Rate-limiting config: maxTokens={maxTokens}, tokensPerSecond={tokensPerSecond}, windowSeconds={windowSeconds}",
        args.[0], args.[1], args.[2]
    )

    app.UseMiddleware<Infrastructure.RateLimiter.RateLimitMiddleware> (infra, script) |> ignore

    app.MapGet("/", Func<string>(fun () -> "Hello World!")) |> ignore
    app.MapGet("/{code}", Func<string, IResult>(
        fun code -> Results.LocalRedirect (sprintf "/shorten/%s" code)
    )) |> ignore
    Health.Routes.map app

    Shortener.Routes.map infra app
    app