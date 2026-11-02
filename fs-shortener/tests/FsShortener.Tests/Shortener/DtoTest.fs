module FsShortener.Tests.Shortener.DtoTest

open System
open Xunit
open FsShortener.Shortener.Dto
open FsShortener.Shortener.Domain

// Roundtrip tests for ShortenRequestDto
module RequestRoundtrip =
    [<Fact>]
    let ``domain -> dto -> domain preserves all fields`` () =
        let domain = {
            ShortenRequest.Url = "https://example.com"
            ShortenRequest.Alias = Some "myalias"
            ShortenRequest.TTL = Some 3600
        }
        
        let dto = Request.ofDomain domain
        let roundtripped = Request.toDomain dto
        
        Assert.Equal(domain.Url, roundtripped.Url)
        Assert.Equal(domain.Alias, roundtripped.Alias)
        Assert.Equal(domain.TTL, roundtripped.TTL)

    [<Fact>]
    let ``domain -> dto -> domain with None alias`` () =
        let domain = {
            ShortenRequest.Url = "https://example.com"
            ShortenRequest.Alias = None
            ShortenRequest.TTL = Some 1800
        }
        
        let dto = Request.ofDomain domain
        let roundtripped = Request.toDomain dto
        
        Assert.Equal(domain.Url, roundtripped.Url)
        Assert.Equal(domain.Alias, roundtripped.Alias)
        Assert.Equal(domain.TTL, roundtripped.TTL)

    [<Fact>]
    let ``domain -> dto -> domain with None TTL`` () =
        let domain = {
            ShortenRequest.Url = "https://example.com"
            ShortenRequest.Alias = Some "test"
            ShortenRequest.TTL = None
        }
        
        let dto = Request.ofDomain domain
        let roundtripped = Request.toDomain dto
        
        Assert.Equal(domain.Url, roundtripped.Url)
        Assert.Equal(domain.Alias, roundtripped.Alias)
        Assert.Equal(domain.TTL, roundtripped.TTL)

    [<Fact>]
    let ``domain -> dto -> domain with all None`` () =
        let domain = {
            ShortenRequest.Url = "http://test.example.com"
            ShortenRequest.Alias = None
            ShortenRequest.TTL = None
        }
        
        let dto = Request.ofDomain domain
        let roundtripped = Request.toDomain dto
        
        Assert.Equal(domain.Url, roundtripped.Url)
        Assert.Equal(domain.Alias, roundtripped.Alias)
        Assert.Equal(domain.TTL, roundtripped.TTL)

    [<Fact>]
    let ``ofDomain converts Some alias to non-null`` () =
        let domain = {
            ShortenRequest.Url = "https://example.com"
            ShortenRequest.Alias = Some "myalias"
            ShortenRequest.TTL = Some 7200
        }
        
        let dto = Request.ofDomain domain
        
        Assert.Equal("https://example.com", dto.Url)
        Assert.Equal("myalias", dto.Alias)
        Assert.True(dto.TTLSeconds.HasValue)
        Assert.Equal(7200, dto.TTLSeconds.Value)

    [<Fact>]
    let ``ofDomain converts None to null`` () =
        let domain = {
            ShortenRequest.Url = "https://example.com"
            ShortenRequest.Alias = None
            ShortenRequest.TTL = None
        }
        
        let dto = Request.ofDomain domain
        
        Assert.Null(dto.Alias)
        Assert.False(dto.TTLSeconds.HasValue)

    [<Fact>]
    let ``toDomain converts null alias to None`` () =
        let dto = { ShortenRequestDto.Url = "https://example.com"; Alias = null; TTLSeconds = Nullable() }
        let domain = Request.toDomain dto
        
        Assert.Equal(None, domain.Alias)

    [<Fact>]
    let ``toDomain converts null TTL to None`` () =
        let dto = { ShortenRequestDto.Url = "https://example.com"; Alias = null; TTLSeconds = Nullable() }
        let domain = Request.toDomain dto
        
        Assert.Equal(None, domain.TTL)

