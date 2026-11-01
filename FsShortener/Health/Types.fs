module FsShortener.Health.Types

type Status = 
    | Healthy
    | Unhealthy of string

type HealthResponse = { status: string; reason: string option }

let toResponse: Status -> HealthResponse =
    function
    | Healthy -> { status = "Healthy"; reason = None }
    | Unhealthy reason -> { status = "Unhealthy"; reason = Some reason }
