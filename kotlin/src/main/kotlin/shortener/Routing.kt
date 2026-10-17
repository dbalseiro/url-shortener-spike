package com.jurisfutura.shortener

import com.jurisfutura.shortener.shorten.shortenRouting
import io.ktor.server.application.*
import io.ktor.server.response.*
import io.ktor.server.routing.*

fun Application.configureRouting() {
    routing {
        get("/") {
            call.respondText("URL Shortener Service is running!")
        }
        get("/health") {
            call.respondText("OK")
        }

        shortenRouting()
    }
}