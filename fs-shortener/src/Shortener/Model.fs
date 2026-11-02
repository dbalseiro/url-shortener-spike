module FsShortener.Shortener.Model

open System.Threading.Tasks

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
    Save: Link -> Task<Result<unit, Error>>
    Get: string -> Task<Result<Link option, Error>>
    Exists: string -> Task<Result<bool, Error>>
}