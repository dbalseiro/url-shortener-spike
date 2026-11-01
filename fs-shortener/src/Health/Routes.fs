module FsShortener.Health.Routes

open System
open Microsoft.AspNetCore.Routing
open Microsoft.AspNetCore.Builder

open FsShortener.Health.Domain

let map (app: IEndpointRouteBuilder) =
    let group = app.MapGroup("/health")

    group.MapGet("", Func<HealthResponse>(
        fun () -> toResponse Status.Healthy
    )) |> ignore