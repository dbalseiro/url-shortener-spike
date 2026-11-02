module FsShortener.Shortener.Routes

open System
open System.Threading.Tasks

open Microsoft.AspNetCore.Routing
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http

let private mapShortenResponse(result: Result<Dto.ShortenResponseDto, Domain.ShortenError>): IResult = failwith "Not Implemented"

let map (store: Model.LinkStore) (app: IEndpointRouteBuilder) =
    let group = app.MapGroup "/shorten"

    group.MapPost("", Func<Dto.ShortenRequestDto, Task<IResult>>(
        fun request -> task {
            let! response = Service.shorten store (Dto.Request.toDomain request)
            let result = Result.map Dto.Response.ofDomain response
            return mapShortenResponse result
        }
    )) |> ignore
