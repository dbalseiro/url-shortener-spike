import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Rate } from 'k6/metrics';

const baseUrl = (__ENV.BASE_URL || 'http://localhost:8080').replace(/\/$/, '');
const scenario = __ENV.SCENARIO || 'all';
const capacity = Number(__ENV.CAPACITY || 10);
const refill = Number(__ENV.REFILL_PER_SECOND || 1);
const loadRate = Number(__ENV.REQUESTS_PER_SECOND || 5);
const duration = Number(__ENV.LOAD_SECONDS || 30);
const scenarios = ['all', 'baseline', 'burst', 'recovery', 'sustained'];

if (!scenarios.includes(scenario)) {
    throw new Error(`SCENARIO must be one of: ${scenarios.join(', ')}`);
}
if (![capacity, refill, loadRate, duration].every(value => Number.isFinite(value) && value > 0)
    || !Number.isInteger(capacity) || !Number.isInteger(loadRate * duration)
    || loadRate <= refill) {
    throw new Error('Use positive settings, integer CAPACITY and total requests, and load rate above refill rate');
}

const allowed = new Counter('allowed_requests');
const limited = new Counter('limited_requests');
const unexpected = new Rate('unexpected_responses');

export const options = {
    batch: capacity * 2,
    batchPerHost: capacity * 2,
    setupTimeout: `${Math.ceil(capacity / refill + 10)}s`,
    scenarios: {
        black_box: {
            executor: 'shared-iterations',
            vus: 1,
            iterations: 1,
            maxDuration: `${Math.ceil(4 * (capacity / refill + 1) + duration + 120)}s`,
        },
    },
    thresholds: {
        checks: ['rate==1'],
        unexpected_responses: ['rate==0'],
    },
};

http.setResponseCallback(http.expectedStatuses(201, 429));

function requestParams(phase) {
    return {
        headers: { 'Content-Type': 'application/json' },
        redirects: 0,
        timeout: '5s',
        tags: { phase, name: 'POST /shorten' },
    };
}

const body = JSON.stringify({ url: 'https://example.com/k6-rate-limit', ttlSeconds: 300 });

function record(response, phase) {
    const tags = { phase };
    unexpected.add(response.status !== 201 && response.status !== 429, tags);
    if (response.status === 201) allowed.add(1, tags);
    if (response.status === 429) {
        limited.add(1, tags);
        let errorBody;
        try { errorBody = response.json(); } catch (_) { errorBody = null; }
        check(response, {
            '429 has positive integer Retry-After': result => /^[1-9]\d*$/.test(result.headers['Retry-After'] || ''),
            '429 has contract JSON': result =>
                /application\/json/i.test(result.headers['Content-Type'] || '')
                && errorBody !== null && errorBody.error === 'RateLimited'
                && typeof errorBody.message === 'string' && errorBody.message.length > 0,
        }, tags);
    }
    if (response.status !== 201 && response.status !== 429) {
        console.error(`${phase}: unexpected status ${response.status}: ${String(response.body).slice(0, 250)}`);
    }
    return response;
}

function send(phase) {
    return record(http.post(`${baseUrl}/shorten`, body, requestParams(phase)), phase);
}

function burst(phase) {
    const started = Date.now();
    const responses = http.batch(Array.from({ length: capacity * 2 }, () => ({
        method: 'POST', url: `${baseUrl}/shorten`, body, params: requestParams(phase),
    })));
    responses.forEach(response => record(response, phase));
    const successes = responses.filter(response => response.status === 201).length;
    const rejections = responses.filter(response => response.status === 429).length;
    const maxAllowed = capacity + Math.floor((Date.now() - started) / 1000 * refill);
    console.log(`${phase}: ${successes} allowed, ${rejections} limited`);
    check(responses, {
        'burst allows initial capacity, plus at most elapsed refill': () => successes >= capacity && successes <= maxAllowed,
        'burst rejects excess traffic': () => rejections > 0,
    }, { phase });
}

function idle() {
    sleep(capacity / refill + 1);
}

export function setup() {
    console.log(`F# Compose target: ${baseUrl}; scenario=${scenario}; capacity=${capacity}; refill=${refill}/s`);
    idle();
}

export default function () {
    const phases = scenario === 'all' ? scenarios.slice(1) : [scenario];
    phases.forEach((phase, index) => {
        if (index > 0) idle();
        if (phase === 'baseline') {
            check(send(phase), { 'idle bucket allows a request': response => response.status === 201 }, { phase });
        } else if (phase === 'burst') {
            burst(phase);
        } else if (phase === 'recovery') {
            burst(phase);
            check(send(phase), { 'exhausted bucket rejects immediately': response => response.status === 429 }, { phase });
            sleep(1 / refill + 0.2);
            check(send(phase), { 'bucket allows again after one-token refill': response => response.status === 201 }, { phase });
        } else {
            const started = Date.now();
            let successes = 0;
            let rejections = 0;
            for (let request = 0; request < loadRate * duration; request++) {
                const wait = started / 1000 + request / loadRate - Date.now() / 1000;
                if (wait > 0) sleep(wait);
                const response = send(phase);
                if (response.status === 201) successes++;
                if (response.status === 429) rejections++;
            }
            const elapsed = (Date.now() - started) / 1000;
            const expected = capacity + elapsed * refill;
            console.log(`sustained: ${successes} allowed, ${rejections} limited in ${elapsed.toFixed(1)}s`);
            check(null, {
                'sustained load stays near token budget': () => successes >= Math.floor(expected) - 2 && successes <= Math.ceil(expected) + 1,
                'sustained overload produces rejections': () => rejections > 0,
                'target load duration maintained': () => elapsed <= duration + 2,
            }, { phase });
        }
    });
}