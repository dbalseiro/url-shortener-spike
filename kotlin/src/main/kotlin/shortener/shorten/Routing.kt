package com.jurisfutura.shortener.shorten

import io.ktor.http.HttpStatusCode
import io.ktor.server.request.receive
import io.ktor.server.response.respond
import io.ktor.server.routing.Route
import io.ktor.server.routing.post
import io.ktor.server.routing.route

fun Route.shortenRouting() {
    route("/shorten") {
        post {
            val request = call.receive<ShortenRequest>()
            val result = request.validate()
            result.fold(
                ifRight = {
                    call.respond(HttpStatusCode.Created)
                },
                ifLeft = {
                    call.respond(HttpStatusCode.BadRequest, it)
                }
            )
        }
    }
}