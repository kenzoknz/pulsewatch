# Implementation Plan: P0 Security Hardening for Monitored URLs

## Feature

Prevent user-controlled monitoring URLs and Playwright deep checks from being
used to reach local, private, reserved, metadata, or otherwise unsafe network
targets.

## Status

Planning only. This document does not modify application code.

## Goal

Make every outbound monitoring request pass one shared, tested URL-safety policy
before a connection is made. Apply the same protection to:

- Website create and update validation
- Bulk website creation
- Standard HTTP checks
- Manual, bulk, and scheduled checks
- Playwright deep checks
- Redirect destinations
- User-supplied custom request headers

## Current Risk and Code Anchors

The current code has several direct outbound-request paths:

- `PulseWatch.Api/DTOs/CreateWebsiteDto.cs` accepts `Url` without URL policy
  validation.
- `PulseWatch.Api/DTOs/UpdateWebsiteDto.cs` accepts `Url` without URL policy
  validation.
- `PulseWatch.Api/DTOs/BulkCreateWebsitesDto.cs` accepts a list of raw URLs.
- `PulseWatch.Api/Controllers/WebsitesController.cs` creates websites from the
  DTOs and calls standard/deep checks.
- `PulseWatch.Api/Services/UptimeCheckerService.cs` sends requests using the
  stored URL and currently allows the default `HttpClient` redirect behavior.
- `PulseWatch.Api/Services/DeepCheckServices.cs` passes the stored URL directly
  to Playwright `GotoAsync`.
- `PulseWatch.Api/Program.cs` configures the named typed `HttpClient`, but does
  not currently configure redirect or outbound-network restrictions.

The `CustomHeadersJson` field also permits user-provided request headers. Invalid
JSON is currently ignored by the checker, and arbitrary header names are added
with `TryAddWithoutValidation`; this needs an explicit security policy.

## Security Decisions

### Allowed URL shape

- Allow only absolute `http` and `https` URLs.
- Require a non-empty DNS hostname or a valid public IP literal.
- Reject credentials in the URL (`user:password@host`).
- Reject control characters, whitespace in the URI, and unsupported ports.
- Reject fragments because fragments are not sent to the server and can create
  confusing equivalence between configured and requested URLs.
- Normalize scheme and hostname to lowercase, preserve the meaningful path and
  query, and use the normalized value for comparison/deduplication.
- Set an explicit maximum URL length.

The exact allowed port policy must be configurable. The default should allow
standard HTTP/HTTPS ports and reject unusual ports unless explicitly enabled for
the deployment.

### Blocked destinations

Block IP addresses in loopback, link-local, private, multicast, unspecified,
documentation, benchmark, carrier-grade NAT, and cloud metadata ranges for both
IPv4 and IPv6. Also block hostnames that resolve to any blocked address.

The policy must evaluate every address returned by DNS, not only the first
address. A hostname is unsafe if any resolved address is blocked unless an
explicit trusted-network mode is enabled for a controlled deployment.

### Redirects

Do not rely on the default automatic redirect behavior. Redirect handling must:

1. Disable automatic redirects in the standard `HttpClient` handler.
2. Read and validate each `Location` target with the same URL policy.
3. Allow only a bounded number of redirects.
4. Reject redirects that change to an unsupported scheme or blocked address.
5. Preserve the original request's header allowlist for the next request.

Playwright navigation must use an equivalent request/redirect interception
policy. The policy must apply to the initial document, redirects, and subresource
requests that can reach internal services.

### DNS rebinding and production defense in depth

Application-level DNS validation reduces SSRF risk but cannot alone guarantee
that an address will not change between DNS validation and socket connection.
The implementation should therefore use one of these defenses:

- Preferred for the standard checker: resolve and connect through a pinned safe
  address or a safe outbound proxy that performs the final egress policy check.
- Required for production deployment: restrict the API container's egress at the
  network/firewall layer so it cannot reach cloud metadata, loopback, private
  service networks, or management interfaces.
- For Playwright: run Chromium in an isolated worker/container with equivalent
  egress restrictions and intercept navigations/subrequests at the browser layer.

The application should fail closed when safe DNS resolution or egress policy
cannot be established. Do not silently fall back to unrestricted requests.

### Custom request headers

- Parse `CustomHeadersJson` once during validation and reject malformed JSON.
- Require a JSON object whose values are strings and enforce a total size and
  maximum header count.
- Reject `Host`, `Connection`, `Content-Length`, `Transfer-Encoding`, `TE`,
  `Trailer`, `Upgrade`, `Proxy-*`, `Authorization`, `Cookie`, and other
  hop-by-hop or credential-bearing headers by default.
- Reject CR/LF characters in names and values.
- Use a small explicit allowlist for headers that the product needs.
- Store the validated/canonical representation, or validate again immediately
  before every outbound request as defense in depth.
