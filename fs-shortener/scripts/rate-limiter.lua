---@diagnostic disable: undefined-global

local tokensKey = KEYS[1]
local timestampKey = KEYS[2]
local maxTokens = tonumber(ARGV[1])
local tokensPerSecond = tonumber(ARGV[2])
local now = tonumber(ARGV[3])
local windowSeconds = tonumber(ARGV[4])

local lastTimestamp = redis.call("GET", timestampKey)
local currentTokens = tonumber(redis.call("GET", tokensKey) or maxTokens)

if not lastTimestamp then
	-- first request, initialize with full bucket
	redis.call("SET", tokensKey, maxTokens - 1, "EX", windowSeconds)
	redis.call("SET", timestampKey, now, "EX", windowSeconds)
	return 1 -- allow
end

lastTimestamp = tonumber(lastTimestamp)
local elapsedMs = now - lastTimestamp
local elapsedSeconds = elapsedMs / 1000

-- How many tokens we need to add based on elapsed time
local tokensToAdd = elapsedSeconds * tokensPerSecond
local newTokens = math.min(currentTokens + tokensToAdd, maxTokens)

-- try to consume one token
if newTokens < 1 then
	-- not enough tokens, keep current tokens
	redis.call("SET", tokensKey, newTokens, "EX", windowSeconds)
	return 0 -- reject
else
	-- consume one
	newTokens = newTokens - 1
	redis.call("SET", tokensKey, tostring(newTokens), "EX", windowSeconds)
	redis.call("SET", timestampKey, now, "EX", windowSeconds)
	return 1 -- allow
end
