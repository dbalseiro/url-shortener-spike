# Plan: Add Rate Limiting to URL Shortener

Your Ktor URL shortener already has rate-limiting error types defined, but needs the actual enforcement logic. I recommend implementing distributed rate limiting using Redis (already available) with a token bucket algorithm, which provides good protection against bursts while being flexible.

## Steps

1. Create a new `RateLimiter.kt` module to implement token bucket rate-limiting logic using Redis, tracking request counts by client IP address.

2. Add configuration in `application.yaml` for rate-limit thresholds (e.g., requests/window per IP).

3. Integrate rate-limiter checks in `ShortenService` `shortenUrl()` and `getOriginalUrl()` methods, returning the appropriate `RateLimited` error when limits are exceeded.

4. Add rate-limiter factory to `Dependencies.kt` and inject into `ShortenService`.

5. Create unit tests for rate-limiter behavior (rate limit exceeded, reset after window, etc.) in `src/test/kotlin/unit/`.

6. Create integration tests validating end-to-end rate-limiting on both `/shorten` and `/{code}` endpoints.

## Further Considerations

1. **Rate-limiting strategy**: Token bucket (smooth rate, burst-friendly) vs. fixed window (simpler, strict limits)? Or different limits per endpoint?
    > token bucket

2. **Client identification**: Use IP address from request headers or authenticated user ID? Handle proxies/load balancers?
    > IP

3. **Endpoint differentiation**: Same limits for read (`GET /{code}`) and write (`POST /shorten`), or separate limits?
    > same limits
