module FsShortener.Shortener.Service

open System.Threading.Tasks

open FsShortener.Infrastructure.Model

let fetch
    (store: LinkStore)
    (code: string)
    : Task<Result<string, Domain.FetchError>> = task {
        let! value = store.Get code
        return match value with
               | Error err -> Error ( err 
                                    |> storageErrorToString 
                                    |> Domain.FetchError.StorageError
                                    )
               | Ok None -> Error Domain.FetchError.NotFound
               | Ok (Some link) -> Ok link.Url
    }
let shorten 
    (store: LinkStore) 
    (baseUrl: string)
    (request: Domain.ShortenRequest)
    : Task<Result<Domain.ShortenResponse, Domain.ShortenError>> = task {
        match Validation.validateRequest request with
        | _ :: _ as vs -> return Error (Domain.InvalidRequest vs)
        | [] -> 
            let alias = Option.defaultWith Encoding.generateCode request.Alias
            let! value = store.Get alias
            match value with
            | Error err -> return Error (Domain.StorageError (storageErrorToString err))
            | Ok (Some link) -> return Error (Domain.AliasTaken link.Url)
            | Ok None ->
                let link = { Url = request.Url; Code = alias; Expiration = request.TTL }
                let! saveResult = store.Save link
                match saveResult with
                | Error err -> return Error (Domain.StorageError (storageErrorToString err))
                | Ok () -> return Ok { 
                    Domain.Code = alias
                    Domain.ShortUrl = baseUrl.TrimEnd('/') + "/" + alias
                    Domain.OriginalUrl = request.Url
                    Domain.ExpiresAt = request.TTL |> Option.map (fun ttl -> System.DateTime.UtcNow.AddSeconds (float ttl))
                }
    }