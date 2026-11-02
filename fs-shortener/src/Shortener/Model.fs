module FsShortener.Shortener.Model

type Link = {
    Url: string
    Code: string
    Expiration: int option
}

type Error = {
    Message: string
    ErrorCode: int
}

type LinkStore = {
    Save: Link -> Async<Result<unit, Error>>
    Get: string -> Async<Result<Link option, Error>>
    Exists: string -> Async<Result<bool, Error>>
}