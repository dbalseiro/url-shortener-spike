package com.jurisfutura.shortener.shorten

import com.jurisfutura.shortener.RateLimiter
import io.ktor.http.HttpStatusCode
import io.ktor.server.plugins.origin
import io.ktor.server.request.receive
import io.ktor.server.response.respond
import io.ktor.server.response.respondRedirect
import io.ktor.server.response.respondText
import io.ktor.server.routing.Route
import io.ktor.server.routing.get
import io.ktor.server.routing.post
import io.ktor.server.routing.route


/**
 * Extracts the client IP address from the request.
 * Handles X-Forwarded-For header for proxied requests.
 */
private fun getClientIp(headers: io.ktor.http.Headers): String {
    // Check X-Forwarded-For header first (for proxies)
    val forwarded = headers["X-Forwarded-For"]?.split(",")?.firstOrNull()?.trim()
    if (!forwarded.isNullOrEmpty()) {
        return forwarded
    }
    // Fallback to direct connection IP
    return "unknown"
}

fun Route.shortenRouting(service: ShortenService, rateLimiter: RateLimiter) {
    route("/{code}") {
        get {
            val shortCode = call.parameters["code"] ?: return@get call.respondText(
                "Missing code",
                status = HttpStatusCode.BadRequest
            )
            service.getOriginalUrl(shortCode, call.request.origin.remoteHost).fold(
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
            val response = service.shortenUrl(request)
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