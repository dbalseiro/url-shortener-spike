package com.jurisfutura

import com.jurisfutura.shortener.shorten.ShortenRequest
import io.ktor.client.HttpClient
import io.ktor.client.request.*
import io.ktor.http.HttpStatusCode
import io.ktor.server.testing.*
import io.ktor.client.plugins.contentnegotiation.*
import io.ktor.serialization.kotlinx.json.*
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
}

class ShortenTest {
    @Test
    fun `POST to shorten returns 201 Created`() = runApp {
        val response = jsonClient().post("/shorten") {
            setBody(ShortenRequest(url = "https://example.com"))
            header("Content-Type", "application/json")
        }
        assertEquals(HttpStatusCode.Created, response.status)
    }
}