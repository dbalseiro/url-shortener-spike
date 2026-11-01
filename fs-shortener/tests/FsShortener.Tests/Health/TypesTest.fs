module Health.TypesTest

open Xunit
open FsShortener.Health.Types

[<Fact>]
let ``toResponse Healthy with no reason`` () =
    let result = toResponse Healthy
    let expected = {status = "Healthy"; reason = None}
    Assert.Equal(expected, result)

[<Theory>]
[<InlineData("redis offline")>]
[<InlineData("timeout")>]
let ``toResponse Healthy with a reason`` reason =
    let result = toResponse (Unhealthy reason)
    let expected = {status = "Unhealthy"; reason = Some reason}
    Assert.Equal(expected, result)
