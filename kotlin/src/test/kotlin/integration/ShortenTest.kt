package com.jurisfutura

import com.jurisfutura.shortener.shorten.ShortenRequest
import com.jurisfutura.shortener.shorten.ShortenResponse
import io.ktor.client.HttpClient
import io.ktor.client.request.*
import io.ktor.http.HttpStatusCode
import io.ktor.server.testing.*
import io.ktor.client.plugins.contentnegotiation.*
import io.ktor.serialization.kotlinx.json.*
import io.ktor.client.call.*
import kotlin.test.*

// these two function belong in a test utils or something?
fun runApp(block: suspend ApplicationTestBuilder.() -> Unit) = testApplication {
    configure()
    block()
}

fun ApplicationTestBuilder.jsonClient(): HttpClient = createClient {
    install(ContentNegotiation) {
        json()
    }
    followRedirects = false
}

class ShortenTest {
    @BeforeTest
    fun setup() {
        RedisContainer.start()
    }

    @AfterTest
    fun teardown() {
        RedisContainer.stop()
    }

    @Test
    fun `POST to shorten returns 201 Created`() = runApp {
        val response = jsonClient().post("/shorten") {
            setBody(ShortenRequest(url = "https://example.com"))
            header("Content-Type", "application/json")
        }
        assertEquals(HttpStatusCode.Created, response.status)
        val body = response.body<ShortenResponse>()
        assertNotNull(body.code)
        assertNotNull(body.shortUrl)
        assertEquals("https://example.com", body.url)
    }

    @Test
    fun `POST to shorten with custom alias returns 201 Created`() = runApp {
        val response = jsonClient().post("/shorten") {
            setBody(ShortenRequest(url = "https://example.com", alias = "myalias"))
            header("Content-Type", "application/json")
        }
        assertEquals(HttpStatusCode.Created, response.status)
        val body = response.body<ShortenResponse>()
        assertEquals("myalias", body.code)
        assertEquals("https://example.com", body.url)
    }

    // @Test
    // fun `GET shortened URL redirects to original`() = runApp {
    //     val shortenResponse = jsonClient().post("/shorten") {
    //         setBody(ShortenRequest(url = "https://example.com", alias = "redirect-test"))
    //         header("Content-Type", "application/json")
    //     }
    //     val code = shortenResponse.body<ShortenResponse>().code

    //     val redirectResponse = jsonClient().get("/$code")
    //     assertEquals(HttpStatusCode.MovedPermanently, redirectResponse.status)
    //     assertEquals("https://example.com", redirectResponse.headers["Location"])
    // }

    // @Test
    // fun `POST to shorten with invalid URL returns 400 BadRequest`() = runApp {
    //     val response = jsonClient().post("/shorten") {
    //         setBody(ShortenRequest(url = "not-a-valid-url"))
    //         header("Content-Type", "application/json")
    //     }
    //     assertEquals(HttpStatusCode.BadRequest, response.status)
    // }

    // @Test
    // fun `POST to shorten with duplicate alias returns 409 Conflict`() = runApp {
    //     val alias = "duplicate-alias"
    //     jsonClient().post("/shorten") {
    //         setBody(ShortenRequest(url = "https://example.com", alias = alias))
    //         header("Content-Type", "application/json")
    //     }

    //     val duplicateResponse = jsonClient().post("/shorten") {
    //         setBody(ShortenRequest(url = "https://different.com", alias = alias))
    //         header("Content-Type", "application/json")
    //     }
    //     assertEquals(HttpStatusCode.Conflict, duplicateResponse.status)
    // }

    // @Test
    // fun `POST to shorten with TTL sets expiration`() = runApp {
    //     val response = jsonClient().post("/shorten") {
    //         setBody(ShortenRequest(url = "https://example.com", ttlSeconds = 3600))
    //         header("Content-Type", "application/json")
    //     }
    //     assertEquals(HttpStatusCode.Created, response.status)
    //     val body = response.body<ShortenResponse>()
    //     assertNotNull(body.expiresAt)
    // }

    // @Test
    // fun `GET non-existent shortened URL returns 404 NotFound`() = runApp {
    //     val response = jsonClient().get("/nonexistent")
    //     assertEquals(HttpStatusCode.NotFound, response.status)
    // }
}