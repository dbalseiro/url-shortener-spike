module FsShortener.Infrastructure.RateLimiter

open Microsoft.AspNetCore.Http

type RateLimitMiddleware(next: RequestDelegate, store: Model.LinkStore, script: string -> Model.Script) =
    member __.InvokeAsync(context: HttpContext) = task {
        let clientIp = match context.Connection.RemoteIpAddress with
                       | null -> "unknown"
                       | ip -> ip.ToString()
        match! store.ExecuteScript (script clientIp) with
        | Ok status -> 
            if status = 1
            then do! next.Invoke context
            else 
                context.Response.Headers.["Retry-After"] <- "60"
                context.Response.StatusCode <- 429
        | Error e ->
            context.Response.StatusCode <- 500
            do! context.Response.WriteAsync("Error executing rate limit script: " + e.Message)
    }