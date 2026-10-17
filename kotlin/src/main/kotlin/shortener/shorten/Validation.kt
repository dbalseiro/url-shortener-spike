package com.jurisfutura.shortener.shorten

import arrow.core.Either
import arrow.core.flatMap
import arrow.core.raise.either

private fun validUrl(url: String): Either<Violation.InvalidUrl, Unit> =
    either {
        try {
            java.net.URI(url).toURL()
        } catch (e: Exception) {
            raise(Violation.InvalidUrl("Invalid URL: ${e.message}"))
        }
        url
    }.flatMap {
        if (it.startsWith("http://") || it.startsWith("https://")) {
            Either.Right(Unit)
        } else {
            Either.Left(Violation.InvalidUrl("URL must start with http:// or https://"))
        }
    }

private fun validAlias(alias: String?): Either<Violation, Unit> {
    return if (alias == null) {
        Either.Right(Unit)
    }
    else if (alias.matches(Regex("^[0-9A-Za-z_-]{3,32}$"))) {
        Either.Right(Unit)
    } else {
        Either.Left(Violation.InvalidAlias("Alias must be 3-32 chars, alphanumeric/dash/underscore"))
    }
}

private fun validTtl(ttl: Int?): Either<Violation, Unit> {
    return if (ttl == null) {
        Either.Right(Unit)
    }
    else if (ttl >= 1) {
        Either.Right(Unit)
    } else {
        Either.Left(Violation.InvalidTtl("TTL must be >= 1 second"))
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
    return if (violations.isEmpty()) {
        Either.Right(this)
    } else {
        Either.Left(InvalidInput("Invalid input", violations))
    }
}