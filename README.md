# 🚀 Hybrid Throttling Demo: Adaptive Rate Limiting with Akka.NET Streams

A demonstration of a **hybrid throttling strategy** for resilient HTTP data processing, combining a fixed throughput limit with adaptive backoff that reacts to downstream failures. The implementation uses **Akka.NET Streams** in **.NET 10** as the streaming engine.

The application uses historical Bitcoin (BTC) price requests to demonstrate how hybrid throttling behaves under normal load, transient `429 Too Many Requests` responses, retries, and downstream recovery. The focus is the **throttling and resilience model**, not the Bitcoin API itself.

- backpressure-aware ingestion,

- hybrid throttling,

- retry with adaptive backoff,

- consecutive-failure circuit breaking,

- graceful stream shutdown,

- and downstream recovery observability.

---

# 🌟 Key Features & Architectural Highlights

## Core Concept: Hybrid Throttling

The application combines two complementary throttling layers:

- **Akka `Throttle`** — hard rate limit (**1 request/second**).

- **`AdaptiveThrottle`** — soft throttling activated only during downstream degradation.

The fixed Akka throttle controls the normal request rate, while the adaptive throttle dynamically increases the delay only when the downstream service starts failing. This separates baseline rate limiting from failure-driven backoff.

## Adaptive Backoff Strategy

Failure sequence:

```text
0s → 10s → 20s → 40s → 60s
```

Each failed HTTP request increases the adaptive delay before the next request.

Recovery sequence:

```text
60s → 0s
40s → 0s
20s → 0s
10s → 0s
```

A successful HTTP request completely resets the adaptive throttle to `0s`.
The error counter is reset to `0` at the same time.

## Consecutive Error Circuit Breaker

The stream tracks **consecutive HTTP failures**. After the configured threshold is reached, the pipeline is gracefully stopped through `SharedKillSwitch`.

The counter is **reset immediately after the first successful response**, so recovery starts a new error series.

## Graceful Stream Termination

`SharedKillSwitch.Shutdown()` completes the stream without faulting the materialized task, allowing clean shutdown and deterministic logging.

## Backpressure Management

Input is pushed through `Source.Queue` with `OverflowStrategy.Backpressure`, so upstream producers automatically slow down when downstream processing becomes slower.

## Downstream Recovery Observability

When a successful response resets the adaptive throttle to `0s`, the application emits a recovery event and logs how long the downstream service remained degraded.

The runtime metrics endpoint exposes the current adaptive throttle delay through `ThrottleDelayMs`. This represents the actual delay configured by `AdaptiveThrottle` and shows the `0 / 10 / 20 / 40 / 60` second backoff states in Grafana.

---

# 🏗️ Stream Architecture Flow

```text
[ Source.Queue ]
        │
        ▼
[ Akka Throttle (1 req/sec) ]
        │
        ▼
[ SelectAsync(1) ]
        │
        ▼
[ AdaptiveThrottle ]
        │
        ▼
[ HTTP Request ]
        │
        ▼
[ SharedKillSwitch.Flow ]
        │
        ▼
[ Sink.ForEach ]
```

The pipeline is intentionally **linear**. `SelectAsync(1)` guarantees that only one HTTP request is in flight at any moment, which greatly simplifies retry and throttling logic.

---

# 🔄 Request Processing Lifecycle

## Normal operation

```text
Throttle 1s → Request → Success
```

## Transient failure

```text
Request → 429 → AdaptiveThrottle=10s → Retry
```

## Persistent failure

```text
429 → 10s
429 → 20s
429 → 40s
429 → 60s
429 → KillSwitch
```

## Recovery

```text
Success → Error counter reset to 0
Success → AdaptiveThrottle reset to 0s
```

---

# 📋 Example Logs

## Successful requests

```text
[19:05:29] [INFO] 2026-07-15: BTC = 64983.519074 USD
[19:05:32] [INFO] 2026-07-16: BTC = 64717.168891 USD
```

## Retry after throttling

```text
[19:05:33] [THROTTLE] Increased to 10s
[19:05:33] [ERROR] 2026-07-18: 429 Too Many Requests. Consecutive error #1/4
[19:05:43] [THROTTLE] Waiting 10s
[19:05:43] [RETRY] 2026-07-18, attempt 2
```

## Downstream recovery

```text
[19:05:33] [THROTTLE] Disabled
[19:05:33] [INFO] Downstream recovered after 42s
```

## Graceful shutdown

```text
[19:05:44] [KILLSWITCH] Error limit reached. Shutting down stream...
[WARN] Processing stopped due to too many consecutive HTTP errors.
```

---

# ⚙️ Current Runtime Configuration

| Setting                 | Value            |
| ----------------------- | ---------------- |
| Hard throttle           | 1 request/second |
| Adaptive throttle max   | 60 seconds       |
| Max consecutive errors  | 4                |
| Max retries per request | 4                |
| Days fetched            | 30               |
| Queue capacity          | 100              |
| Parallelism             | 1                |

---

# 🧠 Why This Design?

The implementation intentionally separates the responsibilities of fixed rate limiting, adaptive backoff, retry handling, circuit breaking, and stream flow control:

| Concern             | Component                 |
| ------------------- | ------------------------- |
| Hard API protection | Akka `Throttle`           |
| Dynamic slowdown    | `AdaptiveThrottle`        |
| Retry policy        | `SelectAsync` worker      |
| Circuit breaking    | Consecutive error counter |
| Graceful shutdown   | `SharedKillSwitch`        |
| Flow control        | Akka backpressure         |

This mirrors real-world streaming systems where throughput control, resilience, and failure handling are independent concerns.

---

# 🧪 What This Repository Demonstrates

- Hybrid throttling: fixed rate limiting + adaptive backoff
- Adaptive backoff and complete recovery reset
- Real runtime throttle state exposed through `ThrottleDelayMs`
- Akka Streams backpressure semantics

- `Source.Queue` ingestion patterns

- Sequential async processing with `SelectAsync(1)`

- Hybrid throttling design

- Exponential backoff

- Consecutive-failure circuit breaker

- Graceful shutdown with `SharedKillSwitch`

- Recovery observability and telemetry-friendly logging
- Runtime exposure of adaptive throttle state through `ThrottleDelayMs`

---

# ▶️ Running

```bash
dotnet restore
dotnet run
```

The console output will show BTC prices, retry attempts, adaptive throttling transitions, and graceful shutdown behavior when the error threshold is exceeded.

---

# 📚 Useful Akka.NET Concepts Demonstrated

- `ActorSystem`

- `IMaterializer`

- `Source.Queue`

- `OverflowStrategy.Backpressure`

- `Throttle`

- `SelectAsync`

- `Sink.ForEach`

- `SharedKillSwitch`

- Backpressure propagation

- Stream materialization

- Graceful completion semantics
