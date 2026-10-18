package com.jurisfutura.shortener.ratelimit

import java.util.concurrent.atomic.AtomicInteger
import kotlin.test.*
import kotlinx.coroutines.runBlocking
import org.testcontainers.containers.GenericContainer
import org.testcontainers.utility.DockerImageName

class RateLimiterTest {
    private lateinit var redis: GenericContainer<*>

    @BeforeTest
    fun setUp() {
        // Start Redis container
        redis = GenericContainer(DockerImageName.parse("redis:7-alpine"))
            .withExposedPorts(6379)
        redis.start()

        // Set Redis URL for the connection
        System.setProperty("REDIS_URL", "redis://localhost:${redis.getMappedPort(6379)}")
    }

    @AfterTest
    fun tearDown() {
        redis.stop()
    }

    @Test
    fun allowsRequestWhenTokensAvailable() {
        runBlocking {
            val limiter = RateLimiter(maxTokens = 10, tokensPerSecond = 5.0, windowSeconds = 60)

            val result = limiter.allowRequest("test-ip")

            assertTrue(result, "First request should be allowed")
        }
    }

    @Test
    fun deniesRequestWhenNoTokensAvailable() {
        runBlocking {
            val limiter = RateLimiter(maxTokens = 2, tokensPerSecond = 0.1, windowSeconds = 60)

            // Consume all tokens
            assertTrue(limiter.allowRequest("test-ip"))
            assertTrue(limiter.allowRequest("test-ip"))

            // This should be denied
            assertFalse(limiter.allowRequest("test-ip"))
        }
    }

    @Test
    fun refillsTokensOverTime() {
        runBlocking {
            val limiter = RateLimiter(maxTokens = 5, tokensPerSecond = 2.0, windowSeconds = 60)

            // Use first token
            assertTrue(limiter.allowRequest("test-ip-2"))

            // Immediately try to use more tokens (should have exactly 4 left)
            repeat(4) {
                assertTrue(limiter.allowRequest("test-ip-2"))
            }

            // All tokens are now used up
            assertFalse(limiter.allowRequest("test-ip-2"))

            // Wait for tokens to refill
            Thread.sleep(1500) // Wait 1.5 seconds, should get ~3 new tokens

            // Should now be able to make at least one more request
            val canMakeRequest = limiter.allowRequest("test-ip-2")
            assertTrue(canMakeRequest, "Should be able to make at least one request after refill")
        }
    }

    @Test
    fun tracksDifferentClientsIndependently() {
        runBlocking {
            val limiter = RateLimiter(maxTokens = 1, tokensPerSecond = 1.0, windowSeconds = 60)

            val ip1 = "192.168.1.1"
            val ip2 = "192.168.1.2"

            // Both clients can make one request
            assertTrue(limiter.allowRequest(ip1))
            assertTrue(limiter.allowRequest(ip2))

            // Both should be rate limited now
            assertFalse(limiter.allowRequest(ip1))
            assertFalse(limiter.allowRequest(ip2))
        }
    }

    @Test
    fun resetsRateLimitForSpecificClient() {
        runBlocking {
            val limiter = RateLimiter(maxTokens = 1, tokensPerSecond = 1.0, windowSeconds = 60)
            val ip = "test-ip-3"

            // Use the token
            assertTrue(limiter.allowRequest(ip))
            assertFalse(limiter.allowRequest(ip))

            // Reset and try again
            limiter.reset(ip)
            assertTrue(limiter.allowRequest(ip))
        }
    }

    @Test
    fun isThreadSafe() {
        runBlocking {
            val limiter = RateLimiter(maxTokens = 50, tokensPerSecond = 10.0, windowSeconds = 60)
            val ip = "thread-safe-test"
            val successCount = AtomicInteger(0)
            val failCount = AtomicInteger(0)

            // Simulate concurrent requests - 100 threads requesting, but only 50 tokens available
            val threads = (1..100).map {
                Thread {
                    runBlocking {
                        if (limiter.allowRequest(ip)) {
                            successCount.incrementAndGet()
                        } else {
                            failCount.incrementAndGet()
                        }
                    }
                }
            }

            threads.forEach { it.start() }
            threads.forEach { it.join() }

            // Verify counts: should have exactly 50 successes (maxTokens) and 50 failures
            // The total should be 100
            val total = successCount.get() + failCount.get()
            assertEquals(100, total, "Total requests should be 100")
            assertEquals(50, successCount.get(), "Should allow exactly 50 requests (maxTokens)")
            assertEquals(50, failCount.get(), "Should deny exactly 50 requests")
        }
    }
}




