module FsShortener.Tests.Shortener.ServiceTest

open System
open System.Threading.Tasks
open Xunit
open FsShortener.Shortener
open FsShortener.Shortener.Model
open FsShortener.Shortener.Domain
open FsShortener.Infrastructure

let private createRequest url alias ttl =
    { Url = url; Alias = alias; TTL = ttl }

let private createStorageError message code =
    { Message = message; ErrorCode = code }

let private createSuccessStore () : LinkStore =
    {
        Save = fun _ -> Task.FromResult(Ok ())
        Get = fun _ -> Task.FromResult(Ok None)
        Exists = fun _ -> Task.FromResult(Ok false)
    }

let private createFailingStore (errorMsg: string) (code: int) : LinkStore =
    {
        Save = fun _ -> Task.FromResult(Error (createStorageError errorMsg code))
        Get = fun _ -> Task.FromResult(Error (createStorageError errorMsg code))
        Exists = fun _ -> Task.FromResult(Error (createStorageError errorMsg code))
    }

let private createAliasConflictStore (alias: string) (url: string) : LinkStore =
    {
        Save = fun _ -> Task.FromResult(Ok ())
        Get = fun code ->
            if code = alias then
                Task.FromResult(Ok (Some { Code = code; Url = url; Expiration = None }))
            else
                Task.FromResult(Ok None)
        Exists = fun _ -> Task.FromResult(Ok false)
    }

// Valid requests
[<Fact>]
let ``shorten with valid HTTPS URL returns Ok`` () = task {
    let store = createSuccessStore ()
    let request = createRequest "https://example.com" None None
    let! result = Service.shorten store "http://localhost:8080" request
    match result with
    | Ok response ->
        Assert.NotNull(response.Code)
        Assert.Equal("https://example.com", response.OriginalUrl)
    | Error _ -> Assert.True(false, "Expected Ok response")
}

[<Fact>]
let ``shorten with custom alias uses it`` () = task {
    let store = createSuccessStore ()
    let request = createRequest "https://example.com" (Some "myalias") None
    let! result = Service.shorten store "http://localhost:8080" request
    match result with
    | Ok response ->
        Assert.Equal("myalias", response.Code)
    | Error _ -> Assert.True(false, "Expected Ok response")
}

[<Fact>]
let ``shorten with TTL sets expiration`` () = task {
    let store = createSuccessStore ()
    let request = createRequest "https://example.com" None (Some 3600)
    let! result = Service.shorten store "http://localhost:8080" request
    match result with
    | Ok response ->
        Assert.NotNull(response.ExpiresAt)
    | Error _ -> Assert.True(false, "Expected Ok response")
}

[<Fact>]
let ``shorten generates different codes`` () = task {
    let store = createSuccessStore ()
    let! r1 = Service.shorten store "http://localhost:8080" (createRequest "https://example1.com" None None)
    let! r2 = Service.shorten store "http://localhost:8080" (createRequest "https://example2.com" None None)
    match r1, r2 with
    | Ok resp1, Ok resp2 -> Assert.NotEqual<string>(resp1.Code, resp2.Code)
    | _ -> Assert.True(false, "Expected Ok responses")
}

// Invalid requests
[<Fact>]
let ``shorten with invalid URL returns error`` () = task {
    let store = createSuccessStore ()
    let request = createRequest "not-a-url" None None
    let! result = Service.shorten store "http://localhost:8080" request
    match result with
    | Error (InvalidRequest violations) -> Assert.NotEmpty(violations)
    | _ -> Assert.True(false, "Expected InvalidRequest error")
}

[<Fact>]
let ``shorten with invalid alias returns error`` () = task {
    let store = createSuccessStore ()
    let request = createRequest "https://example.com" (Some "bad alias!") None
    let! result = Service.shorten store "http://localhost:8080" request
    match result with
    | Error (InvalidRequest violations) ->
        Assert.Contains(violations, fun v ->
            match v with InvalidAlias -> true | _ -> false
        )
    | _ -> Assert.True(false, "Expected InvalidRequest error")
}

[<Fact>]
let ``shorten with zero TTL returns error`` () = task {
    let store = createSuccessStore ()
    let request = createRequest "https://example.com" None (Some 0)
    let! result = Service.shorten store "http://localhost:8080" request
    match result with
    | Error (InvalidRequest violations) -> Assert.NotEmpty(violations)
    | _ -> Assert.True(false, "Expected InvalidRequest error")
}

