package com.jurisfutura.shortener.shorten

import com.jurisfutura.shortener.RateLimiter
import io.ktor.http.HttpStatusCode
import io.ktor.server.plugins.origin
import io.ktor.server.request.receive
import io.ktor.server.response.*
import io.ktor.server.routing.*


fun Route.shortenRouting(service: ShortenService) {
    route("/{code}") {
        get {
            val shortCode = call.parameters["code"] ?: return@get call.respondText(
                "Missing code",
                status = HttpStatusCode.BadRequest
            )
            val clientIp = call.request.origin.remoteHost
            service.getOriginalUrl(shortCode, clientIp).fold(
                ifRight = {
                    call.respondRedirect(url = it, permanent = true)
                },
                ifLeft = {
                    val status = when (it) {
                        is FetchError.NotFound -> HttpStatusCode.NotFound
                        is FetchError.RateLimited -> HttpStatusCode.TooManyRequests
                    }
                    call.respond(status, message = it)
                }
            )
        }
    }
    route("/shorten") {
        post {
            val request = call.receive<ShortenRequest>()
            val clientIp = call.request.origin.remoteHost
            val response = service.shortenUrl(request, clientIp)
            response.fold(
                ifRight = {
                    call.respond(status = HttpStatusCode.OK, it)
                },
                ifLeft = {
                    // this is exhaustive :tada:
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