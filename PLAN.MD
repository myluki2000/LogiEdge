# LogiEdge — Codebase Overview & Inner Workings

> This document is written for AI agents (and new developers) to quickly understand the
> architecture, data flow, and conventions of this codebase before making changes.

## 1. What Is This Project?

LogiEdge is a **logistics / warehouse management web application** built as a
**modular monolith** on **.NET 10 (net10.0) / ASP.NET Core**. A single executable
host app (`LogiEdge`) boots all business-logic libraries and Blazor UI libraries,
wiring them together through a custom **module system**.

Core capabilities:

- **Warehouse management**: track items in warehouses over time (item states),
  manage inbound / outbound / relocation inventory transactions with a
  draft → booked lifecycle.
- **Customer management**: customers own item schemas and items.
- **Excel inventory import**: bulk-import daily Excel inventory spreadsheets from
  customer folders, auto-detecting arrivals, moves, exits, and comment changes.
- **Document generation**: store HTML templates in the DB, render them with JSON
  data to PDF via a [Gotenberg](https://gotenberg.dev) container service.
- **Planning board**: Kanban-style planning (scaffolded, mostly unimplemented).

### Tech stack

| Concern | Technology |
|---|---|
| Language / framework | C# (.NET 10), ASP.NET Core |
| UI | Blazor Server (Web App) + Razor Pages, **Havit.Blazor.Components.Web.Bootstrap 4.20** (`Hx*` components) |
| Auth | ASP.NET Core Identity (EF stores, `IdentityUser`, account confirmation required) |
| ORM / DB | EF Core 10 + Npgsql → **PostgreSQL** (two databases: `appidentity`, `application`) |
| Mapping | AutoMapper 16 (profiles auto-discovered from module assemblies) |
| Excel parsing | ClosedXML 0.105 |
| PDF generation | Gotenberg 8 container via `Gotenberg.Sharp.Api.Client` 3.0 (docker-compose, port 3000) |
| Tests | xUnit 2.9, Testcontainers.PostgreSql 4.9, Microsoft.NET.Test.Sdk 18 |
| CI | SonarQube Cloud scanner on push/PR to `master` (`.github/workflows/build.yml`, windows-latest) |

## 2. Repository Layout

Solution: `LogiEdge.sln` (15 projects). Three layers: **host**, **services**
(class libraries), and **WebUI** (Razor class libraries with Blazor components).

| Project | Kind | Purpose |
|---|---|---|
| `LogiEdge/` | Executable (Sdk.Web) | Host app: DI wiring, Identity, pipeline, host pages (`/`, `/Settings`) |
| `LogiEdge.Shared/` | Class lib (+AspNetCore framework ref) | Module contract, base DbContext, shared attributes/utilities |
| `LogiEdge.BaseService/` | Class lib | Cross-cutting services: settings, file attachments, memory cache; `ApplicationDbContext` |
| `LogiEdge.CustomerService/` | Class lib | Customer entity + CRUD service; `CustomerDbContext` |
| `LogiEdge.WarehouseService/` | Class lib | Warehouse domain: items, item states, transactions, dynamic items-table query engine |
| `LogiEdge.ExcelImporterService/` | Class lib | Excel inventory import (ClosedXML); depends on Customer + Warehouse services |
| `LogiEdge.Service.PlanningBoard/` | Class lib | Planning board entities/context (scaffold only) |
| `LogiEdge.Service.DocumentGenerator/` | Class lib | Document templates, generator discovery, Gotenberg PDF rendering |
| `LogiEdge.WebUI.Shared/` | Razor class lib | Shared layout/nav/login + reusable Blazor components |
| `LogiEdge.WebUI.Warehouse/` | Razor class lib | Warehouse pages & transaction/item-table components (largest UI) |
| `LogiEdge.WebUI.Customers/` | Razor class lib | Customer pages |
| `LogiEdge.WebUI.PlanningBoard/` | Razor class lib | Planning board page (stub) |
| `LogiEdge.WebUI.DocumentGenerator/` | Razor class lib | Template management pages + generic document component |
| `LogiEdge.Test.ExcelImporterService/` | xUnit test project | Import tests with real `.xlsx` fixtures in `Resources/` |
| `LogiEdge.Test.Shared/` | xUnit test project | Test infra: `TestContextFactory`, Testcontainers fixture |

### Dependency graph (simplified)

```
LogiEdge (host)
├── LogiEdge.BaseService ──────────── LogiEdge.Shared
├── LogiEdge.CustomerService ──────── BaseService, Shared
├── LogiEdge.WarehouseService ─────── BaseService, CustomerService, Shared
├── LogiEdge.ExcelImporterService ─── BaseService, CustomerService, WarehouseService, Shared
├── LogiEdge.Service.PlanningBoard ── Shared only (no BaseService)
├── LogiEdge.Service.DocumentGenerator ─ BaseService, Shared
└── WebUI.* ───────────────────────── their matching service lib(s), WebUI.Shared
    WebUI.Warehouse additionally refs WebUI.DocumentGenerator (document generation on transactions)
```

## 3. The Module System (central architecture concept)

Everything hangs off `LogiEdge.Shared/IServiceModuleConfiguration.cs`:

```csharp
public interface IServiceModuleConfiguration
{
    void RegisterServices(WebApplicationBuilder builder); // DI registration
    void OnAppBuilt(WebApplication app);                  // post-build hooks (migrations, endpoints)
    Assembly Assembly { get; }                            // used for Router + AutoMapper discovery
}
```

`LogiEdge/Program.cs` holds a **hardcoded list of 10 modules** (order matters only
for readability): `BaseServiceModuleConfiguration`, `CustomerManagementModuleConfiguration`,
`CustomerServiceModuleConfiguration`, `WarehouseModuleConfiguration`,
`WarehouseServiceModuleConfiguration`, `ExcelImporterServiceModuleConfiguration`,
`PlanningBoardServiceModuleConfiguration`, `PlanningBoardWebUiModuleConfiguration`,
`DocumentGeneratorServiceModuleConfiguration`, `DocumentGeneratorWebUiModuleConfiguration`.

The host then:

1. Calls `RegisterServices(builder)` on each module (DI).
2. Registers AutoMapper, scanning **all module assemblies** for profiles.
3. Stores the modules in a singleton `ServiceModuleConfigurationCollection`.
4. Calls `OnAppBuilt(app)` on each module — this is where every EF context runs
   `Database.Migrate()` (migrations are applied automatically at startup; no manual
   migrate step needed).

**WebUI module configuration classes are mostly empty stubs**, but they MUST be
registered in `Program.cs`: the Blazor `Router` in `LogiEdge/App.razor` uses
`AdditionalAssemblies="@Modules.Select(x => x.Assembly)"`, so a WebUI assembly only
gets its `@page` routes discovered if its module is in this list.

## 4. Host App Startup (`LogiEdge/Program.cs`)

Pipeline order: Identity setup → Havit services → Razor Pages + Server-Side Blazor →
module registration → AutoMapper → build → module `OnAppBuilt` → (dev:
`UseMigrationsEndPoint` / prod: exception handler + HSTS) → HTTPS redirect → static
files → routing → authorization → `MapControllers()` → `MapBlazorHub()` →
`MapFallbackToPage("/_Host")`.

Identity details:

- `AppIdentityDbContext` (`LogiEdge/Data/AppIdentityDbContext.cs`) is a plain
  `IdentityDbContext`, uses the **`AppIdentityConnection`** connection string (DB `appidentity`).
- `AddDefaultIdentity<IdentityUser>` with `SignIn.RequireConfirmedAccount = true`.
- `RevalidatingIdentityAuthenticationStateProvider` (`LogiEdge/Areas/Identity/`) revalidates
  the auth state per circuit.

### Configuration (`LogiEdge/appsettings.json`)

- `ConnectionStrings:AppIdentityConnection` → `localhost:5432`, DB `appidentity`, user `postgres` / `root`.
- `ConnectionStrings:DatabaseConnection` → `localhost:5432`, DB `application` (all business data).
- `GotenbergSharpClient` → service URL `http://localhost:3000` (+ retry policy).

## 5. Data Layer

### Databases & contexts

| DbContext | Project | Database | Notes |
|---|---|---|---|
| `AppIdentityDbContext` | host | `appidentity` | Identity only |
| `ApplicationDbContext` | BaseService | `application` | `Settings` (KV strings), `Attachments` (files) |
| `CustomerDbContext` | CustomerService | `application` | `Customers` |
| `WarehouseDbContext` | WarehouseService | `application` | warehouses, items, states, schemas, transactions |
| `PlanningBoardDbContext` | PlanningBoard | `application` | `Boards` — **no Migrations folder exists** (see Known Issues) |
| `DocumentGeneratorDbContext` | DocumentGenerator | `application` | TPT templates |

All business contexts derive from `LogiEdge.Shared/LogiEdgeDbContext.cs`, which:

- Forces `UseNpgsql(DatabaseConnection)` in `OnConfiguring` (connection string is
  always taken from config, even when options are passed).
- Sets **`QueryTrackingBehavior.NoTracking` by default** — call `.AsTracking()` for updates.
- Installs an EF convention (`LogiEdge.Shared/Conventions/AutoIncludeAttributeConvention.cs`)
  honoring the `[AutoInclude(bool)]` attribute on navigation properties (e.g.
  `FileAttachment.Data` is `[AutoInclude(false)]`).

Because several contexts map overlapping entities, `WarehouseDbContext.OnModelCreating`
marks `Customer` with `.ExcludeFromMigrations()` so only `CustomerDbContext` migrates it.
Each module's `OnAppBuilt` runs its own context's migrations — multiple contexts share
the one `application` database.

### Context usage pattern (important)

Services are registered as **singletons** and take an `IDbContextFactory<T>`; each
operation creates/uses a short-lived context (`await using ... = await factory.CreateDbContextAsync()`).
This is deliberate: Blazor circuits outlive scoped lifetimes, so never register or
inject a raw `DbContext` into a singleton. Services expose both sync and async
methods (sync versions are `.Result` wrappers — prefer the async ones in UI code).

## 6. Module Deep Dives

### 6.1 BaseService (`LogiEdge.BaseService/`)

- `SettingsService` — string key/value store in `ApplicationDbContext.Settings`.
  Generic app configuration lives here (e.g. the Excel importer config path is stored
  under key `ExcelImporterConfig` and edited on the `/Settings` page).
- `FileAttachmentService` + `Data/FileAttachment.cs` — file storage as rows
  (`FileName`, `ContentType`, owned `BinaryData`). Download endpoint:
  `GET /api/attachments/{attachmentId:guid}/download` (`Controllers/FileAttachmentController.cs`).
- `MemoryCacheService` + `MemoryCache/CachedFile.cs` — caches files in IMemoryCache
  and returns a Guid; if `CachedFile.ServeAtEndpoint` is true, the bytes are served at
  `GET /tmp/{id:guid}` (mapped in `BaseServiceModuleConfiguration.OnAppBuilt`). This is
  how PDF previews are embedded in Blazor (`<embed src="/tmp/{guid}">`).

### 6.2 CustomerService (`LogiEdge.CustomerService/`)

- `Customer` entity: `Id`, `Abbreviation` (unique-ish, uppercased on lookup), `Name`.
- `CustomerManagementService`: list/get-by-abbreviation/get-by-id/create (sync + async).
- Used by WarehouseService (items belong to customers) and ExcelImporterService
  (auto-creates missing customers with a 2-letter uppercase abbreviation).

### 6.3 WarehouseService (`LogiEdge.WarehouseService/`) — the core domain

**Entities** (`Data/`):

- `Warehouse` — name + items.
- `Item` — belongs to an `ItemSchema` and a `Customer`; has `ItemNumber`,
  `AdditionalProperties` (a JSON document of schema-defined dynamic properties),
  `Comments` (sorted by date), and `ItemStates` (its history). Implements `IDisposable`
  (disposes the JsonDocument). `GetAdditionalProperty(name)` deserializes a dynamic
  property according to its declared type (requires `ItemSchema` to be loaded).
- `ItemSchema` — named template of additional properties; many-to-many with customers.
  `ItemSchemaProperty` is `[Owned]`: name + `SupportedTypes` enum (String, Int32, Single,
  Boolean, DateTime) + IsRequired/IsUnique.
- `ItemState` — one row per (item, date, warehouse, location): the item's position in
  time. `IsInWarehouse` excludes special locations `__PRE_ARRIVAL` and `__SHIPPED`
  (`SpecialLocations.cs` also defines `__ARRIVAL_AREA`, `__LOADING_AREA`).
  `RelatedTransactionId` links a state to the transaction that created it.
- `Comment` — author, date, text, `Retracted` flag (comments are retracted, not deleted).
- `InventoryTransaction` (`Data/Transactions/`) — `Title`, `CreatedDate`, `BookedDate?`,
  `CreatedByUserId`, `HandledByWorker`, comments, attachment ids, `State` (DRAFT | BOOKED).
  Has up to one part per kind via **TPT inheritance** on `InventoryTransactionPart`
  (keyed by `TransactionId`):
  - `InboundTransactionPart.DraftItems` — `InboundDraftItem`s (schema/customer/warehouse,
    item number, additional props, location, count, quarantine, comment). Items do not
    exist in the DB until booking.
  - `OutboundTransactionPart.DraftSelectedItems` — concrete items selected for shipping;
    `DraftPlaceholderItems` — rows where a specific item wasn't picked yet.
  - `RelocationTransactionPart` — empty; booking is a TODO.

**Booking flow** (`Services/WarehouseManagement/TransactionManagementService.cs`):

- `BookTransactionAsync` (DRAFT only) sets `BookedDate`, then per part:
  - **Inbound**: each draft item becomes a real `Item` with an initial `ItemState`
    (location from the draft); failures throw `BookingException`.
  - **Outbound**: adds a new `ItemState` with `Location = __SHIPPED` to each selected
    item (fails if already shipped or stateless).
  - **Relocation**: not implemented.
- New states are recorded on `InventoryTransactionPart.NewItemStates` for UI display.
- Draft transactions can be deleted (`DeleteTransactionAsync`, DRAFT only).

**Outbound helpers** (`Services/WarehouseManagement/OutboundTransactionService.cs`):
add/remove selected items, load parts, and find the last in-warehouse state ids of a
booked transaction (used to show "where items were before shipping").

**ItemsTable query engine** (`Services/ItemsTable/`) — a dynamic column/filter/sort
system used by all item tables:

- Which properties are displayable/filterable is driven by attributes on the entity:
  `[DisplayColumnProperty]` (shows as a column) and `[QueryFilterableProperty(optional
  compare-with property)]` (filterable via query params; e.g. `Customer` filters by
  `CustomerId`). Schema additional properties are always displayable + filterable.
- `ItemsTableService.GetItemsAtStateAsync / GetItemsAsync / GetAllItemsAsync` build an
  `IQueryable<ItemState>` (with deep Includes) and apply:
  - `ItemsTablePropertyFilter` — builds **expression trees**: equality filters for base
    item properties and state properties; for additional (JSON) properties it emits a
    Npgsql `JsonContains` call with a JSON filter document.
  - `ItemsTableStateFilter` — picks which state to show per item: latest, or point-in-time
    (`AtTime`), optionally excluding shipped items (`ShowShipped`).
- Results are projected into `ItemsTableResultDto` (items + available property metadata)
  and sorted **in memory** by `ItemsTableSorter` (any base or additional property).
- Filter/sort/state/column-display parameters are immutable record-style parameter types
  with `With*`/`Without*` copy methods, so UI components can bind them to query strings.

### 6.4 ExcelImporterService (`LogiEdge.ExcelImporterService/`)

Entry point: **`GET /RunExcelImport`** (mapped in the module's `OnAppBuilt`).

Flow (`Services/ExcelImportingService.cs`):

1. Read setting `ExcelImporterConfig` → a **file system path to a JSON config file**.
2. `ExcelImporterConfig` = `FolderPath`, `DayOffset` (declared but currently unused),
   `InventoryItemSchemas[]` (name, customers, additional properties + types),
   `InventoryFileMatchingOptions[]` (customer, warehouse, file name, worksheet, schema,
   column map, identifying columns).
3. Ensure schemas exist (throws if a same-named schema has different properties);
   ensure listed customers exist (auto-created); link schema↔customer.
4. For each matching options entry, get-or-create the warehouse (customer must already
   exist or it throws), then iterate the subdirectories of `FolderPath`, which are named
   `yyyy-MM-dd`. The **first (earliest) date is saved as a baseline snapshot** (items
   created directly); later dates are diffed against existing items.
   `Internal/InventoryFileMatcher.cs` reads each Excel file with ClosedXML: requires an
   **autofilter** on the worksheet, maps configured column names to indices in row 1,
   and parses rows into `InventoryItem`s. Column types: `ITEM_NUMBER`, `ENTRY_DATE`,
   `ITEM_COUNT` (row is duplicated N times; text like "3 GB" is parsed),
   `STORAGE_LOCATION`, `EXIT_DATE`, `COMMENT`, `OTHER` (→ additional property).
5. Match each row to an existing item via `IdentifyingColumns` (item number, entry date,
   location, or other props); otherwise create the item. Existing items get new states:
   exit-date set → `__SHIPPED`; location changed → move state; comment changed → all old
   comments retracted + new one added. Items absent from the file are marked shipped.

### 6.5 Service.DocumentGenerator (`LogiEdge.Service.DocumentGenerator/`)

- Templates stored in DB with TPT: abstract `DocumentGenerationTemplate` (name,
  `InputTypeName`) → `HtmlDocumentGenerationTemplate` (Html + header/footer HTML).
- Generators implement `IDocumentGenerator<TInput>`; implementations are discovered by
  **reflection** over the assembly. `DocumentGenerationService.GetGeneratorsForDocumentTypeAsync<T>()`
  matches each stored template to a generator whose static `IDocumentGenerator.TemplateType`
  equals the template's runtime type, then instantiates it with
  `(IServiceProvider, template)` constructor args.
- The only generator today: `HtmlPdfDocumentGenerator<T>` — serializes the input object
  to JSON, injects `<script>const data = {...}</script>` before the template HTML, and
  renders A4 PDF via the Gotenberg client (`HtmlToPdfAsync`).
- `GenerateDocument` returns an **unsaved** `FileAttachment` (content type hardcoded to
  `application/pdf` — TODO). Saving is done by callers.
- Template CRUD: create/get/update/delete in `DocumentGenerationService`.

### 6.6 Service.PlanningBoard (`LogiEdge.Service.PlanningBoard/`)

Scaffold only: `Board` (title, columns, items), `BoardColumn`, `BoardItem` with owned
`BoardItemPosition` (column + start/end dates + positions). No service class, no
migrations, and the UI is a static stub.

## 7. WebUI Layer (Blazor)

- All WebUI projects are **Razor class libraries**; pages declare `@page` routes inside
  them and become routable because their module assembly is passed to the `Router`
  (see §3). Shared usings come from `LogiEdge.WebUI.Shared/GlobalUsings.cs`
  (`global using static LogiEdge.Shared.Utility.LinkHelpers;`) — note `WebUI.Customers`
  uses Sdk.Web with `OutputType=Library`.
- `LogiEdge.WebUI.Shared/`: `MainLayout` (sidebar + top bar, padding can be disabled per
  page via cascading `Layout.SetPadding(false)`), `NavMenu`, `LoginDisplay`, plus
  reusable components: `FileAttachmentManagerComponent`, `DraggableList`,
  `DebouncerComponent`.
- **Warehouse UI** (`LogiEdge.WebUI.Warehouse/`): item tables
  (`Components/WarehouseItemTable/*`) built on the ItemsTable engine with dynamic
  column display, filters (badge components), grouping; transaction pages with
  draft/booked and inbound/outbound sub-components; per-type property value input
  components (`Components/PropertyValueInput/*`).
- **DocumentGenerator UI**: generic `DocumentGeneratorComponent<TInputType>`
  (constrained to `IDocumentGeneratorInput`) — lists compatible templates, generates a
  PDF, previews it via `/tmp/{guid}`, and raises `OnFileAttachmentSaveRequested` for the
  parent page to persist. Template management pages live under
  `/document-generator/templates/`.
- **Customers UI**: list + create pages (simple).
- Programmatic links: `LinkHelpers.RouteTo<T>(params)` parses a component's route
  template and substitutes parameters (used across components instead of hardcoding URLs).

### Routes & endpoints quick reference

| Route | Kind | Source |
|---|---|---|
| `/` | Blazor page | `LogiEdge/Pages/Index.razor` (contains debug `Component1`) |
| `/Settings` | Blazor page | settings KV editor (`ExcelImporterConfig` etc.) |
| `/customer`, `/customer/create` | Blazor pages | WebUI.Customers |
| `/warehouse/items`, `/warehouse/item/{ItemId}`, `/warehouse/create` | Blazor pages | WebUI.Warehouse |
| `/warehouse/transactions/`, `/warehouse/transactions/{TransactionId:guid}` | Blazor pages | WebUI.Warehouse |
| `/planning-board` | Blazor page (stub) | WebUI.PlanningBoard |
| `/document-generator/templates/`, `.../{TemplateId:guid}` | Blazor pages | WebUI.DocumentGenerator |
| `/tmp/{id:guid}` | Minimal API | cached file serving (PDF previews) |
| `/api/attachments/{attachmentId:guid}/download` | MVC controller | attachment download |
| `/RunExcelImport` | Minimal API | triggers Excel import |

## 8. Conventions & Patterns to Follow

- **C# style**: latest language features everywhere — primary constructors, file-scoped
  namespaces, collection expressions (`[]`), nullable enabled. `WebUI.Warehouse` sets
  `LangVersion=preview`.
- **New DI services**: register in your module's `RegisterServices`; prefer singletons
  taking `IDbContextFactory<T>` (see §5).
- **EF**: NoTracking by default — `.AsTracking()` for writes; use the context factory,
  not a cached context. Add new migrations per owning project (`Migrations/` folders);
  they auto-run at startup.
- **AutoMapper**: add `Profile` classes in the module assembly (discovered automatically).
- **UI attributes**: mark entity properties with `[DisplayColumnProperty]` /
  `[QueryFilterableProperty]` to expose them in item tables; use `[AutoInclude(false)]`
  for heavy navigations.
- **Documents**: input types must implement `IDocumentGeneratorInput`; templates are DB
  rows, generators are classes implementing `IDocumentGenerator<T>`.
- **Files**: persist via `FileAttachmentService`; preview via `MemoryCacheService` +
  `/tmp/{guid}`; download via the attachments API.

## 9. Build, Run & Test

Prerequisites: **.NET 10 SDK**, Docker (for Postgres + Gotenberg).

```bash
# 1. Infrastructure (PostgreSQL :5432 with user postgres/root, Gotenberg :3000)
docker compose up -d

# 2. Build everything
dotnet build LogiEdge.sln

# 3. Run the app (migrations apply automatically on startup)
dotnet run --project LogiEdge
# https://localhost:7273 (http://localhost:5085) — see LogiEdge/Properties/launchSettings.json

# 4. Tests
dotnet test LogiEdge.Test.ExcelImporterService   # needs Docker (Testcontainers postgres:17-alpine)
```

Notes:

- The Excel importer expects the JSON config path stored in setting `ExcelImporterConfig`
  to point at a real file; its folder layout is `<FolderPath>/<yyyy-MM-dd>/<fileName>`.
- CI (`build.yml`) only runs SonarQube analysis + `dotnet build`; it does **not** run tests.

## 10. Known Issues, TODOs & Gotchas

- **Broken test fixture**: `LogiEdge.Test.ExcelImporterService/ExcelImportingServiceTest.cs`
  — `DatabaseFixture.InitializeAsync` sets both context factories to `null` with a
  "TODO: Fix test" (the commented-out code shows the intended Testcontainers setup).
  Tests currently throw NREs.
- **PlanningBoard has no migrations** despite calling `Database.Migrate()`; tables for
  `Boards` are never created by EF. The UI is a static stub.
- **Relocation transactions**: part entity and booking logic are unimplemented (TODO).
- `WarehouseManagementService` is an empty class — its logic moved to
  `TransactionManagementService` (see git history "Move Transaction deletion logic...").
- `DocumentGenerationService.GenerateDocument` hardcodes `application/pdf` (TODO: derive
  from generator); `GetGeneratorsForDocumentTypeAsync` re-queries the DB every call
  (TODO: cache).
- `NavMenu` still contains template links (`counter`, `fetchdata`) that don't exist.
- `LogiEdge/Pages/Index.razor` renders `Component1` (a debug placeholder from
  WebUI.Warehouse).
- `WarehouseService/Data/ItemId.cs` is entirely commented out (legacy composite-id idea).
- Services expose sync wrappers using `.Result` over async methods — fine for current
  usage, but don't call them from a synchronized-context-sensitive path without care.
- Connection strings in `appsettings.json` use local credentials (`postgres`/`root`)
  matching `docker-compose.yml`; there are no environment-specific overrides beyond
  `appsettings.Development.json`.

## 11. How to Add a New Module (checklist)

1. Create the service class lib `LogiEdge.Service.X` (ref `LogiEdge.BaseService` if it
   needs persistence/settings/attachments; otherwise just `LogiEdge.Shared`).
2. Implement `IServiceModuleConfiguration`: register services + `IDbContextFactory<T>`
   in `RegisterServices`; run `ctx.Database.Migrate()` in `OnAppBuilt`.
3. Add a `Persistence/XDbContext : LogiEdgeDbContext<XDbContext>` and initial migration
   (`dotnet ef migrations add ...` against that context).
4. If there is UI: create Razor class lib `LogiEdge.WebUI.X` (ref `WebUI.Shared`, the
   service lib, and link `..\LogiEdge.WebUI.Shared\GlobalUsings.cs` like the others),
   put pages in with `@page` routes, and add an (empty) `XWebUiModuleConfiguration`.
5. Add both module classes to the list in `LogiEdge/Program.cs` and reference the new
   projects from `LogiEdge/LogiEdge.csproj`.
6. AutoMapper profiles in either assembly are picked up automatically.

## 12. Where Things Live (cheat sheet)

| Question | Look at |
|---|---|
| How does startup/DI work? | `LogiEdge/Program.cs`, `LogiEdge.Shared/IServiceModuleConfiguration.cs` |
| DB setup & conventions | `LogiEdge.Shared/LogiEdgeDbContext.cs`, per-module `Persistence/*DbContext.cs` |
| Warehouse transaction lifecycle | `LogiEdge.WarehouseService/Services/WarehouseManagement/TransactionManagementService.cs` |
| Dynamic item tables | `LogiEdge.WarehouseService/Services/ItemsTable/` + `LogiEdge.WebUI.Warehouse/Components/WarehouseItemTable/` |
| Excel import behavior | `LogiEdge.ExcelImporterService/Services/ExcelImportingService.cs`, `Internal/InventoryFileMatcher.cs` |
| PDF generation | `LogiEdge.Service.DocumentGenerator/DocumentGeneration/HtmlPdfDocumentGenerator.cs` + docker-compose `gotenberg` |
| File storage/previews | `LogiEdge.BaseService/Services/FileAttachmentService.cs`, `MemoryCacheService.cs`, `BaseServiceModuleConfiguration.cs` (`/tmp/{id}`) |
| App settings (KV) | `LogiEdge/Pages/Settings.razor`, `LogiEdge.BaseService/Services/SettingsService.cs` |
