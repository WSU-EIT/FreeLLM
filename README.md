# FreeLLM — Prepare Code Context for AI (LLMs) the Right Way

FreeLLM is a lightweight Blazor app that helps engineers assemble the *right* source files and *clear* intent before handing work to AI (large language models, “LLMs”). Instead of copy-pasting random snippets into a chat window, FreeLLM guides you to select files, add instructions, and export a clean, chunked prompt that consistently produces better AI results.

> **Target stack:** C# / **.NET 9** / Blazor, designed to integrate with the **FreeCRM** framework: github.com/WSU-EIT/FreeCRM

---

## Why this exists

* **AI outcomes depend on input quality.** Inconsistent copy/paste leads to missing context, token overflows, and weak answers.
* **Engineers waste time curating context.** Manual file gathering and prompt scaffolding is repetitive and error-prone.
* **Leaders need repeatability.** Teams should ask AI in a consistent, auditable way that scales across repos.

**FreeLLM standardizes the “prep” step** so your AI receives complete, right-sized context and clear objectives—every time.

---

## What it does (the Index page workflow)

> The **Index page** is the product. Everything else in this repo is supporting auth/infra.

1. **Point to a directory**
   Enter a local repo path. The app lists files while excluding noise (bin/obj/hidden).

2. **Curate the context**

   * Filter by extension (`.cs`, `.razor`, `.md`, custom types), wildcard (`*.cs`, `*/Views/*`), and plain-text match.
   * Ignore specific folders (e.g., `bootstrap`, `fontawesome`).
   * Sort by **name**, **line count**, or **character count**.
   * Select files for *full content*; leave others to contribute *metadata* only.

3. **Write intent once, reuse often**

   * Add top “Modification Instructions.”
   * Toggle common phrases/presets for consistent asks.
   * Optional bottom instructions (team or project-specific).

4. **Right-size for AI**

   * The app computes line/character counts and badges file sizes.
   * It auto-splits the final package into balanced **chunks** to respect LLM context limits.

5. **Export in one click**

   * Copy a clean, structured “Project Update” that includes:

     * Selected file contents
     * Unselected file metadata (so AI understands scope without flooding tokens)
     * Your instructions (top, bottom, and common phrases)
     * Chunk headers (if split)

---

## How it works (supporting APIs)

The Index page calls three secured endpoints (AppAdmin policy):

* **GetFiles** — enumerate files under a path, excluding `bin/obj` and hidden folders.

  * **Route:** `POST ~/{DataObjects.Endpoints.EndpointFileGpt.GetFiles}`
  * **Body:**

    ```json
    { "Path": "C:\\path\\to\\repo" }
    ```
  * **Response (sample):**

    ```json
    [
      { "FileName": "Program.cs", "FullPath": "C:\\repo\\Program.cs" },
      { "FileName": "App.razor",  "FullPath": "C:\\repo\\App.razor"  }
    ]
    ```

* **GetFileMetadata** — line/char counts for a list of files (used for *unselected* files).

  * **Route:** `POST ~/{DataObjects.Endpoints.EndpointFileGpt.GetFileMetadata}`
  * **Body:**

    ```json
    { "FilePaths": ["C:\\repo\\Program.cs", "C:\\repo\\App.razor"] }
    ```

* **GetFileContents** — full contents for a list of files (used for *selected* files).

  * **Route:** `POST ~/{DataObjects.Endpoints.EndpointFileGpt.GetFileContents}`
  * **Body:**

    ```json
    { "FilePaths": ["C:\\repo\\Program.cs"] }
    ```

**Implementation:** `FreeLLM/Controllers/DataController.App.FreeLLM.cs`
**Notes:** cached briefly (`IMemoryCache`), AppAdmin-gated, used exclusively by the Index page.

---

## Where things live (selected files)

