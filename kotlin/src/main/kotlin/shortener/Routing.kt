package com.jurisfutura.shortener

import com.jurisfutura.shortener.shorten.ShortenDependencies
import com.jurisfutura.shortener.shorten.ShortenService
import com.jurisfutura.shortener.shorten.shortenRouting
import io.ktor.server.application.*
import io.ktor.server.response.*
import io.ktor.server.routing.*


object ProdShortenDependencies : ShortenDependencies {
    // Lazy initialization is used here to defer the creation of the
    // ShortenService instance until it is actually needed.
    // If the service is accessed from multiple threads, care
    // must be taken to ensure thread safety during initialization.
    override val service by lazy { ShortenService() }
}

fun Application.configureRouting() {
    routing {
        get("/") {
            call.respondText("URL Shortener Service is running!")
        }
        get("/health") {
            call.respondText("OK")
        }

        shortenRouting(ProdShortenDependencies.service)
    }
}