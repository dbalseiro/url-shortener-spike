package com.jurisfutura.shortener.shorten

import arrow.core.Either
import kotlinx.serialization.Serializable

@Serializable
data class ShortenRequest(
    val url: String,
    val alias: String? = null,
    val ttlSeconds: Int? = null
)

@Serializable
data class ShortenResponse(
    val code: String,
    val shortUrl: String,
    val url: String,
    // date in milliseconds if present
    val expiresAt: Long? = null
)