```
FreeLLM/
├─ FreeLLM.Client/
│  ├─ Shared/AppComponents/
│  │  ├─ Index.App.razor            <-- Main UI (this is the product)
│  │  ├─ Settings.App.razor         (optional app settings UI stubs)
│  │  └─ About.App.razor            (about page content)
│  └─ Layout/MainLayout.razor       (SignalR, theme, auth bootstrapping)
│
├─ FreeLLM/
│  ├─ Components/App.razor          (shell: assets, scripts, head/body)
│  ├─ Controllers/
│  │  ├─ DataController.cs          (base controller/auth plumbing)
│  │  ├─ DataController.App.FreeLLM.cs  <-- FileGpt endpoints
│  │  └─ DataController.App.cs      (pattern for custom endpoints)
│  ├─ Classes/ConfigurationHelper.App.cs (app config customization points)
│  └─ ... (auth, SignalR hub, policies)
│
├─ FreeLLM.DataObjects/
│  └─ DataObjects.cs                (DTOs, enums, settings, responses)
│
└─ FreeLLM.EFModels/
   └─ EFModels/EFDataModel.cs       (EF entities & model mapping)
```

---

## Customization guide

### 1) “Check Defaults” selection (what gets pre-selected)

**Why customize:**
Speed up onboarding for a given repo/framework by auto-selecting the “must-have” context files (entry points, startup, DI wiring, data models, etc.). This reduces misses and creates consistent AI prompts across teams.

**Where:** `FreeLLM.Client/Shared/AppComponents/Index.App.razor`
**Method:** `CheckDefaults()`

```csharp
private async Task CheckDefaults()
{
    List<string> stringList = [
        "Readme.md", "Program.cs", "App.razor", "MainLayout.razor",
        "\\DataAccess.cs", "\\DataObjects.cs", "Controllers\\DataController.cs",
        "\\DataModel.cs", "\\EFDataModel.cs"
    ];

    var listCopy = fileItems.ToList();

    foreach (FileItem item in listCopy)
    {
        if (stringList.Any(o =>
            (string.Empty + item.FullPath).Trim().ToLower()
            .EndsWith(((string.Empty + o).ToLower().Trim()))))
        {
            item.IsSelected = true;
        }
    }

    fileItems = listCopy.ToList();
    await UpdateSummary();
    StateHasChanged();
}
```

**How to extend for your repo:**

