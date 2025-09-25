# FreeLLM — Open-source CRM/tenant framework (Blazor + EF Core)

FreeLLM is a multi-tenant CRM-style web app and framework you can run as-is or extend to build your own line-of-business application. It’s built with **ASP.NET Core/Blazor**, **Entity Framework Core**, and **SignalR**, with a modular UI and a small set of helper APIs to accelerate common admin/developer workflows.

---

## Highlights

* **Blazor Web App UI**
  Clean, componentized UI with Bootstrap/MudBlazor/Radzen; supports light/dark/auto themes, custom fonts/CSS, and per-tenant theming.

* **Multi-tenant core**
  Tenants, users, groups, departments, and UDF labels are first-class. URLs can optionally include tenant codes.

* **Auth choices**
  Pluggable auth providers (Google/Microsoft/Apple/Facebook/OpenID) plus optional custom provider and local login. Per-tenant login options & rules.

* **File Explorer (Dev tool)**
  A built-in “project file explorer” on the home page lets App Admins list, filter, and fetch file contents/metadata from a server path (handy for code-review/update workflows). Backed by secured APIs with in-memory caching.

* **Real-time updates**
  SignalR hub broadcasts tenant/user/language/settings changes so connected clients stay in sync (e.g., “users on same page” detection, toasts, maintenance state).

* **Config & Setup flow**
  First-run/startup checks for database/connection string; includes a `/Setup` path and a `DatabaseOffline` UX. Maintenance Mode supported.

* **Extensibility**
  A plugin cache model is present for future add-ins. “App.*.razor” partials/regions are provided throughout to add app-specific code without forking core files.

---

## Major projects & folders (high level)

```
FreeLLM/                      # Server + core shared pieces (controllers, DI, helpers)
  Components/                 # Root App.razor + modules & HTML/JS helpers
  Controllers/                # API endpoints (incl. FileGpt endpoints)
  Classes/                    # Configuration helper (partial)
  ...                         #

FreeLLM.Client/               # Blazor client-side components
  Layout/MainLayout.razor     # Shell: navigation, SignalR wiring, toasts, theme, timers
  Shared/AppComponents/       # App-specific component extension points (Edit*, Settings, About, Index.App)
    Index.App.razor           # File Explorer + “chunked” summary builder UI

FreeLLM.DataObjects/          # Shared DTOs/models & enums (tenants, users, mail, settings, filters...)
  DataObjects.cs              # Central types (SensitiveAttribute, TenantSettings, etc.)
  GlobalSettings.App.cs       # App-specific global settings (partial)

FreeLLM.EFModels/             # EF Core DbContext and entity maps
  EFModels/EFDataModel.cs     # DbSets, indexes, relationship config
```

---

## Data model (selected types)

* **Tenants**: `Tenant`, `TenantSettings` (theme, login options, work schedule, file types, delete policy, etc.)
* **Users & groups**: `User`, `UserGroup`, `UserInGroup`, with admin/app-admin flags, photo, login metadata
* **Departments**: `Department`, `DepartmentGroup`
* **File storage**: `FileStorage` metadata (binary payload optional, soft delete supported)
* **Localization**: `Language` + `OptionPair` phrase store
* **Settings**: Key/value settings with `SettingType`
* **Security annotations**: `SensitiveAttribute` (mark fields like LDAP/JWT private keys as sensitive in UI/logging)

EF Core entities and relationships (indexes, FK constraints) are set up in `EFDataModel.cs`. Providers are configurable (SQLite/MySQL/PostgreSQL/SQL Server).

---

## Notable features & flows

### Startup & health

* The client calls `api/Data/GetStartupState` and may redirect to **/Setup** (missing connection string) or **/DatabaseOffline**.
* A 10s “check for updates” timer reloads the app if version/release changes, and reduces interval if the server goes offline.

### Maintenance Mode

* Toggle via `ApplicationSettingsUpdate`. Non-admins see a maintenance message; App Admins can still access and will see a red banner.

### Real-time (SignalR)

* Hub: `freellmHub` (client joins by TenantId).
* Broadcasts `SignalRUpdate` events for many types (User, Tenant, Language, Settings, etc.).
* “Users on same page” and other small UX niceties are supported in `MainLayout.razor`.

### File Explorer & GPT helper (Index.App)

* Client UI to:

  * Configure directory, file filters/types, ignored folders
  * Select files; fetch **metadata** for unselected and **contents** for selected
  * Generate “instructions + file content” **chunks** for pasting to an LLM
* Server endpoints (AppAdmin policy):

  * `GetFiles` — recursive file list (skips `bin/obj` and hidden folders)
  * `GetFileMetadata` — line/char counts (cached)
  * `GetFileContents` — full text (cached)
* Basic caching with `IMemoryCache` (5 minutes per file key).

---

## Authentication & authorization

* **Providers** toggled in `DataController` based on `ICustomAuthentication` (Google/Microsoft/Apple/Facebook/OpenID).
* Cookie/token + optional **device fingerprint** via ThumbmarkJS.
* Requests honor `TenantId`, `Token`, and `Fingerprint` headers/query.
* Example **policy**: `Policies.AppAdmin` protects developer-oriented endpoints.

