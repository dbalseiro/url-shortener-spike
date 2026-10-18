package com.jurisfutura.shortener.shorten

import arrow.core.Either
import arrow.core.flatMap
import arrow.core.left
import arrow.core.right
import com.jurisfutura.shortener.ratelimit.RateLimiter

class ShortenService(
    private val repository: ShortenRepository,
    private val rateLimiter: RateLimiter
) {
    suspend fun getOriginalUrl(shortCode: String, clientIp: String): Either<FetchError, String> {
        // Check rate limit
        if (!rateLimiter.allowRequest(clientIp)) {
            return FetchError.RateLimited("Rate limit exceeded").left()
        }

        return repository.get(shortCode)?.right() ?:
            FetchError.NotFound("Code not found $shortCode").left()
    }

    suspend fun shortenUrl(request: ShortenRequest, clientIp: String): Either<ShortenError, ShortenResponse> {
        // Check rate limit
        if (!rateLimiter.allowRequest(clientIp)) {
            return ShortenError.RateLimited("Rate limit exceeded").left()
        }

        return request.validate().flatMap { createShortCode(it) }
    }

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