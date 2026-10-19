package com.jurisfutura.shortener

import com.jurisfutura.shortener.shorten.*
import io.ktor.server.application.*
import io.ktor.server.response.*
import io.ktor.server.routing.*
import io.ktor.server.plugins.forwardedheaders.XForwardedHeaders


class ProdShortenDependencies(private val app: Application) : ShortenDependencies {
    // Lazy initialization is used here to defer the creation of the
    // ShortenService instance until it is actually needed.
    // If the service is accessed from multiple threads, care
    // must be taken to ensure thread safety during initialization.
    override val repository: ShortenRepository by lazy { ShortenRepositoryImpl() }
    override val rateLimiter by lazy {
        val cfg = RateLimiterCfg.fromConfig(app.environment.config.toMap())
        RateLimiter(cfg)
    }
    override val service by lazy { ShortenService(repository, rateLimiter) }
}

fun Application.configureRouting() {
    install(XForwardedHeaders)
    val deps = ProdShortenDependencies(this)
    routing {
        get("/") {
            call.respondText("URL Shortener Service is running!")
        }
        get("/health") {
            call.respondText("OK")
        }

        shortenRouting(deps.service)
    }
}
