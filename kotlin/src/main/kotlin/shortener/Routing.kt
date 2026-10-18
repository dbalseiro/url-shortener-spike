package com.jurisfutura.shortener

import com.jurisfutura.shortener.ratelimit.RateLimiter
import com.jurisfutura.shortener.shorten.ShortenDependencies
import com.jurisfutura.shortener.shorten.ShortenRepository
import com.jurisfutura.shortener.shorten.ShortenService
import com.jurisfutura.shortener.shorten.shortenRouting
import io.ktor.server.application.*
import io.ktor.server.config.tryGetString
import io.ktor.server.response.*
import io.ktor.server.routing.*


class ProdShortenDependencies(application: Application) : ShortenDependencies {
    // Lazy initialization is used here to defer the creation of the
    // ShortenService instance until it is actually needed.
    // If the service is accessed from multiple threads, care
    // must be taken to ensure thread safety during initialization.
    override val repository by lazy { ShortenRepository() }

    override val rateLimiter by lazy {
        val config = application.environment.config
        val maxTokens = config.tryGetString("ratelimit.maxTokens")?.toIntOrNull() ?: 100
        val tokensPerSecond = config.tryGetString("ratelimit.tokensPerSecond")?.toDoubleOrNull() ?: 10.0
        val windowSeconds = config.tryGetString("ratelimit.windowSeconds")?.toLongOrNull() ?: 60
        RateLimiter(maxTokens, tokensPerSecond, windowSeconds)
    }

    override val service by lazy { ShortenService(repository, rateLimiter) }
}

fun Application.configureRouting() {
    val dependencies = ProdShortenDependencies(this)

    routing {
        get("/") {
            call.respondText("URL Shortener Service is running!")
        }
        get("/health") {
            call.respondText("OK")
        }

        shortenRouting(dependencies.service)
    }
}