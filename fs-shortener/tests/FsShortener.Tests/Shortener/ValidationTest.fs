module Shortener.ValidationTest

open Xunit
open FsShortener.Shortener.Validation

/// Tests for the FsShortener.Shortener.Validation module

[<Fact>]
let ``validUrl returns None for a valid HTTP URL`` () =
    let result = validUrl "http://example.com"
    Assert.Equal(None, result)

[<Fact>]
let ``validUrl returns None for a valid HTTPS URL`` () =
    let result = validUrl "https://example.com"
    Assert.Equal(None, result)

[<Fact>]
let ``validUrl returns Some for an invalid URL`` () =
    let result = validUrl "invalid-url"
    Assert.Equal(Some "Invalid URL invalid-url", result)

[<Fact>]
let ``validUrl returns Some for an unsupported scheme`` () =
    let result = validUrl "ftp://example.com"
    Assert.Equal(Some "Schema not supported - only HTTP and HTTPS are allowed", result)

[<Fact>]
let ``validAlias returns true for a valid alias`` () =
    let result = validAlias "valid-alias123"
    Assert.True(result)

[<Fact>]
let ``validAlias returns false for an invalid alias`` () =
    let result = validAlias "invalid alias!"
    Assert.False(result)

[<Fact>]
let ``validTTL returns true for a positive TTL`` () =
    let result = validTTL 10
    Assert.True(result)

[<Fact>]
let ``validTTL returns false for a non-positive TTL`` () =
    let result = validTTL 0
    Assert.False(result)

[<Fact>]
let ``validTTL returns false for a negative TTL`` () =
    let result = validTTL -5
    Assert.False(result)

