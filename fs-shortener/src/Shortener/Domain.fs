module FsShortener.Shortener.Domain

type Violation =
    | InvalidUrl of string
    | InvalidAlias
    | InvalidTTL

type TryAgain = {
    RetryAfter: int
}

type ShortenError =
    | InvalidRequest of Violation list
    | StorageError of string
    | AliasTaken of string
    | RateLimited of TryAgain

type FetchError =
    | NotFound
    | RateLimited of TryAgain

type ShortenRequest = {
    Url: string
    Alias: string option
    TTL: int option
}

type ShortenResponse = {
    Code: string
    ShortUrl: string
    OriginalUrl: string
    ExpiresAt: System.DateTime option
}