[<Fact>]
let ``shorten with negative TTL returns error`` () = task {
    let store = createSuccessStore ()
    let request = createRequest "https://example.com" None (Some -100)
    let! result = Service.shorten store "http://localhost:8080" request
    match result with
    | Error (InvalidRequest violations) -> Assert.NotEmpty(violations)
    | _ -> Assert.True(false, "Expected InvalidRequest error")
}

[<Fact>]
let ``shorten with multiple invalid fields returns multiple violations`` () = task {
    let store = createSuccessStore ()
    let request = createRequest "invalid-url" (Some "bad!") (Some -5)
    let! result = Service.shorten store "http://localhost:8080" request
    match result with
    | Error (InvalidRequest violations) ->
        Assert.True(violations.Length >= 2, "Expected multiple violations")
    | _ -> Assert.True(false, "Expected InvalidRequest error")
}

// Alias conflicts
[<Fact>]
let ``shorten returns AliasTaken when alias exists`` () = task {
    let store = createAliasConflictStore "existing" "https://taken.com"
    let request = createRequest "https://example.com" (Some "existing") None
    let! result = Service.shorten store "http://localhost:8080" request
    match result with
    | Error (AliasTaken takenUrl) -> Assert.Equal("https://taken.com", takenUrl)
    | _ -> Assert.True(false, "Expected AliasTaken error")
}

[<Fact>]
let ``shorten succeeds with available alias`` () = task {
    let store = createAliasConflictStore "taken" "https://taken.com"
    let request = createRequest "https://example.com" (Some "available") None
    let! result = Service.shorten store "http://localhost:8080" request
    match result with
    | Ok response -> Assert.Equal("available", response.Code)
    | Error _ -> Assert.True(false, "Expected Ok response")
}

// Storage errors
[<Fact>]
let ``shorten returns StorageError on Get failure`` () = task {
    let store = createFailingStore "DB connection failed" 500
    let request = createRequest "https://example.com" (Some "test") None
    let! result = Service.shorten store "http://localhost:8080" request
    match result with
    | Error (ShortenError.StorageError msg) ->
        Assert.Contains("Error:", msg)
        Assert.Contains("500", msg)
    | _ -> Assert.True(false, "Expected StorageError")
}

[<Fact>]
let ``shorten returns StorageError on Save failure`` () = task {
    let failingSave: LinkStore = {
        Save = fun _ -> Task.FromResult(Error (createStorageError "Write failed" 503))
        Get = fun _ -> Task.FromResult(Ok None)
        Exists = fun _ -> Task.FromResult(Ok false)
    }
    let request = createRequest "https://example.com" None None
    let! result = Service.shorten failingSave "http://localhost:8080" request
    match result with
    | Error (ShortenError.StorageError msg) -> Assert.Contains("Write failed", msg)
    | _ -> Assert.True(false, "Expected StorageError")
}

// Integration
[<Fact>]
let ``shorten persists to store and can be retrieved`` () = task {
    let store = Memory.create ()
    let request = createRequest "https://integration.example.com" (Some "inttest") None
    let! result = Service.shorten store "http://localhost:8080" request
    match result with
    | Ok _ ->
        let! getResult = store.Get "inttest"
        match getResult with
        | Ok (Some link) -> Assert.Equal("https://integration.example.com", link.Url)
        | _ -> Assert.True(false, "Expected link in store")
    | Error _ -> Assert.True(false, "Expected success")
}

[<Fact>]
let ``shorten respects TTL expiration`` () = task {
    let store = Memory.create ()
    let request = createRequest "https://ttl.example.com" (Some "ttltest") (Some 1)
    let! result = Service.shorten store "http://localhost:8080" request
    match result with
    | Ok _ ->
        let! exists1 = store.Exists "ttltest"
        Assert.Equal(Ok true, exists1)
        do! Task.Delay 1200
        let! exists2 = store.Exists "ttltest"
        Assert.Equal(Ok false, exists2)
    | Error _ -> Assert.True(false, "Expected success")
}

[<Fact>]
let ``shorten handles concurrent requests`` () = task {
    let store = Memory.create ()
    let requests = [
        createRequest "https://example1.com" (Some "alias1") None
        createRequest "https://example2.com" (Some "alias2") None
        createRequest "https://example3.com" (Some "alias3") None
    ]
    let! results = requests |> List.map (Service.shorten store "http://localhost:8080") |> Task.WhenAll
    let successCount = results |> Array.filter (fun r -> r.IsOk) |> Array.length
    Assert.Equal(3, successCount)
}
