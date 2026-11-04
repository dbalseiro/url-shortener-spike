module FsShortener.Infrastructure.Model

open System.Threading.Tasks
open Microsoft.Extensions.Configuration

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

type Script = {
    ScriptCode: string
    ScriptKeys: string list
    ScriptArgs: string list
}

type LinkStore = {
    Save: Link -> Task<Result<unit, StorageError>>
    Get: string -> Task<Result<Link option, StorageError>>
    Exists: string -> Task<Result<bool, StorageError>>
    ExecuteScript: Script -> Task<Result<int32, StorageError>>
}