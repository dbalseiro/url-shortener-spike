module FsShortener.Tests.Shortener.DtoTest

open FsShortener.Shortener.Dto
open FsShortener.Shortener.Domain
open FsCheck
open FsCheck.FSharp
open FsCheck.Xunit

type Generators() =
    static member private asciiPrintable() =
        Gen.choose (32, 126) 
        |> Gen.map char
        |> Gen.listOf
        |> Gen.map (fun chars -> new string(List.toArray chars))
    
    static member shortenRequest(): Arbitrary<ShortenRequest> = 
        gen {
            let! url = ArbMap.defaults |> ArbMap.generate<string>
            let! alias = ArbMap.defaults |> ArbMap.generate<string option>
            let! ttl = Gen.choose (1, 3600) |> Gen.optionOf
            return {
                Url = url
                Alias = alias
                TTL = ttl
            }
        } |> Arb.fromGen

    static member shortenResponse(): Arbitrary<ShortenResponse> =
        gen {
            let! code = Generators.asciiPrintable()
            let! url = Generators.asciiPrintable()
            let! expiresAt = 
                    Gen.choose (1, 3600)
                    |> Gen.map (fun days -> System.DateTime(2020, 1, 1).AddDays(float days))
                    |> Gen.optionOf
            return {
                Code = code
                OriginalUrl = url
                ShortUrl = "http://short.url/" + code
                ExpiresAt = expiresAt
            }
        } |> Arb.fromGen

[<Properties(Arbitrary = [| typeof<Generators> |])>]
type RoundtripTests() =
    [<Property>]
    member __.``RequestDto roundtrip``(request: ShortenRequest) =
        Request.ofDomain request
        |> Request.toDomain
        |> (=) request

    [<Property>]
    member __.``ResponseDto roundtrip``(response: ShortenResponse) =
        Response.ofDomain response
        |> Response.toDomain
        |> (=) response
