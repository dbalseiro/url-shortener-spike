package com.jurisfutura.shortener.redis

import io.ktor.server.application.*

// This took me some time to figure out, but these are basically middleware hooks that run when the application starts and stops.
fun Application.configureRedis() {
    monitor.subscribe(ApplicationStarted) { application ->
        RedisConnection.getInstance()
        application.log.info("Redis connected")
    }

    monitor.subscribe(ApplicationStopped) { application ->
        RedisConnection.shutdown()
        application.log.info("Redis closed")
    }
}