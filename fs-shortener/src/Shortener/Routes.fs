module FsShortener.Shortener.Routes

open System
open System.Threading.Tasks

open Microsoft.AspNetCore.Routing
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http

let private mapShortenResponse
    (result: Result<Dto.ShortenResponseDto, Domain.ShortenError>)
    : IResult = 
        match result with
        | Ok response -> 
            Results.Ok response
        | Error error ->
            match error with
            | Domain.InvalidRequest vl -> Results.BadRequest (List.map Dto.Response.violationDto vl)
            | Domain.StorageError s -> Results.BadRequest s
            | Domain.AliasTaken s -> Results.Conflict s
            | Domain.RateLimited s -> failwith "Not Implemented"

let private mapFetchResponse
    (result: Result<string, Domain.FetchError>)
    : IResult =
        match result with
        | Ok url -> Results.Redirect url
        | Error error ->
            match error with
            | Domain.FetchError.NotFound -> Results.NotFound()
            | Domain.FetchError.StorageError s -> Results.BadRequest s
            | Domain.FetchError.RateLimited s -> failwith "Not Implemented"

let map (store: Model.LinkStore) (app: IEndpointRouteBuilder) =
    let group = app.MapGroup "/shorten"

    group.MapPost("", Func<Dto.ShortenRequestDto, HttpContext, Task<IResult>>(
        fun request httpContext -> task {
            let baseUrl = sprintf "%s://%s" httpContext.Request.Scheme httpContext.Request.Host.Value
            let! response = Service.shorten store baseUrl (Dto.Request.toDomain request)
            let result = Result.map Dto.Response.ofDomain response
            return mapShortenResponse result
        }
    )) |> ignore

    group.MapGet("/{code}", Func<string, Task<IResult>>(
        fun code -> task {
            let! response = Service.fetch store code
            return mapFetchResponse response
        }
    )) |> ignore
