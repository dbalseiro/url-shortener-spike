package com.jurisfutura

import com.jurisfutura.shortener.configureRouting
import com.jurisfutura.shortener.configureSerialization
import com.jurisfutura.shortener.configureStatusPages
import com.jurisfutura.shortener.redis.configureRedis
import io.ktor.client.request.get
import io.ktor.client.request.header
import io.ktor.client.request.post
import io.ktor.client.request.setBody
import io.ktor.http.ContentType
import io.ktor.http.HttpStatusCode
import io.ktor.server.testing.testApplication
import kotlin.test.Test
import kotlin.test.assertEquals

class RateLimitingIntegrationTest {

    @Test
    fun `shorten endpoint respects rate limit`() = testApplication {
        application {
            configureSerialization()
            configureStatusPages()
            configureRouting()
            // Note: configureRedis and configureMonitoring would need special handling for tests
        }

        val client = this.client
        val clientIp = "192.168.1.100"

        // Make successful requests up to the limit
        repeat(10) {
            val response = client.post("/shorten") {
                header("X-Forwarded-For", clientIp)
                contentType(ContentType.Application.Json)
                setBody("""{"url":"https://example.com/path/$it"}""")
            }
            assertEquals(HttpStatusCode.OK, response.status, "Request $it should succeed")
        }

        // Next request should be rate limited
        val rateLimitedResponse = client.post("/shorten") {
            header("X-Forwarded-For", clientIp)
            contentType(ContentType.Application.Json)
            setBody("""{"url":"https://example.com/over-limit"}""")
        }
        assertEquals(HttpStatusCode.TooManyRequests, rateLimitedResponse.status, "Request should be rate limited")
    }

    @Test
    fun `fetch endpoint respects rate limit`() = testApplication {
        application {
            configureSerialization()
            configureStatusPages()
            configureRouting()
        }

        val client = this.client
        val clientIp = "192.168.1.101"

        // Make successful requests up to the limit
        repeat(10) {
            val response = client.get("/$it") {
                header("X-Forwarded-For", clientIp)
            }
            // Will mostly be NotFound, but shouldn't be TooManyRequests yet
            val status = response.status
            assertEquals(false, status == HttpStatusCode.TooManyRequests, "Request $it should not be rate limited yet")
        }

        // Next request should be rate limited
        val rateLimitedResponse = client.get("/over-limit") {
            header("X-Forwarded-For", clientIp)
        }
        assertEquals(HttpStatusCode.TooManyRequests, rateLimitedResponse.status, "Request should be rate limited")
    }

    @Test
    fun `different clients have separate rate limits`() = testApplication {
        application {
            configureSerialization()
            configureStatusPages()
            configureRouting()
        }

        val client = this.client

        // Client 1 makes requests
        repeat(5) {
            val response = client.post("/shorten") {
                header("X-Forwarded-For", "192.168.1.200")
                contentType(ContentType.Application.Json)
                setBody("""{"url":"https://example.com/client1/$it"}""")
            }
            assertEquals(HttpStatusCode.OK, response.status)
        }

        // Client 2 should be able to make requests too
        repeat(5) {
            val response = client.post("/shorten") {
                header("X-Forwarded-For", "192.168.1.201")
                contentType(ContentType.Application.Json)
                setBody("""{"url":"https://example.com/client2/$it"}""")
            }
            assertEquals(HttpStatusCode.OK, response.status)
        }
    }
}

