[<Xunit.Trait("Category", "Integration")>]
module Integration.Health

open Xunit
open Integration.TestApp
open System.Net

[<Fact>]
let ``Health check returns OK`` () =
    withClient {
        let! response = get "/health"
        Assert.Equal(HttpStatusCode.OK, response.StatusCode)
    }