namespace FsShortener.Infrastructure

open System
open System.Text.Json
open StackExchange.Redis

module Redis =
    type RedisConfig = {
        Host: string
        Port: int
    }

    let defaultConfig = { Host = "localhost"; Port = 6379 }

    let create (config: RedisConfig) : Model.LinkStore =
        let connectionString = sprintf "%s:%d" config.Host config.Port
        
        let options = ConfigurationOptions.Parse connectionString
        let mux = ConnectionMultiplexer.Connect options
        let db = mux.GetDatabase()

        let keyPrefix = "link:"

        let serializeLink (link: Model.Link) : string =
            JsonSerializer.Serialize {| 
                url = link.Url; 
                code = link.Code; 
                expiration = link.Expiration 
            |}

        let deserializeLink (code: string) (json: string) : Model.Link =
            let data = 
                JsonSerializer.Deserialize<{| 
                    url: string; 
                    code: string; 
                    expiration: int option 
                |}>json
            { Model.Url = data.url; Code = code; Expiration = data.expiration }

        {
            Save = fun link -> task {
                try
                    let key = RedisKey(keyPrefix + link.Code)
                    let value = RedisValue(serializeLink link)
                    
                    if link.Expiration.IsSome then
                        let ttl = TimeSpan.FromSeconds(float link.Expiration.Value)
                        db.StringSet(key, value, ttl) |> ignore
                    else
                        db.StringSet(key, value) |> ignore
                    
                    return Ok ()
                with ex ->
                    return Error { Message = sprintf "Failed to save link: %s" ex.Message; ErrorCode = 1 }
            }
            
            Get = fun code -> task {
                try
                    let key = RedisKey(keyPrefix + code)
                    let value = db.StringGet key
                    
                    if value.IsNullOrEmpty then
                        return Ok None
                    else
                        let link = deserializeLink code (value.ToString())
                        return Ok (Some link)
                with ex ->
                    return Error { Message = sprintf "Failed to get link: %s" ex.Message; ErrorCode = 2 }
            }
            
            Exists = fun code -> task {
                try
                    let key = RedisKey(keyPrefix + code)
                    let exists = db.KeyExists key
                    return Ok exists
                with ex ->
                    return Error { Message = sprintf "Failed to check existence: %s" ex.Message; ErrorCode = 3 }
            }
            ExecuteScript = fun script -> task {
                try
                    let result = db.ScriptEvaluate (
                                    script.ScriptCode,
                                    script.ScriptKeys |> List.map RedisKey |> Array.ofList,
                                    script.ScriptArgs |> List.map RedisValue |> Array.ofList
                                )
                    return Ok (int32 result)
                with ex ->
                    return Error { Message = sprintf "Failed to execute script: %s" ex.Message; ErrorCode = 4 }
            }
        }

    let createDefault () : Model.LinkStore =
        create defaultConfig
