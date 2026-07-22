# AutVent Desktop Architecture (Phase 1)

## Why this architecture
AutVent Desktop is offline-first and must stay responsive for cashiers even when internet is unstable. The solution is split by responsibility so business rules, infrastructure, sync behavior, and UI evolve independently and remain testable.

## Solution layers
- **AutVent.Domain**: core entities and domain enums only. No framework dependencies.
- **AutVent.Application**: use-case orchestration, module service contracts, repository contracts, and DTOs.
- **AutVent.Infrastructure**: external concerns (SQLite, HttpClient API clients, encrypted session persistence, connectivity checks).
- **AutVent.Sync**: background queue-based sync worker and conflict-safe sync orchestration.
- **AutVent.Shared**: cross-cutting primitives (result/error models, constants).
- **AutVent.Desktop**: Avalonia UI, MVVM view models, navigation, and composition root.

## Dependency direction
- Desktop -> Application, Infrastructure, Sync, Shared
- Sync -> Application, Infrastructure, Shared
- Infrastructure -> Application, Domain, Shared
- Application -> Domain, Shared
- Domain -> (none)

This keeps domain logic independent and aligns with SOLID/Clean Architecture.

## DI and composition
The desktop project is the composition root. It builds a host and registers:
- Application services (`AddApplication`)
- Infrastructure services (`AddInfrastructure`)
- Sync engine (`AddSyncEngine`)

All modules consume interfaces. This enables unit tests with mocked repositories/API clients.

## MVVM and navigation
- `ViewModelBase` uses `CommunityToolkit.MVVM`.
- `NavigationService` owns the current route and selected module.
- `ShellViewModel` drives a collapsible sidebar and top-level module navigation.

Navigation is view-model-first, enabling keyboard-friendly workflows and clear state transitions.

## API layer decisions
Typed clients are used for auth, products, inventory, and sales. `HttpClientFactory` controls lifecycle and policies. A delegated auth handler attaches access tokens and supports refresh handling in a central place.

## SQLite decisions
SQLite is the source of truth for local operations:
- Products
- Inventory
- Sales
- SaleItems
- Stores
- Settings
- PendingSync
- AuthenticationSession

Writes are local-first. Sync pushes queued operations and pulls updates in background.

## Authentication decisions
Rules implemented in architecture:
1. First login must be online.
2. Successful online login caches encrypted auth session and business bootstrap data.
3. Offline login is allowed only with valid non-expired encrypted cached session.
4. Passwords are never persisted locally.

## Sync engine decisions
- Runs every 30 seconds as a hosted background service.
- Processes queued pending operations idempotently.
- Retries transient network failures.
- Downloads server updates for products, inventory, and settings.
- Never blocks UI flow.

## UI decisions
Fluent Avalonia theme with modern minimal spacing and rounded cards. Dark mode is supported by theme variants. Branding color `#2563EB` is registered as primary accent resource.