// Roundtrip tests for ShortenResponseDto
module ResponseRoundtrip =
    [<Fact>]
    let ``domain -> dto -> domain preserves all fields`` () =
        let expiresAt = DateTime(2025, 12, 25, 10, 30, 45, DateTimeKind.Utc)
        let domain = {
            ShortenResponse.Code = "abc123"
            ShortenResponse.OriginalUrl = "https://example.com/page"
            ShortenResponse.ExpiresAt = Some expiresAt
        }
        
        let dto = Response.ofDomain domain
        let roundtripped = Response.toDomain dto
        
        Assert.Equal(domain.Code, roundtripped.Code)
        Assert.Equal(domain.OriginalUrl, roundtripped.OriginalUrl)
        Assert.Equal(domain.ExpiresAt, roundtripped.ExpiresAt)

    [<Fact>]
    let ``domain -> dto -> domain with None expiresAt`` () =
        let domain = {
            ShortenResponse.Code = "xyz789"
            ShortenResponse.OriginalUrl = "https://another.example.com"
            ShortenResponse.ExpiresAt = None
        }
        
        let dto = Response.ofDomain domain
        let roundtripped = Response.toDomain dto
        
        Assert.Equal(domain.Code, roundtripped.Code)
        Assert.Equal(domain.OriginalUrl, roundtripped.OriginalUrl)
        Assert.Equal(None, roundtripped.ExpiresAt)
        Assert.False(dto.ExpiresAt.HasValue)

    [<Fact>]
    let ``ofDomain converts Some DateTime to Unix timestamp and back`` () =
        let expiresAt = DateTime(2026, 6, 15, 14, 0, 0, DateTimeKind.Utc)
        let domain = {
            ShortenResponse.Code = "test"
            ShortenResponse.OriginalUrl = "https://test.com"
            ShortenResponse.ExpiresAt = Some expiresAt
        }
        
        let dto = Response.ofDomain domain
        let roundtripped = Response.toDomain dto
        
        Assert.True(dto.ExpiresAt.HasValue)
        Assert.Equal(domain.ExpiresAt, roundtripped.ExpiresAt)

    [<Fact>]
    let ``ofDomain converts None to nullable`` () =
        let domain = {
            ShortenResponse.Code = "noexp"
            ShortenResponse.OriginalUrl = "https://example.com"
            ShortenResponse.ExpiresAt = None
        }
        
        let dto = Response.ofDomain domain
        
        Assert.False(dto.ExpiresAt.HasValue)

    [<Fact>]
    let ``toDomain handles null ExpiresAt`` () =
        let dto = { ShortenResponseDto.Code = "abc"; OriginalUrl = "https://example.com"; ExpiresAt = Nullable() }
        let domain = Response.toDomain dto
        
        Assert.Equal(None, domain.ExpiresAt)

    [<Fact>]
    let ``toDomain handles zero Unix timestamp`` () =
        let dto = {
            ShortenResponseDto.Code = "zero"
            ShortenResponseDto.OriginalUrl = "https://example.com"
            ShortenResponseDto.ExpiresAt = Nullable 0
        }
        
        let domain = Response.toDomain dto
        
        let expected = DateTimeOffset.FromUnixTimeSeconds(0L).UtcDateTime
        Assert.Equal(expected, domain.ExpiresAt.Value)

    [<Fact>]
    let ``toDomain handles large Unix timestamp`` () =
        let largeTimestamp = int32 Int32.MaxValue
        let dto = {
            ShortenResponseDto.Code = "large"
            ShortenResponseDto.OriginalUrl = "https://example.com"
            ShortenResponseDto.ExpiresAt = Nullable largeTimestamp
        }
        
        let domain = Response.toDomain dto
        
        let expected = DateTimeOffset.FromUnixTimeSeconds(int64 largeTimestamp).UtcDateTime
        Assert.Equal(expected, domain.ExpiresAt.Value)
