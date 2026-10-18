package com.jurisfutura.shortener.shorten

interface ShortenDependencies {
    val service: ShortenService
    val repository: ShortenRepository
}