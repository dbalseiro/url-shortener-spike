module FsShortener.Infrastructure.MemoryServer

type private StoredValue = {
    Value: string
    ExpiresAt: System.DateTime option
}

let inline private expired (item: StoredValue) =
    let now = System.DateTime.UtcNow
    item.ExpiresAt.IsSome && item.ExpiresAt.Value <= now

type private InternalCommand =
    | Set of string * string
    | Get of string * AsyncReplyChannel<string option>
    | Exists of string * AsyncReplyChannel<bool>
    | Expire of string * int * AsyncReplyChannel<bool>

type MemoryServer() =
    let db = System.Collections.Concurrent.ConcurrentDictionary<string, StoredValue>()
    let agent = MailboxProcessor<InternalCommand>.Start(fun inbox ->
        let rec loop () = async {
            let! msg = inbox.Receive()

            // Handle messages here
            match msg with
            | Set (key, value) ->
                db.[key] <- { Value = value; ExpiresAt = None }
                return! loop ()
            | Get (key, reply) ->
                match db.TryGetValue key with
                | true, item when not (expired item) ->
                    reply.Reply(Some item.Value)
                | true, _ ->
                    // Item has expired
                    db.TryRemove key |> ignore
                    reply.Reply None
                | false, _ ->
                    reply.Reply None
                return! loop ()
            | Exists (key, reply) ->
                match db.TryGetValue key with
                | true, item when not (expired item) ->
                    reply.Reply true
                | true, _ ->
                    // Item has expired
                    db.TryRemove key |> ignore
                    reply.Reply false
                | false, _ ->
                    reply.Reply false
                return! loop ()
            | Expire (key, seconds, reply) ->
                match db.TryGetValue key with
                | true, item when not (expired item) ->
                    let expiresAt = System.DateTime.UtcNow.AddSeconds(float seconds)
                    db.[key] <- { item with ExpiresAt = Some expiresAt }
                    reply.Reply true
                | true, _ ->
                    // Item has expired
                    db.TryRemove key |> ignore
                    reply.Reply false
                | false, _ ->
                    reply.Reply false
                return! loop ()
        }
        loop ()
    )

    member __.Set(key: string, value: string) = agent.Post(Set(key, value))
    member __.Get(key: string) = agent.PostAndAsyncReply(fun reply -> Get(key, reply))
    member __.Exists(key: string) = agent.PostAndAsyncReply(fun reply -> Exists(key, reply))
    member __.Expire(key: string, seconds: int) = agent.PostAndAsyncReply(fun reply -> Expire(key, seconds, reply))