- Never log header values.

## Proposed Components

### 1. `MonitoringUrlOptions`

Add configuration for the policy, for example:

- `AllowedSchemes`
- `AllowedPorts`
- `MaxUrlLength`
- `MaxRedirects`
- `DnsResolutionTimeoutSeconds`
- `AllowPrivateNetworks` (default `false`, intended only for controlled use)
- `AllowedCustomHeaders`
- `MaxCustomHeaderCount`
- `MaxCustomHeadersLength`

Bind this section in `Program.cs`. Configuration validation must reject unsafe
production combinations such as enabling private networks without an explicit
environment/deployment override.

### 2. `IMonitoringUrlPolicy`

Create a single policy abstraction under `PulseWatch.Api/Services` or a focused
security namespace. Suggested operations:

- `ValidateAndNormalize(string rawUrl)`
- `ValidateCustomHeaders(string? rawHeadersJson)`
- `ResolveAndValidateAsync(Uri uri, CancellationToken cancellationToken)`
- `ValidateRedirect(Uri source, Uri target)`

Return a structured validation result or a domain exception that controllers can
map to a safe `400 Bad Request` response. Do not expose DNS records, internal IP
addresses, or socket details to API clients.

Keep pure URI parsing separate from DNS/network resolution so most unit tests do
not need network access.

### 3. Safe standard-check transport

Update `UptimeCheckerService` and its `HttpClient` registration to:

- Validate the website URL before the retry loop.
- Use a handler with automatic redirects disabled.
- Validate every redirect before following it.
- Apply the validated custom headers only.
- Apply a response-size limit before reading a body for keyword checks.
- Distinguish policy violations from transient target failures; policy
  violations must not be retried.
- Avoid logging raw custom headers or sensitive URL query values.

If a pinned-address connector or outbound proxy is selected, isolate that logic
behind a transport abstraction so the checker remains testable.

### 4. Safe Playwright deep-check transport

Update `DeepCheckService` and the deep-check endpoint to:

- Validate the stored URL before acquiring browser resources.
- Intercept document navigation, redirects, and subresource requests.
- Abort requests to unsupported schemes and blocked destinations.
- Limit navigation depth, page load time, response size, and screenshot size.
- Keep the current semaphore and ensure every context/page is closed on failure.
- Return a generic safe error for policy violations.

The browser must run in a network-isolated worker/container in production. Do
not treat Playwright route interception as a substitute for network egress
isolation.

### 5. Shared controller validation

Apply validation before persistence in:

- `WebsitesController.CreateWebsite`
- `WebsitesController.UpdateWebsite`
- `WebsitesController.BulkCreate`

Bulk creation should validate each URL independently and return a stable reason
code/message in `Skipped` or `Failed` without including internal resolution
details. A malformed or unsafe item must not prevent safe items from being
created unless the request-level contract explicitly chooses all-or-nothing
behavior.

The checker must validate again at execution time because a stored URL, DNS
answer, redirect target, or configuration may have changed after persistence.

## Implementation Phases

### Phase 0: Threat model and policy contract

1. Document the trust boundary: authenticated users control website URLs,
   custom headers, and response keywords; the server owns network access.
2. Decide the default port policy and whether any trusted private-network mode is
   required.
3. Decide whether production standard checks use pinned sockets or an outbound
   proxy.
4. Define stable API validation codes such as `invalid_url`,
   `unsupported_scheme`, `blocked_destination`, `unsafe_redirect`, and
   `invalid_custom_headers`.
5. Record the deployment egress requirements for Docker and SQL/Playwright
   workers.

### Phase 1: Pure policy and parsing

1. Add `MonitoringUrlOptions` and configuration validation.
2. Implement absolute URI parsing and normalization.
3. Implement IP classification for IPv4 and IPv6 blocked ranges.
4. Implement custom-header JSON parsing, size limits, and allowlist checks.
5. Add unit tests that do not call external networks.

### Phase 2: DNS and redirect-safe transport

1. Add bounded, cancellable DNS resolution.
2. Add validation for every resolved address.
3. Configure standard `HttpClient` with redirects disabled.
4. Implement bounded manual redirect handling.
5. Add response/body limits and classify policy errors as non-retryable.
6. Add integration tests using a local controllable HTTP server, including safe
   redirects and blocked redirect targets.

### Phase 3: Controller and persistence integration

1. Inject `IMonitoringUrlPolicy` into website creation/update flows.
2. Validate bulk items independently and return stable client-safe reasons.
3. Decide whether to persist the normalized URL; if so, add a migration only if
   the existing data can be normalized safely.
4. Revalidate at manual, bulk, scheduled, and deep-check execution time.
5. Add authorization tests to ensure validation changes do not bypass ownership
   checks.

### Phase 4: Playwright hardening

