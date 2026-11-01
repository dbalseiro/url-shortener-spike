module FsShortener.Health.Domain

type Status = 
    | Healthy
    | Unhealthy of string

type HealthResponse = { Status: string; Reason: string option }

let toResponse: Status -> HealthResponse =
    function
    | Healthy -> { Status = "Healthy"; Reason = None }
    | Unhealthy reason -> { Status = "Unhealthy"; Reason = Some reason }
