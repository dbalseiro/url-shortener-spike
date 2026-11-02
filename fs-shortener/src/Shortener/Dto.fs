module FsShortener.Shortener.Dto

open System
open System.Text.Json.Serialization

[<CLIMutable>]
type ShortenRequestDto = {
    [<JsonPropertyName("url")>]
    Url: string

    [<JsonPropertyName("alias")>]
    Alias: string

    [<JsonPropertyName("ttlSeconds")>]
    TTLSeconds: Nullable<int>
}

module Request =
    let toDomain (dto: ShortenRequestDto) : Domain.ShortenRequest = {
        Domain.Url = dto.Url
        Domain.Alias = Option.ofObj dto.Alias
        Domain.TTL = Option.ofNullable dto.TTLSeconds
    }

    let ofDomain (domain: Domain.ShortenRequest) : ShortenRequestDto = {
        Url = domain.Url
        Alias = match domain.Alias with Some a -> a | None -> null
        TTLSeconds = match domain.TTL with Some t -> Nullable t | None -> Nullable()
    }


[<CLIMutable>]
type ShortenResponseDto = {
    [<JsonPropertyName("code")>]
    Code: string

    [<JsonPropertyName("url")>]
    OriginalUrl: string

    [<JsonPropertyName("shortUrl")>]
    ShortUrl: string

    [<JsonPropertyName("expiresAt")>]
    ExpiresAt: Nullable<int32>
}

[<CLIMutable>]
type ViolationDto = {
    [<JsonPropertyName("message")>]
    Message: string

    [<JsonPropertyName("type")>]
    Type: string
}

module Response = 
    let toDomain (dto: ShortenResponseDto) : Domain.ShortenResponse = {
        Domain.Code = dto.Code
        Domain.OriginalUrl = dto.OriginalUrl
        Domain.ShortUrl = dto.ShortUrl
        Domain.ExpiresAt = 
            Option.ofNullable dto.ExpiresAt 
            |> Option.map (fun x -> DateTimeOffset.FromUnixTimeSeconds(int64 x).UtcDateTime)
    }

    let ofDomain (domain: Domain.ShortenResponse) : ShortenResponseDto = {
        Code = domain.Code
        OriginalUrl = domain.OriginalUrl
        ShortUrl = domain.ShortUrl
        ExpiresAt = match domain.ExpiresAt with 
                    | Some dt -> Nullable (int (DateTimeOffset(dt, TimeSpan.Zero).ToUnixTimeSeconds())) 
                    | None -> Nullable()
    }


    let violationDto(violation: Domain.Violation) : ViolationDto =
        match violation with
        | Domain.InvalidUrl message -> {
                Message = "Invalid URL: " + message
                Type = "InvalidUrl"
            }
        | Domain.InvalidAlias -> {
                Message = "Invalid alias"
                Type = "InvalidAlias"
            }
        | Domain.InvalidTTL -> {
                Message = "Invalid TTL"
                Type = "InvalidTTL"
            }