---

## Theming & UX

* Light/dark/auto theme, persisted in local storage; syncs Monaco editor theme when present.
* Per-tenant theme/font CSS and custom imports supported.
* Bootstrap Icons, Font Awesome, MudBlazor, Radzen components available.
* Toast message queue with auto-hide and max persistent count per tenant settings.

---

## Running locally

### Prereqs

* .NET SDK (8+ recommended)
* A database (choose one):

  * SQLite (easiest for development)
  * MySQL / PostgreSQL / SQL Server

### 1) Configure the connection string

Choose one approach:

* **Code config** (for quick dev): in `EFDataModel.cs` there are commented provider samples; typically you configure via `appsettings.json` or environment variables wired into your DI.
* **Setup flow**: run the app and navigate to `/Setup` to provide a connection string.

Supported providers (from `DataObjects.ConnectionStringConfig`):

* SQLite (`SQLiteDatabase`)
* SQL Server (`SqlServer_*`)
* PostgreSQL (`PostgreSql_*`)
* MySQL (`MySQL_*`)

### 2) Database migrations

Create/apply your EF Core migrations for your chosen provider. (The project includes entity maps; generate migrations in your local solution and update the DB.)

```bash
# Example (adjust project names/paths/provider):
dotnet ef migrations add InitialCreate -p FreeLLM.EFModels -s FreeLLM
dotnet ef database update -p FreeLLM.EFModels -s FreeLLM
```

### 3) Build & run

```bash
dotnet restore
dotnet build
dotnet run --project FreeLLM
```

Open the printed URL (typically `https://localhost:****/`).
On first run you may be redirected to `/Setup` if the DB isn’t configured.

---

## Configuration knobs (selected)

* **Base path** & **Analytics** (`ConfigurationHelper` partial)

  * `BasePath` for `<base href>` if hosting under a sub-path
  * `AnalyticsCode` (supports a simple “G-XXXX” GA code or a full `<script>` block)
* **App settings** (`ApplicationSettingsUpdate`)

  * `MaintenanceMode`
  * `UseTenantCodeInUrl`, `ShowTenantListingWhenMissingTenantCode`
  * `ApplicationURL`
* **Tenant settings** (`TenantSettings`)

  * Theme: `Theme`, `ThemeCss`, `ThemeFont`, `ThemeFontCssImport`
  * Login rules: `AllowUsersToSignUpForLocalLogin`, `RequirePreExistingAccountToLogIn`, etc.
  * Delete behavior: `DeletePreference` (Immediate/MarkAsDeleted) + purge days
  * Work schedule & allowed file types
  * Sensitive fields (LDAP/JWT keys) are marked with `[Sensitive]`

---

## Security notes

* Many fields are annotated with `SensitiveAttribute` to help ensure they’re never casually exposed in logs or UI.
* Developer file APIs are protected by an App Admin policy and exclude hidden/bin/obj directories.
* Device fingerprint (ThumbmarkJS) is captured and tied to tokens to reduce token theft risk.

---

## Customization points

* **`*.App.*` partials**: sprinkle across server/controllers/components so you can add/override behavior without modifying core files.
* **`Modules.App.razor`**: inject per-app `<head>`/`<body>` fragments and JavaScript hooks.
* **Client components** in `Shared/AppComponents/*` provide slots to extend edit forms (Tenant/User/Department/Settings) and About page.

---

## Development tips

* When modifying UI behaviors, watch the timers in `MainLayout.razor` (`ThemeWatcher`, `MessageWatcher`, `CheckForUpdates`).
* To extend SignalR handling, see `ProcessSignalRUpdate` in `MainLayout.razor` and the server’s `SignalRUpdate` plumbing in `DataController`.
* If you add new APIs, mirror the style in `DataController.App.*` files and use `ActionResponseObject`/`BooleanResponse` where helpful.

---

## Roadmap ideas (suggested)

* Finish wiring a polished **plugin model** (runtime load/register).
* Add **role/permission granularity** beyond Admin/AppAdmin (module/action scopes).
* Expand built-in **audit logs** and **export/import** for tenant settings.
* Provide **seed data** & sample migration scripts per provider.

---

## Contributing

PRs and issues welcome! Keep changes modular by using the provided `*.App.*` extension points where possible.

---

## License

Add a license file (e.g., MIT/Apache-2.0) to clarify usage. Until then, treat this as “all rights reserved” for redistribution purposes.

---

## Credits

Built with ❤️ on:

* ASP.NET Core + Blazor
* EF Core
* SignalR
* Bootstrap/MudBlazor/Radzen
* ThumbmarkJS (device fingerprint)

---

## Quick start (TL;DR)

1. `dotnet restore && dotnet build`
2. Configure a DB connection (use `/Setup` or your `appsettings`).
3. `dotnet run --project FreeLLM` and open the site.
4. Create a tenant + admin user, tweak **Settings** (theme, auth), and explore the **File Explorer** on the home page (App Admin only).
