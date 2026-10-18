package com.jurisfutura.shortener.shorten

import kotlin.test.*

class ValidationTest {

    @Test
    fun `validate should pass with valid URL and no optional fields`() {
        val request = ShortenRequest(url = "https://example.com")
        val result = request.validate()
        
        result.fold(
            { fail("Expected success but got error: ${it.message}") },
            { assertTrue(true) }
        )
    }

    @Test
    fun `validate should pass with valid URL, alias, and TTL`() {
        val request = ShortenRequest(
            url = "https://example.com/path",
            alias = "my-link",
            ttlSeconds = 3600
        )
        val result = request.validate()
        
        result.fold(
            { fail("Expected success but got error: ${it.message}") },
            { assertTrue(true) }
        )
    }

    @Test
    fun `validate should fail with invalid URL - missing protocol`() {
        val request = ShortenRequest(url = "example.com")
        val result = request.validate()
        
        result.fold(
            { error ->
                assertEquals(1, error.violations.size)
                assertIs<Violation.InvalidUrl>(error.violations[0])
            },
            { fail("Expected error but validation passed") }
        )
    }

    @Test
    fun `validate should fail with invalid URL - malformed`() {
        val request = ShortenRequest(url = "not a url")
        val result = request.validate()
        
        result.fold(
            { error ->
                assertTrue(error.violations.isNotEmpty())
            },
            { fail("Expected error but validation passed") }
        )
    }

    @Test
    fun `validate should fail with invalid alias - too short`() {
        val request = ShortenRequest(
            url = "https://example.com",
            alias = "ab"
        )
        val result = request.validate()
        
        result.fold(
            { error ->
                assertTrue(error.violations.any { it is Violation.InvalidAlias })
            },
            { fail("Expected error but validation passed") }
        )
    }

    @Test
    fun `validate should fail with invalid alias - invalid characters`() {
        val request = ShortenRequest(
            url = "https://example.com",
            alias = "my link!"
        )
        val result = request.validate()
        
        result.fold(
            { error ->
                assertTrue(error.violations.any { it is Violation.InvalidAlias })
            },
            { fail("Expected error but validation passed") }
        )
    }

    @Test
    fun `validate should pass with valid alias patterns`() {
        listOf("abc", "my-link", "my_link", "ABC123", "a-b_c").forEach { alias ->
            val request = ShortenRequest(url = "https://example.com", alias = alias)
            request.validate().fold(
                { fail("Alias '$alias' should be valid but got error: ${it.message}") },
                { assertTrue(true) }
            )
        }
    }

    @Test
    fun `validate should fail with invalid TTL - negative`() {
        val request = ShortenRequest(
            url = "https://example.com",
            ttlSeconds = -1
        )
        val result = request.validate()
        
        result.fold(
            { error ->
                assertTrue(error.violations.any { it is Violation.InvalidTtl })
            },
            { fail("Expected error but validation passed") }
        )
    }

    @Test
    fun `validate should fail with invalid TTL - zero`() {
        val request = ShortenRequest(
            url = "https://example.com",
            ttlSeconds = 0
        )
        val result = request.validate()
        
        result.fold(
            { error ->
                assertTrue(error.violations.isNotEmpty())
            },
            { fail("Expected error but validation passed") }
        )
    }

    @Test
    fun `validate should pass with valid TTL`() {
        val request = ShortenRequest(
            url = "https://example.com",
            ttlSeconds = 1
        )
        val result = request.validate()
        
        result.fold(
            { fail("Expected success but got error: ${it.message}") },
            { assertTrue(true) }
        )
    }

    @Test
    fun `validate should fail with multiple violations`() {
        val request = ShortenRequest(
            url = "not-a-url",
            alias = "x",
            ttlSeconds = -5
        )
        val result = request.validate()
        
        result.fold(
            { error ->
                assertEquals(3, error.violations.size)
            },
            { fail("Expected error but validation passed") }
        )
    }
}
