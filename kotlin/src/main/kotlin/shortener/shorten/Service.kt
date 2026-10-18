package com.jurisfutura.shortener.shorten

import arrow.core.Either
import arrow.core.flatMap
import arrow.core.left
import arrow.core.right

class ShortenService(private val repository: ShortenRepository) {
    suspend fun getOriginalUrl(shortCode: String): Either<FetchError, String> =
        repository.get(shortCode)?.right() ?:
            FetchError.NotFound("Code not found $shortCode").left()

    suspend fun shortenUrl(request: ShortenRequest): Either<ShortenError, ShortenResponse> =
        request.validate().flatMap { createShortCode(it) }

    private suspend fun createShortCode(request: ShortenRequest): Either<ShortenError, ShortenResponse> {
        val shortCode = request.alias ?: generateRandomShortCode()

        if (repository.exists(shortCode)) {
            return ShortenError.AliasTaken("Alias '$shortCode' is already taken").left()
        }

        repository.save(shortCode, request.url, request.ttlSeconds?.toLong())

        return ShortenResponse(
            code = shortCode,
            shortUrl = "http://0.0.0.0/$shortCode",
            url = request.url,
            expiresAt = request.ttlSeconds?.let { System.currentTimeMillis() + it * 1000 }
        ).right()
    }

    private fun generateRandomShortCode(): String {
        val chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
        return (1..8)
            .map { chars.random() }
            .joinToString("")
    }
}