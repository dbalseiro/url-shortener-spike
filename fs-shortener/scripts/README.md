# F# Rate-Limit Black-Box Tests

Run from the repository root with k6 installed. The test targets the existing
Compose F# app and Redis; it does not inspect or reset Redis or change the app.

```sh
docker compose up -d --build
k6 run fs-shortener/scripts/rate-limit.k6.js
```

Run one phase to investigate a failure:

```sh
k6 run -e SCENARIO=baseline fs-shortener/scripts/rate-limit.k6.js
k6 run -e SCENARIO=burst fs-shortener/scripts/rate-limit.k6.js
k6 run -e SCENARIO=recovery fs-shortener/scripts/rate-limit.k6.js
k6 run -e SCENARIO=sustained fs-shortener/scripts/rate-limit.k6.js
```

The default `all` run takes about 75 seconds on a responsive app. It checks:

- Baseline: a request succeeds after the bucket has refilled.
- Burst: 20 concurrent requests allow the initial 10-token budget, plus any
  elapsed refill, and reject excess traffic.
- Recovery: an exhausted bucket rejects, then allows after one token refills.
- Sustained: 5 requests/second for 30 seconds yields approximately the initial
  capacity plus one successful request per elapsed second, with excess rejected.
- Every rejection: `429`, a positive integer `Retry-After`, and JSON with
  `error: "RateLimited"` and a nonempty `message`.

Expected `429` responses do not count as HTTP failures. `allowed_requests`,
`limited_requests`, and `unexpected_responses` distinguish outcomes; failed
checks or unexpected statuses make k6 exit nonzero. Unexpected response bodies
are logged with a 250-character limit for diagnosis.

Settings (pass with `-e NAME=value`):

| Setting | Default | Meaning |
| --- | --- | --- |
| `BASE_URL` | `http://localhost:8080` | F# app URL |
| `SCENARIO` | `all` | `baseline`, `burst`, `recovery`, or `sustained` |
| `CAPACITY` | `10` | Expected configured bucket capacity |
| `REFILL_PER_SECOND` | `1` | Expected configured token refill rate |
| `REQUESTS_PER_SECOND` | `5` | Sustained offered rate, greater than refill |
| `LOAD_SECONDS` | `30` | Sustained duration |

These settings describe expectations; they do not configure the server. Each
phase starts after capacity/refill + 1 seconds of idle time. Avoid browser,
health-probe, or other traffic from the same client during the run, since it can
consume the same bucket. Local virtual users normally share a client IP, so this
tests a shared bucket, not isolation between client IPs. No spoofed forwarded
headers are used.

Successful requests create short links with a requested 300-second TTL. No alias
is supplied. Redirect following is disabled. Sustained pacing uses one VU; if
the app is slow, this cannot maintain the offered rate and the duration check
fails rather than presenting it as a valid rate-limit result. This is a
functional limiter check, not a throughput benchmark.