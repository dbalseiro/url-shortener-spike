package com.jurisfutura.shortener

import com.jurisfutura.shortener.trivialdata.trivialDataRouting
import io.ktor.server.application.*
import io.ktor.server.response.*
import io.ktor.server.routing.*

fun Application.configureRouting() {
    routing {
        get("/") {
            call.respondText("Hello, World!")
        }
        get("/health") {
            call.respondText("OK")
        }

        route("/trivialdata") {
            trivialDataRouting()
        }
    }
}