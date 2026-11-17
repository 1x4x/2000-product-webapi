# 🧪 ProductAPI testing guide

This repository contains **xUnit unit tests** and **integration tests**. Test projects are included in `ProductAPI.sln`.

## Requirements

- .NET 8 SDK
- Docker Engine / Docker Desktop running for the MySQL integration suite
- Internet access for first-time NuGet restore and MySQL test image pull

No production database credentials are required. Integration tests use an ephemeral MySQL 8 Docker container and a disposable database. They **must not** connect to `walteradmin`.

## Run locally

```bash
dotnet restore ProductAPI.sln
dotnet build ProductAPI.sln
dotnet test ProductAPI.Tests.Unit/ProductAPI.Tests.Unit.csproj
dotnet test ProductAPI.Tests.Integration/ProductAPI.Tests.Integration.csproj
```

To run both suites and collect coverage:

```bash
dotnet test ProductAPI.sln --collect:"XPlat Code Coverage"
```

### Unit test coverage

- Reorder threshold boundary at exactly 20%
- Available stock accounting for reserved quantities
- Fixed reorder quantity and target-stock top-up
- Prevention of over-reserving stock
- Validation of partial and final receipt quantities
- Deterministic API-key hashing

### Integration test coverage

- Anonymous product API requests rejected
- Authorized product listing without supplier purchase costs
- Read-only token denied reseller order creation
- Expired/revoked tokens rejected
- Successful reseller order reserves inventory
- Overselling rejected
- Disabled reseller denied API access
- Purchase-order receipt persistence (EF InMemory model test)

## CI

GitHub Actions workflow: `.github/workflows/tests.yml`. It restores, builds, runs unit tests and runs integration tests on GitHub's Docker-enabled Ubuntu runner. TRX test reports are uploaded as workflow artifacts.

## Limitations and next test priorities

- **Not yet executed in this environment**: .NET SDK and Docker were unavailable when these tests were authored. CI results should be inspected before treating the suite as passing.
- The receipt persistence check uses EF InMemory and is not a full HTTP-level purchasing workflow test. Add a MySQL-backed Razor Pages workflow test including anti-forgery and admin sign-in.
- Add parallel order stress tests, cancellation/reservation release tests, and idempotency tests for stock receipts.
- Add migration-from-existing-schema tests once migrations are committed.
- The API currently returns `Unauthorized` for insufficient scopes; consider `Forbidden` for authenticated tokens without permission.


### Product image tests

Unit tests validate the 20-image limit, allowed file signatures and maximum file size. An EF InMemory persistence test covers image-to-product relationships. Add end-to-end upload, deletion, file validation and MySQL concurrency tests before production deployment.