1. Add browser request interception for navigation and subresources.
2. Validate redirect and request URLs before allowing them to continue.
3. Configure browser/page resource limits and cancellation behavior.
4. Add tests with a local test server that attempts redirects and subresource
   requests toward blocked targets.
5. Document the required container/network egress policy for deployment.

### Phase 5: Rollout and operations

1. Add structured security logs containing website ID and policy result, never
   secrets or internal IP values.
2. Add metrics for rejected URLs, blocked redirects, policy failures, and deep
   check aborts.
3. Run the policy in report-only mode against existing records in a staging
   environment, then fail closed before production rollout.
4. Review existing stored URLs and provide an admin report for records that
   become invalid under the new policy.
5. Update `README.md`, `SECURITY.md`, and deployment documentation.

## Test Plan

### Unit tests

- Missing, malformed, relative, credential-bearing, and overlong URLs
- `file`, `ftp`, `javascript`, `data`, and other unsupported schemes
- HTTP and HTTPS with allowed/disallowed ports
- IPv4 and IPv6 loopback, private, link-local, multicast, unspecified,
  documentation, and metadata ranges
- Hostnames resolving to one or more blocked addresses
- Case, trailing-dot, and normalized-host equivalence
- Redirect to a safe URL, unsupported scheme, private IP, and excessive depth
- Valid, malformed, oversized, duplicate, and forbidden custom headers
- CR/LF injection attempts in header names and values

### Service/integration tests

- No outbound request occurs for a policy-rejected URL.
- Standard checks do not automatically follow an unsafe redirect.
- Unsafe redirect failures are not retried.
- Response-body keyword checks cannot cause an unbounded body read.
- Playwright aborts blocked document and subresource requests.
- Cancellation releases the HTTP/browser resource and semaphore.
- Manual, bulk, scheduled, and deep checks use the same policy.

### Regression tests

- Valid public websites continue to support GET, HEAD, and POST checks.
- Existing status transitions, downtime events, and notifications still work.
- Public status endpoints remain read-only and do not become a URL proxy.
- Website ownership and admin authorization behavior remains unchanged.

## Error and Logging Contract

Client-facing responses should be concise and stable, for example:

```json
{
  "code": "blocked_destination",
  "message": "This monitoring target is not allowed."
}
```

Do not return resolved IP addresses, DNS errors, internal hostnames, request
headers, credentials, stack traces, or raw exception messages. Security logs may
include a website ID, operation type, policy result, and correlation ID, but not
secret values or complete sensitive URLs.

## Rollback and Compatibility

- Keep the policy behind configuration so rollout can be staged, but keep
  `AllowPrivateNetworks` disabled by default.
- Do not provide an unrestricted bypass in the public API.
- Existing records that fail the new policy should remain stored but become
  `invalid_target` at check time until the owner updates them, unless policy or
  legal requirements require an immediate disable.
- A rollback must not re-enable unrestricted outbound access in production; use a
  network egress control as the final safety boundary.

## Acceptance Criteria

- Every user-controlled URL passes the shared policy before standard or
  Playwright outbound access.
- Unsupported schemes, blocked IP ranges, unsafe redirects, invalid headers, and
  oversized responses are rejected or aborted safely.
- URL policy violations do not disclose internal network details and are not
  retried as target failures.
- Standard checks and deep checks have unit and integration coverage for the
  threat cases listed above.
- Existing valid monitoring flows, ownership checks, downtime transitions, and
  notifications continue to pass regression tests.
- Production deployment documentation specifies outbound egress restrictions.
- Security logs and metrics contain enough information to investigate incidents
  without recording secrets.

## Suggested File Changes

Expected implementation surface:

- Add `PulseWatch.Api/Services/MonitoringUrlOptions.cs`
- Add `PulseWatch.Api/Services/IMonitoringUrlPolicy.cs`
- Add `PulseWatch.Api/Services/MonitoringUrlPolicy.cs`
- Add a safe transport/redirect handler if pinned connections or a proxy is used
- Update `PulseWatch.Api/Services/UptimeCheckerService.cs`
- Update `PulseWatch.Api/Services/DeepCheckServices.cs`
- Update `PulseWatch.Api/Controllers/WebsitesController.cs`
- Update `PulseWatch.Api/DTOs/CreateWebsiteDto.cs` and `UpdateWebsiteDto.cs` only
  if validation metadata is appropriate there
- Update `PulseWatch.Api/Program.cs` for dependency injection and HTTP handler
  configuration
- Add backend unit/integration test project and test fixtures
- Add client validation/error-state updates where API error codes are surfaced
- Update `README.md`, `SECURITY.md`, and deployment documentation

Avoid putting all security logic in DTO attributes or controllers. The runtime
checker and Playwright path must enforce the policy even when a record was
created before the feature, imported, or changed outside the normal UI.