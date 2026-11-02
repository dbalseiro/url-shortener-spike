module FsShortener.Shortener.Routes

open System
open Microsoft.AspNetCore.Routing
open Microsoft.AspNetCore.Builder

let map (store: Model.LinkStore) (app: IEndpointRouteBuilder) =
    let group = app.MapGroup "/shorten"

    group.MapPost("", Func<Domain.ShortenRequest, Domain.ShortenResponse>(
        fun request -> failwith "Not implemented"
    )) |> ignore
