module FsShortener.Tests.Shortener.MemoryTest

open System.Threading.Tasks
open Xunit
open FsShortener.Infrastructure
open FsShortener.Shortener.Model

let private link code url expiration =
    { Code = code; Url = url; Expiration = expiration }

let private unwrap result =
    match result with
    | Ok value -> value
    | Error e -> failwithf "Unexpected error: %s" e.Message

let private save (store: LinkStore) l =
    async {
        let! r = store.Save l
        unwrap r
    }

let private get (store: LinkStore) code =
    async {
        let! r = store.Get code
        return unwrap r
    }

let private exists (store: LinkStore) code =
    async {
        let! r = store.Exists code
        return unwrap r
    }

// MemoryServer expires keys with second granularity
let private waitForExpiry () = Task.Delay 1200

[<Fact>]
let ``Get returns None for an unknown code`` () = task {
    let store = Memory.create ()
    let! result = get store "missing"
    Assert.True(result.IsNone)
}

[<Fact>]
let ``Exists returns false for an unknown code`` () = task {
    let store = Memory.create ()
    let! result = exists store "missing"
    Assert.False(result)
}

[<Fact>]
let ``Save then Get returns the saved link`` () = task {
    let store = Memory.create ()
    do! save store (link "abc" "https://example.com" None)

    let! result = get store "abc"
    Assert.True(result.IsSome)
    Assert.Equal("abc", result.Value.Code)
    Assert.Equal("https://example.com", result.Value.Url)
}

[<Fact>]
let ``Save then Exists returns true`` () = task {
    let store = Memory.create ()
    do! save store (link "abc" "https://example.com" None)

    let! result = exists store "abc"
    Assert.True(result)
}

[<Fact>]
let ``Save without expiration keeps the link available`` () = task {
    let store = Memory.create ()
    do! save store (link "abc" "https://example.com" None)
    do! Task.Delay 50

    let! result = exists store "abc"
    Assert.True(result)
}

[<Fact>]
let ``Save with a future expiration keeps the link available`` () = task {
    let store = Memory.create ()
    do! save store (link "abc" "https://example.com" (Some 60))

    let! existsResult = exists store "abc"
    let! getResult = get store "abc"
    Assert.True(existsResult)
    Assert.True(getResult.IsSome)
}

[<Fact>]
let ``Get returns None once the link has expired`` () = task {
    let store = Memory.create ()
    do! save store (link "abc" "https://example.com" (Some 1))
    do! waitForExpiry ()

    let! result = get store "abc"
    Assert.True(result.IsNone)
}

[<Fact>]
let ``Exists returns false once the link has expired`` () = task {
    let store = Memory.create ()
    do! save store (link "abc" "https://example.com" (Some 1))
    do! waitForExpiry ()

    let! result = exists store "abc"
    Assert.False(result)
}

[<Fact>]
let ``Save with the same code overwrites the previous url`` () = task {
    let store = Memory.create ()
    do! save store (link "abc" "https://one.example" None)
    do! save store (link "abc" "https://two.example" None)

    let! result = get store "abc"
    Assert.Equal("https://two.example", result.Value.Url)
}

[<Fact>]
let ``Links with different codes are independent`` () = task {
    let store = Memory.create ()
    do! save store (link "a" "https://a.example" None)
    do! save store (link "b" "https://b.example" (Some 1))
    do! waitForExpiry ()

    let! a = get store "a"
    let! b = get store "b"
    Assert.Equal("https://a.example", a.Value.Url)
    Assert.True(b.IsNone)
}

[<Fact>]
let ``Stores created separately do not share state`` () = task {
    let first = Memory.create ()
    let second = Memory.create ()
    do! save first (link "abc" "https://example.com" None)

    let! result = exists second "abc"
    Assert.False(result)
}

[<Fact>]
let ``Concurrent saves are all stored`` () = task {
    let store = Memory.create ()
    let codes = [ for i in 1..200 -> sprintf "code%d" i ]

    let! _ =
        codes
        |> List.map (fun c -> save store (link c ("https://example.com/" + c) None))
        |> Async.Parallel

    let! results = codes |> List.map (exists store) |> Async.Parallel
    Assert.All(results, fun r -> Assert.True(r))
}
