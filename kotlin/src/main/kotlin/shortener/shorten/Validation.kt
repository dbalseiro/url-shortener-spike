package com.jurisfutura.shortener.shorten

import arrow.core.Either
import arrow.core.raise.context.ensure
import arrow.core.raise.either

private fun validUrl(url: String): Either<Violation.InvalidUrl, Unit> =
    either {
        try {
            java.net.URI(url).toURL()
        } catch (e: Exception) {
            raise(Violation.InvalidUrl("Invalid URL: ${e.message}"))
        }
        ensure(url.startsWith("http://") || url.startsWith("https://")) {
            Violation.InvalidUrl("URL must start with http:// or https://")
        }
    }

private fun validAlias(alias: String?): Either<Violation, Unit> = either {
    alias?.let {
        ensure(it.matches(Regex("^[0-9A-Za-z_-]{3,32}$"))) {
            Violation.InvalidAlias("Alias must be 3-32 chars, alphanumeric/dash/underscore")
        }
    }
}
// above is basically equivalent to:
//    return if (alias == null) {
//        Either.Right(Unit)
//    }
//    else if (alias.matches(Regex("^[0-9A-Za-z_-]{3,32}$"))) {
//        Either.Right(Unit)
//    } else {
//        Either.Left(Violation.InvalidAlias("Alias must be 3-32 chars, alphanumeric/dash/underscore"))
//    }


private fun validTtl(ttl: Int?): Either<Violation, Unit> = either {
    ttl?.let {
        ensure(it >= 1) {
            Violation.InvalidTtl("TTL must be >= 1 second")
        }
        ensure(it <= 60 * 60 * 24 * 30) {
            Violation.InvalidTtl("TTL must be <= 30 days")
        }
    }
}

fun ShortenRequest.validate(): Either<InvalidInput, ShortenRequest> {
    val violations = listOf(
        validUrl(url),
        validAlias(alias),
        validTtl(ttlSeconds),
    ).fold(listOf<Violation>()) { acc, e ->
        e.fold({ acc + it }, { acc })
    }
    return either {
        ensure(violations.isEmpty()) {
            InvalidInput("Invalid input", violations)
        }
        // it's another "this" because we are in a different scope, the "this" of
        // the either block is not the same as the "this" of the validate function
        this@validate
    }
}