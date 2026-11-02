module FsShortener.Shortener.Service

open System.Threading.Tasks

let shorten 
    (store: Model.LinkStore) 
    (request: Domain.ShortenRequest)
    : Task<Result<Domain.ShortenResponse, Domain.ShortenError>> = task {
        match Validation.validateRequest request with
        | _ :: _ as vs -> return Error (Domain.InvalidRequest vs)
        | [] -> 
            let alias = Option.defaultWith Encoding.generateCode request.Alias
            let! value = store.Get alias
            match value with
            | Error err -> return Error (Domain.StorageError (Model.storageErrorToString err))
            | Ok (Some link) -> return Error (Domain.AliasTaken link.Url)
            | Ok None ->
                let link = { Model.Url = request.Url; Model.Code = alias; Model.Expiration = request.TTL }
                let! saveResult = store.Save link
                match saveResult with
                | Error err -> return Error (Domain.StorageError (Model.storageErrorToString err))
                | Ok () -> return Ok { 
                    Domain.Code = alias
                    Domain.OriginalUrl = request.Url
                    Domain.ExpiresAt = request.TTL |> Option.map (fun ttl -> System.DateTime.UtcNow.AddSeconds (float ttl))
                }
    }