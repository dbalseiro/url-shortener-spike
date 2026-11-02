module FsShortener.Tests.Shortener.EncodingTest

open System
open Xunit
open FsShortener.Shortener.Encoding
open FsShortener.Shortener.Domain
open System.Net


// base62Encode / base62Decode
[<Theory>]
[<InlineData(0L, "0")>]
[<InlineData(9L, "9")>]
[<InlineData(10L, "A")>]
[<InlineData(35L, "Z")>]
[<InlineData(36L, "a")>]
[<InlineData(61L, "z")>]
[<InlineData(62L, "10")>]
[<InlineData(125L, "21")>]
[<InlineData(3844L, "100")>]
let ``base62Encode known values`` (n: int64, expected: string) =
    Assert.Equal(expected, base62Encode n)

[<Theory>]
[<InlineData("0", 0L)>]
[<InlineData("A", 10L)>]
[<InlineData("z", 61L)>]
[<InlineData("10", 62L)>]
[<InlineData("21", 125L)>]
[<InlineData("100", 3844L)>]
let ``base62Decode known values`` (s: string, expected: int64) =
    Assert.Equal(expected, base62Decode s)

[<Fact>]
let ``base62Decode of empty string is zero`` () =
    Assert.Equal(0L, base62Decode "")

[<Theory>]
[<InlineData(0L)>]
[<InlineData(1L)>]
[<InlineData(61L)>]
[<InlineData(62L)>]
[<InlineData(123456789L)>]
[<InlineData(9223372036854775807L)>]
let ``decode (encode n) round-trips`` (n: int64) =
    Assert.Equal(n, n |> base62Encode |> base62Decode)

// generateCode
[<Fact>]
let ``generateCode returns a non-empty alphanumeric code`` () =
    let code = generateCode ()
    Assert.NotEmpty code
    Assert.All(code, fun c -> Assert.True(Char.IsAsciiLetterOrDigit c))

[<Fact>]
let ``generateCode decodes to a value within the Int32 range`` () =
    let value = generateCode () |> base62Decode
    Assert.InRange(value, 0L, int64 Int32.MaxValue)

// mapErrorToStatus
[<Fact>]
let ``InvalidRequest maps to 400 with json body`` () =
    let status, headers, body =
        mapErrorToStatus (InvalidRequest [ InvalidUrl "bad" ])
    Assert.Equal(HttpStatusCode.BadRequest, status)
    Assert.Contains(("Content-Type", "application/json"), headers)
    Assert.Equal<obj>("Invalid request", body.GetType().GetProperty("message").GetValue body)

[<Fact>]
let ``AliasTaken maps to 409`` () =
    let status, headers, body = mapErrorToStatus AliasTaken
    Assert.Equal(HttpStatusCode.Conflict, status)
    Assert.Contains(("Content-Type", "text/plain"), headers)
    Assert.Equal<obj>("Alias already taken", body)

[<Fact>]
let ``RateLimited maps to 429 with Retry-After`` () =
    let status, headers, _ =
        mapErrorToStatus (ShortenError.RateLimited { RetryAfter = 30 })
    Assert.Equal(HttpStatusCode.TooManyRequests, status)
    Assert.Contains(("Retry-After", "30"), headers)