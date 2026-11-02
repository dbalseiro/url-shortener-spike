module FsShortener.Health.Routes

open System
open Microsoft.AspNetCore.Routing
open Microsoft.AspNetCore.Builder

open Domain

let map (app: IEndpointRouteBuilder) =
    let group = app.MapGroup "/health"

    group.MapGet("", Func<HealthResponse>(
        fun () -> toResponse Healthy
    )) |> ignore