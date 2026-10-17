package com.jurisfutura.shortener.trivialdata

import io.ktor.server.routing.*
import io.ktor.server.response.*

fun Route.trivialDataRouting() {
    get() {
        call.respondText("Hello, Trivial Data!")
    }

    post() {
        call.respondText("Received a POST request to /trivialdata")
    }
}