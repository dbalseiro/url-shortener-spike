package com.jurisfutura.shortener.shorten

import com.jurisfutura.shortener.RateLimiter

interface ShortenDependencies {
    val service: ShortenService
    val repository: ShortenRepository
    val rateLimiter: RateLimiter
}