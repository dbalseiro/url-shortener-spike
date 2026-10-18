package com.jurisfutura.shortener.shorten

import com.jurisfutura.shortener.ratelimit.RateLimiter
import kotlin.test.*
import kotlinx.coroutines.runBlocking


class ServiceTest {
    // lateinit allows declaring non-nullable properties without initializing them immediately.
    // In tests, we use it because we need to initialize these objects in @BeforeTest
    // (which runs before each test method) rather than at class declaration time.
    // This ensures each test gets a fresh instance of the mock and service.
    private lateinit var mockRepository: MockShortenRepository
    private lateinit var mockRateLimiter: MockRateLimiter
    private lateinit var service: ShortenService

    // @BeforeTest runs before each test method. Here we initialize our mock and service.
    // If we tried to use these properties before initialization, it would throw an exception.
    @BeforeTest
    fun setUp() {
        mockRepository = MockShortenRepository()
        mockRateLimiter = MockRateLimiter()
        service = ShortenService(mockRepository, mockRateLimiter)
    }

    @Test
    fun getOriginalUrlReturnsUrlWhenShortCodeExists() {
        runBlocking {
            val shortCode = "abc123"
            val longUrl = "https://example.com/very/long/path"
            mockRepository.setGetResult(shortCode, longUrl)

            val result = service.getOriginalUrl(shortCode, "127.0.0.1")

            result.fold(
                ifLeft = { fail("Expected right but got: $it") },
                ifRight = { assertEquals(longUrl, it) }
            )
        }
    }

    @Test
    fun getOriginalUrlReturnsNotFoundErrorWhenShortCodeDoesntExist() {
        runBlocking {
            val shortCode = "notfound"
            mockRepository.setGetResult(shortCode, null)

            val result = service.getOriginalUrl(shortCode, "127.0.0.1")

            result.fold(
                ifLeft = { assertIs<FetchError.NotFound>(it) },
                ifRight = { fail("Expected left but got: $it") }
            )
        }
    }

    @Test
    fun getOriginalUrlReturnsRateLimitedErrorWhenRateLimited() {
        runBlocking {
            val shortCode = "abc123"
            mockRepository.setGetResult(shortCode, "https://example.com")
            mockRateLimiter.setAllowRequest(false)

            val result = service.getOriginalUrl(shortCode, "127.0.0.1")

            result.fold(
                ifLeft = { assertIs<FetchError.RateLimited>(it) },
                ifRight = { fail("Expected left but got: $it") }
            )
        }
    }

    @Test
    fun shortenUrlCreatesShortCodeWithCustomAlias() {
        runBlocking {
            val request = ShortenRequest(
                url = "https://example.com",
                alias = "my-link"
            )

            val result = service.shortenUrl(request, "127.0.0.1")

            result.fold(
                ifLeft = { fail("Expected right but got: $it") },
                ifRight = {
                    assertEquals("my-link", it.code)
                    assertEquals("https://example.com", it.url)
                    assertTrue(mockRepository.wasSaveCalled())
                }
            )
        }
    }

    @Test
    fun shortenUrlGeneratesRandomCodeWhenNoAliasProvided() {
        runBlocking {
            val request = ShortenRequest(url = "https://example.com")

            val result = service.shortenUrl(request, "127.0.0.1")

            result.fold(
                ifLeft = { fail("Expected right but got: $it") },
                ifRight = {
                    assertEquals(8, it.code.length)
                    assertTrue(mockRepository.wasSaveCalled())
                }
            )
        }
    }

    @Test
    fun shortenUrlReturnsErrorWhenAliasTaken() {
        runBlocking {
            val alias = "taken"
            mockRepository.setExistsResult(alias, true)

            val request = ShortenRequest(
                url = "https://example.com",
                alias = alias
            )

            val result = service.shortenUrl(request, "127.0.0.1")

            result.fold(
                ifLeft = { assertIs<ShortenError.AliasTaken>(it) },
                ifRight = { fail("Expected left but got: $it") }
            )
            assertFalse(mockRepository.wasSaveCalled())
        }
    }

    @Test
    fun shortenUrlReturnsRateLimitedErrorWhenRateLimited() {
        runBlocking {
            mockRateLimiter.setAllowRequest(false)
            val request = ShortenRequest(url = "https://example.com")

            val result = service.shortenUrl(request, "127.0.0.1")

            result.fold(
                ifLeft = { assertIs<ShortenError.RateLimited>(it) },
                ifRight = { fail("Expected left but got: $it") }
            )
            assertFalse(mockRepository.wasSaveCalled())
        }
    }

    @Test
    fun shortenUrlSavesWithTTLWhenProvided() {
        runBlocking {
            val ttlSeconds = 3600
            val request = ShortenRequest(
                url = "https://example.com",
                alias = "with-ttl",
                ttlSeconds = ttlSeconds
            )

            val result = service.shortenUrl(request, "127.0.0.1")

            result.fold(
                ifLeft = { fail("Expected right but got: $it") },
                ifRight = {
                    assertNotNull(it.expiresAt)
                    assertTrue(it.expiresAt > System.currentTimeMillis())
                    assertTrue(mockRepository.wasSaveCalled())
                }
            )
        }
    }

    @Test
    fun shortenUrlFailsValidationOnInvalidUrl() {
        runBlocking {
            val request = ShortenRequest(url = "not-a-valid-url")

            val result = service.shortenUrl(request, "127.0.0.1")

            result.fold(
                ifLeft = { assertIs<ShortenError.InvalidInput>(it) },
                ifRight = { fail("Expected left but got: $it") }
            )
            assertFalse(mockRepository.wasSaveCalled())
        }
    }
}

// Simple mock repository without external dependencies
class MockShortenRepository : ShortenRepository() {
    private val storage = mutableMapOf<String, String?>()
    private val existsResults = mutableMapOf<String, Boolean>()
    private var saveWasCalled = false

    fun setGetResult(code: String, url: String?) {
        storage[code] = url
    }

    fun setExistsResult(code: String, exists: Boolean) {
        existsResults[code] = exists
    }

    fun wasSaveCalled(): Boolean = saveWasCalled

    override suspend fun save(shortCode: String, longUrl: String, ttlSeconds: Long?) {
        saveWasCalled = true
        storage[shortCode] = longUrl
    }

    override suspend fun get(shortCode: String): String? = storage[shortCode]

    override suspend fun exists(shortCode: String): Boolean = existsResults[shortCode] ?: false
}

// Mock rate limiter for unit tests
class MockRateLimiter : RateLimiter(100, 10.0) {
    private var allowRequestResult = true

    fun setAllowRequest(allow: Boolean) {
        allowRequestResult = allow
    }

    override suspend fun allowRequest(clientIp: String): Boolean = allowRequestResult
}

