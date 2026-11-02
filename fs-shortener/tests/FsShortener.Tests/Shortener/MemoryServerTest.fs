module FsShortener.Tests.Shortener.MemoryServerTest

open System
open System.Threading.Tasks
open Xunit
open FsShortener.Infrastructure.MemoryServer

let inline toTask (asyncValue: Async<'a>) : Task<'a> =
    Async.StartAsTask asyncValue

[<Fact>]
let ``Set and Get returns the stored value`` () = task {
    let server = MemoryServer()
    server.Set("key1", "value1")
    let! result = server.Get "key1" |> toTask
    Assert.Equal(Some "value1", result)
}

[<Fact>]
let ``Get non-existent key returns None`` () = task {
    let server = MemoryServer()
    let! result = server.Get "nonexistent" |> toTask
    Assert.Equal(None, result)
}

[<Fact>]
let ``Exists returns true for stored key`` () = task {
    let server = MemoryServer()
    server.Set("key1", "value1")
    let! result = server.Exists "key1" |> toTask
    Assert.True result
}

[<Fact>]
let ``Exists returns false for non-existent key`` () = task {
    let server = MemoryServer()
    let! result = server.Exists "nonexistent" |> toTask
    Assert.False result
}

[<Fact>]
let ``Expire returns true and value expires`` () = task {
    let server = MemoryServer()
    server.Set("key1", "value1")
    let! expireResult = server.Expire("key1", 1) |> toTask
    Assert.True expireResult
    
    do! Task.Delay 1100
    
    let! getResult = server.Get "key1" |> toTask
    Assert.Equal(None, getResult)
}

[<Fact>]
let ``Expire non-existent key returns false`` () = task {
    let server = MemoryServer()
    let! result = server.Expire("nonexistent", 10) |> toTask
    Assert.False result
}

// Concurrent test cases

[<Fact>]
let ``Multiple concurrent Sets store all values`` () = task {
    let server = MemoryServer()
    
    for i = 0 to 99 do
        server.Set($"key{i}", $"value{i}")
    
    let! results = 
        seq { 0..99 }
        |> Seq.map (fun i -> server.Get $"key{i}" |> toTask)
        |> Seq.toArray
        |> Task.WhenAll
    
    Assert.All(results, fun result ->
        Assert.NotEqual(None, result)
    )
}

[<Fact>]
let ``Multiple concurrent Gets retrieve correct values`` () = task {
    let server = MemoryServer()
    
    for i = 0 to 9 do
        server.Set($"key{i}", $"value{i}")
    
    let! results = 
        seq { 0..99 }
        |> Seq.map (fun i -> server.Get $"key{i % 10}" |> toTask)
        |> Seq.toArray
        |> Task.WhenAll
    
    Assert.All(results, fun result ->
        Assert.NotEqual(None, result)
        match result with
        | Some v -> Assert.NotEmpty v
        | None -> Assert.True false
    )
}

[<Fact>]
let ``Concurrent Exists checks work correctly`` () = task {
    let server = MemoryServer()
    
    for i = 0 to 49 do
        server.Set($"key{i}", $"value{i}")
    
    let! results = 
        seq { 0..99 }
        |> Seq.map (fun i -> 
            let key = if i % 2 = 0 then $"key{i / 2}" else $"nonexistent{i}"
            (server.Exists key |> toTask).ContinueWith(fun (t: Task<bool>) -> (key, t.Result))
        )
        |> Seq.toArray
        |> Task.WhenAll
    
    Assert.All(results, fun (key, exists) ->
        if key.StartsWith("key") && not (key.Contains("nonexistent")) then
            Assert.True(exists)
        else
            Assert.False(exists)
    )
}

[<Fact>]
let ``Mixed concurrent Set and Get operations`` () = task {
    let server = MemoryServer()
    
    let operations = [|
        for i = 0 to 49 do
            task {
                server.Set($"key{i}", $"value{i}")
                return ()
            } :> Task
        for i = 0 to 49 do
            task {
                let! _ = server.Get $"key{i % 50}" |> toTask
                return ()
            } :> Task
    |]
    
    do! Task.WhenAll operations
    
    for i = 0 to 49 do
        let! result = server.Get $"key{i}" |> toTask
        Assert.Equal(Some $"value{i}", result)
}

[<Fact>]
let ``Concurrent Sets with overwrites`` () = task {
    let server = MemoryServer()
    
    let overwrites =
        [|
            for i = 0 to 99 do
                task {
                    server.Set("shared-key", $"value{i}")
                } :> Task
        |]
    
    do! Task.WhenAll overwrites
    
    let! result = server.Get "shared-key" |> toTask
    Assert.NotEqual(None, result)
}

[<Fact>]
let ``Concurrent Expire operations`` () = task {
    let server = MemoryServer()
    
    for i = 0 to 49 do
        server.Set($"key{i}", $"value{i}")
    
    let expires = 
        seq { 0..49 }
        |> Seq.map (fun i -> task {
            let! result = server.Expire($"key{i}", 1) |> toTask
            return result
        })
        |> Seq.toArray
        |> Task.WhenAll
    
    let! results = expires
    
    Assert.All(results, Assert.True)
    
    do! Task.Delay 1100
    
    for i = 0 to 49 do
        let! result = server.Get $"key{i}" |> toTask
        Assert.Equal(None, result)
}

[<Fact>]
let ``Concurrent Get and Expire on same key`` () = task {
    let server = MemoryServer()
    server.Set("key", "value")
    
    let operations = [|
        task {
            let! _ = server.Get "key" |> toTask
            return ()
        } :> Task
        task {
            let! _ = server.Expire("key", 10) |> toTask
            return ()
        } :> Task
        task {
            let! _ = server.Get "key" |> toTask
            return ()
        } :> Task
        task {
            let! _ = server.Exists "key" |> toTask
            return ()
        } :> Task
    |]
    
    do! Task.WhenAll operations
    
    let! exists = server.Exists "key" |> toTask
    Assert.True exists
}

[<Fact>]
let ``High concurrency stress test`` () = task {
    let server = MemoryServer()
    let keyCount = 20
    let operationsPerKey = 100
    
    let operations = [|
        for i = 0 to keyCount - 1 do
            for j = 0 to operationsPerKey - 1 do
                match j % 3 with
                | 0 -> 
                    task {
                        server.Set($"key{i}", $"value{i}-{j}")
                        return ()
                    } :> Task
                | 1 ->
                    task {
                        let! _ = server.Get $"key{i}" |> toTask
                        return ()
                    } :> Task
                | _ ->
                    task {
                        let! _ = server.Exists $"key{i}" |> toTask
                        return ()
                    } :> Task
    |]
    
    do! Task.WhenAll operations
    
    for i = 0 to keyCount - 1 do
        let! exists = server.Exists $"key{i}" |> toTask
        Assert.True exists
}

[<Fact>]
let ``Concurrent operations with various expiration times`` () = task {
    let server = MemoryServer()
    
    let operations = [|
        for i = 0 to 19 do
            task {
                server.Set($"key{i}", $"value{i}")
                let expiryTime = 1 + (i % 3)
                let! _ = server.Expire($"key{i}", expiryTime) |> toTask
                return ()
            } :> Task
        for i = 0 to 99 do
            task {
                let! _ = server.Get $"key{i % 20}" |> toTask
                return ()
            } :> Task
    |]
    
    do! Task.WhenAll operations
    
    do! Task.Delay 1500
    
    let! results = 
        seq { 0..19 }
        |> Seq.map (fun i -> server.Get $"key{i}" |> toTask)
        |> Seq.toArray
        |> Task.WhenAll
    
    let expiredCount = results |> Array.filter (fun r -> r = None) |> Array.length
    Assert.True(expiredCount > 0)
}
