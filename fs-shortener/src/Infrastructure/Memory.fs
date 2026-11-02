namespace FsShortener.Infrastructure

open FsShortener.Infrastructure.MemoryServer
open FsShortener.Shortener.Model

module Memory = 
    let create () : LinkStore =
        let store = MemoryServer()
        {
            Save = fun link -> async {
                let key = link.Code
                let value = link.Url
                store.Set(key, value) 
                if link.Expiration.IsSome then
                    let ttl = link.Expiration.Value
                    let! result = store.Expire(key, ttl)
                    if not result then
                        return Error { Message = "Failed to set expiration"; ErrorCode = 1 }
                    else
                        return Ok ()
                else
                    return Ok ()
            }
            Get = fun key -> async {
                let! result = store.Get key
                let result = 
                    Option.map (
                        fun value -> { Code = key; Url = value; Expiration = None }
                    ) result
                return Ok result
            }
            Exists = fun key -> async {
                let! result = store.Exists key
                return Ok result
            }
        }
