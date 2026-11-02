module FsShortener.Shortener.Model

open System.Threading.Tasks

type Link = {
    Url: string
    Code: string
    Expiration: int option
}

type StorageError = {
    Message: string
    ErrorCode: int
}

let storageErrorToString (err: StorageError) = sprintf "Error: %s (Code: %d)" err.Message err.ErrorCode

type LinkStore = {
    Save: Link -> Task<Result<unit, StorageError>>
    Get: string -> Task<Result<Link option, StorageError>>
    Exists: string -> Task<Result<bool, StorageError>>
}