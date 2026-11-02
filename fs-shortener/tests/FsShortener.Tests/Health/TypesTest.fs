module FsShortener.Tests.Health.TypesTest

open Xunit
open FsShortener.Health.Domain

[<Fact>]
let ``toResponse Healthy with no reason`` () =
    let result = toResponse Healthy
    let expected = {Status = "Healthy"; Reason = None}
    Assert.Equal(expected, result)

[<Theory>]
[<InlineData("redis offline")>]
[<InlineData("timeout")>]
let ``toResponse Healthy with a reason`` reason =
    let result = toResponse (Unhealthy reason)
    let expected = {Status = "Unhealthy"; Reason = Some reason}
    Assert.Equal(expected, result)
