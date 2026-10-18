package com.jurisfutura.shortener.shorten

import io.ktor.http.HttpStatusCode
import io.ktor.server.request.receive
import io.ktor.server.response.respond
import io.ktor.server.routing.Route
import io.ktor.server.routing.post
import io.ktor.server.routing.route

fun Route.shortenRouting(service: ShortenService) {
    route("/shorten") {
        post {
            val request = call.receive<ShortenRequest>()
            val response = service.shortenUrl(request)
            response.fold(
                ifRight = {
                    call.respond(status = HttpStatusCode.OK, it)
                },
                ifLeft = {
                    val status = when (it) {
                        is ShortenError.InvalidInput -> HttpStatusCode.BadRequest
                        is ShortenError.AliasTaken -> HttpStatusCode.Conflict
                        is ShortenError.RateLimited -> HttpStatusCode.TooManyRequests
                    }
                    call.respond(status, message = it)
                }
            )
        }
    }
}