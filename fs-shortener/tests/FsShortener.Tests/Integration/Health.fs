[<Xunit.Trait("Category", "Integration")>]
module FsShortener.Tests.Integration.Health

open Xunit
open System.Net

open FsShortener.Tests.Integration.TestApp

[<Fact>]
let ``Health check returns OK`` () =
    withClient {
        let! response = get "/health"
        Assert.Equal(HttpStatusCode.OK, response.StatusCode)
    }