* Add common core files for **.NET 9 + FreeCRM** (e.g., `Startup.cs` if present, `FreeLLM.*.cs`, domain service registrations, `appsettings.json`, module folders).
* Include conventional paths used in your org (e.g., `\Web\` or `\Services\`).
* Keep patterns short and stable (suffix‐based `EndsWith`) so they continue to match across machines.

**FreeCRM-aligned suggestions to add:**

```csharp
"Controllers\\DataController.App.FreeLLM.cs",
"FreeLLM.Client\\Shared\\AppComponents\\Index.App.razor",
"FreeLLM\\Components\\App.razor",
"FreeLLM.DataObjects\\DataObjects.cs",
"FreeLLM.EFModels\\EFModels\\EFDataModel.cs"
```

---

### 2) Default filters (file types, ignored folders, preselected types)

**Why customize:**
Different stacks need different defaults. For .NET 9 + Blazor you might bias toward `.cs`, `.razor`, `.md`; for full-stack scenarios you might pull in `.js`, `.css`, `.ts`, etc. Ignored folders keep the UI clean and prevent accidental token blow-ups.

**Where:** `FreeLLM.Client/Shared/AppComponents/Index.App.razor`
**Fields to adjust:**

```csharp
// Default list shown in the “File Type Filters” UI
private List<string> fileTypes = new()
{ ".cs", ".razor", ".cshtml", ".md", ".js", ".css", ".ts", ".json", ".html", ".xml", ".scss" };

// Types that start CHECKED
private HashSet<string> selectedFileTypes = new()
{ ".cs", ".razor", ".cshtml", ".md" };

// Folders hidden from the list by default
private List<string> ignoredFolders = new() { "fontawesome", "bootstrap" };

// Optional: a wildcard filter textbox exists; set an initial value if desired
private string filterText { get; set; } = ""; // e.g., "*.cs;*.razor" (if you add support for semicolon lists)
```

**Recommendations (FreeCRM / Blazor):**

* **Keep** `.cs`, `.razor`, `.md` preselected.
* **Consider adding** `.json` (e.g., `appsettings.json`), `.csproj` (project references), `.svg` if your component library relies on them.
* **Extend ignores** with `node_modules`, `dist`, `wwwroot/lib`, `obj`, `bin`, `coverage`, `artifacts`.
* If you want `.cshtml` off by default for pure Blazor repos, remove it from `selectedFileTypes`.

---

### 3) Sorting defaults and badges

**Why customize:**
For larger repos, **LineCount** or **CharCount** default sorting surfaces “heavy” files first, helping authors prune or chunk earlier.

**Where:** `Index.App.razor`

```csharp
// Defaults
private string _sortOption = "Name";   // "Name" | "LineCount" | "CharCount"
private string _sortDirection = "asc"; // "asc" | "desc"
```

**Badges (size coloring):**

```csharp
// Thresholds used to color badge backgrounds by line count
// Adjust to your prompt/token budget or LLM model limits
if (lineCount < 1000)  success;
else if (lineCount < 2000) info;
else if (lineCount < 3000) warning;
else danger;
```

---

### 4) Chunking behavior

**Why customize:**
Different LLMs / gateways have different safe context windows. Adjusting chunk strategy keeps each payload comfortably within limits.

**Where:** `Index.App.razor` → `SplitSummaryIntoChunks(string summary)`

* The method groups the preamble + each file block and greedily packs them into balanced chunks by **line count**.
* You can change the target heuristic (lines → characters) or implement a token estimator if you have one.

---

## Getting started (dev)

1. **Clone & open** the solution in Visual Studio / Rider.
2. **.NET 9 SDK** installed; run Server + Client.
3. Ensure your account has **AppAdmin** to call file endpoints.
4. Navigate to the app, open the **Index** page, and enter a repo path (e.g., `C:\src\your-solution`).
5. Select files, write instructions, choose chunk count, and **Copy** the prepared AI package.

> The framework includes maintenance mode, multi-tenant settings, languages, and SignalR notifications. These exist to support enterprise deployment but are not required for understanding or using the Index workflow.

---

## Security & controls

* **Access control:** File operations are AppAdmin-gated.
* **Least privilege:** Endpoint authorization is explicit; no anonymous file reads.
* **Noisy paths filtered:** Hidden directories and `bin/obj` are excluded.
* **Token discipline:** Pre-flight line/char counts + chunking reduce overflows.

---

## Roadmap (short list)

* Prompt presets by repo/domain (reusable instruction packs).
* Export AI outputs/diffs back into the app (optional).
* Usage analytics (time saved, prompts/week, iteration count).
* Enterprise SSO/RBAC hardening and audit trail.

---

## FAQ

**Is this an “AI code generator”?**
No. FreeLLM *prepares* clean, right-sized context and intent for your chosen AI (LLM). It doesn’t replace your LLM; it makes it effective.

**Can we use it with any LLM?**
Yes—copy the generated “Project Update” into ChatGPT, Azure OpenAI, or your preferred tool.

**What about huge repos?**
Use filters, folder ignores, and chunking. Unselected files still contribute metadata so AI grasps scope without blowing tokens.

---

## License

Open source. See `LICENSE` (or your org’s standard OSS notice).

---

### Quick reference (endpoints & sample bodies)

```http
POST ~/{DataObjects.Endpoints.EndpointFileGpt.GetFiles}
Content-Type: application/json
{ "Path": "C:\\path\\to\\repo" }

POST ~/{DataObjects.Endpoints.EndpointFileGpt.GetFileMetadata}
Content-Type: application/json
{ "FilePaths": ["C:\\repo\\Program.cs", "C:\\repo\\App.razor"] }

POST ~/{DataObjects.Endpoints.EndpointFileGpt.GetFileContents}
Content-Type: application/json
{ "FilePaths": ["C:\\repo\\Program.cs"] }
```

**Core UI:** `FreeLLM.Client/Shared/AppComponents/Index.App.razor`
**API implementation:** `FreeLLM/Controllers/DataController.App.FreeLLM.cs`

**Customization hotspots:**

* *Check Defaults:* `Index.App.razor` → `CheckDefaults()`
* *Default filters:* `Index.App.razor` → `fileTypes`, `selectedFileTypes`, `ignoredFolders`, `filterText`
* *Chunking logic:* `Index.App.razor` → `SplitSummaryIntoChunks()`

**Integration target:** **FreeCRM** (.NET 9): github.com/WSU-EIT/FreeCRM/

**Bottom line:** FreeLLM turns *prompt prep* into a reliable, fast, and repeatable step—so your AI produces better engineering output with less rework.
