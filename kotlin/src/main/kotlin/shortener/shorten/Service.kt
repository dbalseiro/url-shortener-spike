package com.jurisfutura.shortener.shorten

import arrow.core.Either
import arrow.core.flatMap

class ShortenService {
    suspend fun shortenUrl(request: ShortenRequest): Either<ShortenError, ShortenResponse> =
        request.validate().flatMap { createShortCode(it) }

    private suspend fun createShortCode(request: ShortenRequest): Either<ShortenError, ShortenResponse> {
        val shortCode = request.alias ?: generateRandomShortCode()
        return Either.Right(ShortenResponse(
            code = shortCode,
            shortUrl = "http://0.0.0.0/$shortCode",
            url = request.url,
            expiresAt = request.ttlSeconds?.let { System.currentTimeMillis() + it * 1000 }
        ))
    }

    private fun generateRandomShortCode(): String {
        val chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
        return (1..8)
            .map { chars.random() }
            .joinToString("")
    }
}