# 🧩 **1. Agent List**

| Agent Name               | Responsibility                                                           | Input                     | Output                                    |
| ------------------------ | ------------------------------------------------------------------------ | ------------------------- | ----------------------------------------- |
| **PluginArchitect**      | Creates plugin base architecture and folder structure                    | Requirements              | Full plugin skeleton                      |
| **CodeEngineer**         | Generates production-ready C# code files                                 | File names + requirements | C# classes, interfaces, configurations    |
| **DatabaseEngineer**     | Generates migrations, entity builders, removes entity mappings           | Entity definitions        | FluentBuilder + Migration classes         |
| **FrontendEngineer**     | Creates CSHTML, JS, CSS, UI components                                   | UI/UX requirements        | Razor views + components                  |
| **ConfigEngineer**       | Generates plugin settings, config models, admin configuration pages      | Fields + settings         | Settings + ViewModels + Admin UI          |
| **ApiEngineer**          | Creates controllers, endpoints, DTOs                                     | API specification         | API Controllers + Request/Response Models |
| **ServiceEngineer**      | Writes business logic layer (CRUD, filtering, domain rules)              | Use cases                 | Services + Interfaces                     |
| **ModelFactoryEngineer** | Prepares ViewModels **for both Admin & Public**, including ListModels, SearchModels, dropdowns, grids, localized enums | Entities + Services       | ModelFactories + ViewModels               |
| **TestCaseEngineer**     | Generates sanity, functional, and regression test cases                  | Plugin features           | Excel/Markdown test suites                |
| **DocumentEngineer**     | Generates documentation (README, user manual, release notes)             | Features                  | HTML/Markdown documentation               |
| **ReleaseEngineer**      | Builds plugin packages, manifest files, zip artifacts                    | Version + output path     | Zipped plugin packages                    |


---

# 🛠️ **2. Agent Details**

## **2.1 PluginArchitect**

Responsible for:

* Plugin folder & file structure
* `plugin.json` template (standard)
* Admin menu registration
* Dependency injection skeleton
* route provider & permissions

Output includes:

```
/NopStation.Plugin.{Group}.{Name}
├─ Areas
│   └─ Admin
│       ├─ Controllers
│       ├─ Factories
│       ├─ Infrastructure
│       │   └─ MapperConfiguration.cs
│       ├─ Models
│       ├─ Validators
│       └─ Views
│         ├─ _ViewImports.cshtml
│         └─ _ViewStart.cshtml
├─ Components
├─ Controllers
├─ Data
│   ├─ Builders
│   └─ Migrations
├─ Domain
├─ Infrastructure
│   └─ PluginNopStartup.cs
├─ Models
├─ Services
├─ Views
│   └─ _ViewImports.cshtml
├─ AdminMenuCreatedEventConsumer.cs
├─ plugin.json
├─ {Name}PermissionProvider.cs
├─ {Name}Settings.cs
└─ {Name}Plugin.cs
```

## **2.2 CodeEngineer**

Writes:

* Domain models
* Interfaces & implementations
* Helpers/Factories
* Extension methods
* Settings loaders
* Background tasks

Constraints:

* Must follow NopStation naming conventions
* Should not duplicate code
* Should use `IWorkContext`, `ILocalizationService`, `INotificationService`, etc.

## **2.3 DatabaseEngineer**

Responsible for:

* Fluent Migrator Migrations
* Entity Builders
* BaseNameCompatibility
* TableFor<T>() usage
* One-to-one/one-to-many mapping
* Unique constraints, indexes

### **2.3.1 BaseNameCompatibility Mapping**

Define `BaseNameCompatibility : INameCompatibility` in data layer to align nopCommerce tables with the legacy NopStation naming scheme when required. Use the mapping pattern below for table name overrides (columns dictionary can remain empty if no remaps are needed). Keep the example generic so it can be copied into any plugin without renaming product-specific entities first:

```csharp
public class BaseNameCompatibility : INameCompatibility
{
    public Dictionary<Type, string> TableNames => new()
    {
        { typeof(<Domain>), "NS_<Plugin_Abbreviation>_<Domain>" }
    };

    public Dictionary<(Type, string), string> ColumnName => new Dictionary<(Type, string), string>
    {
    };
}
```

SchemaMigration Example output:

```csharp
[NopMigration("2025-12-01 00:00:00", "{PluginName} base schema")]
public class SchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        if (!Schema.Table<<Domain>>().Exists())
            Create.TableFor<<Domain>>();
    }
}
```

## **2.4 FrontendEngineer**

Delivers:

* Admin configuration UI
* Public widget views
* ViewComponent structures
* JS/CSS assets
* AJAX calls

Standards:

* Use `_AdminLayout.cshtml`
* Try not to use inline JS
* Use localizable resources

## **2.5 ConfigEngineer**

Generates:

* `PluginSettings`
* Admin models
* Mapping to/from settings
* Configuration page with Save() action

Property ordering (configuration models with store overrides):

* Each display-named property must be immediately followed by its corresponding `{Property}_OverrideForStore` flag.
Sample setting:

```csharp
public class MyPluginSettings : ISettings
{
    public string ApiKey { get; set; }
    public bool EnableLogging { get; set; }
}
```

Place the configuration model immediately after the sample setting, keeping each override next to its display-named property:

```csharp
public partial record ConfigurationModel : BaseNopModel, ISettingsModel
{
    [NopResourceDisplayName("Admin.NopStation.{Plugin}.Configuration.Fields.ApiKey")]
    public string ApiKey { get; set; }
    public bool ApiKey_OverrideForStore { get; set; }

    [NopResourceDisplayName("Admin.NopStation.{Plugin}.Configuration.Fields.EnableLogging")]
    public bool EnableLogging { get; set; }
    public bool EnableLogging_OverrideForStore { get; set; }

    public int ActiveStoreScopeConfiguration { get; set; }
}
```

## **2.6 ServiceEngineer**

Responsible for:

* Core plugin logic
* Cron jobs
* Sync jobs
* Data processing
* External API integration

Must follow:

* Async-first pattern
* Logging via `Nop.Services.Logging.ILogger`
* Exceptions wrapped to user-friendly messages


## **2.7 TestCaseEngineer**

Generates:

* Sanity test cases
* Functional test suite
* Regression test plan
* Negative cases
* Edge cases

Export formats: `.xlsx`, `.csv`, `.md`

Standards:

* Case ID format: `PLUGINNAME_TC_###`
* Steps must be atomic
* Expected result must be measurable

## **2.8 DocumentEngineer**

Responsible for ALL plugin documentation and product descriptions.

This includes:

* Proper heading structure
* Feature blocks
* Installation guide
* Configuration guide
* Screenshots placeholders
* FAQs
* Troubleshooting guide
* Version history

**2.8.1. Main Documentation File (HTML)**

**Path:** `/files/NopStation.Plugin.{Group}.{Name}/doc.html`

Includes:

* Overview
* Features
* Requirements
* Installation
* Configuration (Admin UI + Storefront behavior)
* How it works
* Troubleshooting
* FAQs
* Version history

**2.8.2. Release Notes**

**Path:** `/files/NopStation.Plugin.{Group}.{Name}/release_note.txt`

Format:

```
Version: 4.90.1.0
Release date: 2025-01-01

- Feature list...
- Bug fixes...
- Performance improvements...
```

**2.8.3. Nop-Station Product Description (HTML)**

**Path:** `/files/NopStation.Plugin.{Group}.{Name}/nop-station-description.html`
 
Style must follow:
🔗 [https://www.nop-station.com/downtown-theme-for-nopcommerce](https://www.nop-station.com/downtown-theme-for-nopcommerce)

Sections:

* Overview
* Plugin highlights
* Key features
* Why choose NopStation
* SEO-rich description
* Compatible versions
* Screenshots placeholders

**2.8.4. nopCommerce Marketplace Description (HTML)**

**Path:** `/files/NopStation.Plugin.{Group}.{Name}/nopcommerce-description.html`

Style reference:
🔗 [https://www.nopcommerce.com/en/downtown-theme-plugin-bundle-by-nopstation](https://www.nopcommerce.com/en/downtown-theme-plugin-bundle-by-nopstation)

Simplified format:

* Short description
* Key features
* Supported versions
* About NopStation
* Support details

---

# 🧪 **3. Agent Execution Workflow**

```
PluginArchitect → 
CodeEngineer → 
DatabaseEngineer → 
ServiceEngineer → 
ApiEngineer → 
FrontendEngineer → 
ConfigEngineer → 
DocumentEngineer → 
TestCaseEngineer → 
ReleaseEngineer
```

Each agent completes its work before handing off.

--- 

# 📚 **4. Naming Conventions**

| Item             | Rule                                     |
| ---------------- | ---------------------------------------- |
| Plugin Namespace | `NopStation.Plugin.{Group}.{SystemName}` |
| SystemName       | No spaces, PascalCase                    |
| Entities         | Singular name                            |
| Migrations       | `YYYY-MM-DD HH:mm:ss` format             |
| Permissions      | `PublicPortal.Manage` style              |
| ViewComponents   | Must end with `ViewComponent`            |

---

# 🧵 **5. `plugin.json` Template**

```json
{
  "Group": "Nop-Station",
  "FriendlyName": "Your Plugin Name",
  "SystemName": "NopStation.Plugin.{Group}.{Name}",
  "Version": "4.90.1.0",
  "SupportedVersions": [ "4.90" ],
  "Author": "Nop-Station Team",
  "DisplayOrder": 1,
  "FileName": "NopStation.Plugin.{Group}.{Name}.dll",
  "Description": "Description here...",
  "DependsOnSystemNames": [ "NopStation.Core" ]
}
```

---

# 🧭 **6. Implementation Rules**

* Do not use Entity Framework for ORM; linq2db is already available for data access.
* Do not use raw SQL in services; rely on abstractions or repositories built over linq2db.
* Do not use database transactions.
* Keep controllers minimal—no business logic should reside in controllers.
* Always use model validators for input handling.
* Include localization resources in `GetPluginResources()` following NopStation resource key formatting.
* Admin controller formatting rules:
  * `{PluginName}Controller.cs` must contain a `Configure` method handling settings with multi-store support.
  * Additional domain controllers must expose `List (GET)`, `List (POST)`, `Create (GET)`, `Create (POST)`, `Edit (GET)`, `Edit (POST)`, and `Delete` actions as appropriate.
* Model factories (admin and public) must provide mapping helpers for creating/editing view models and use localized properties when applicable.
* Validators (admin and public) should enforce required fields, length constraints, and culture-aware rules, and should register through dependency injection.
* Views (admin and public) should avoid inline styles/scripts, rely on shared layouts, and ensure validation/localization helpers are wired.
* **Do not add unnecessary comments** in code:
  * Avoid XML documentation comments (`/// <summary>`) on properties, methods, or classes unless the code is part of a public API library.
  * Do not use inline grouping comments (e.g., `// Configuration`, `// Menu items`).
  * Do not include placeholder or example comments (e.g., `// Services will be registered here`, `// Example: services.AddScoped<>`).
  * Code should be self-documenting through clear naming and structure.

---
# 🗂️ **7. File-Level Code Patterns & Return Types**

Follow these file-specific patterns to stay aligned with existing NopStation plugins:

## Controllers (Admin)

* Use async signatures that return `Task<IActionResult>` for all action methods.
* Action return types:
  * `List (GET)`: returns a view with the initialized search model (e.g., `View(model)`).
  * `List (POST)`: returns a JSON data source (`Json(model)` or `return await ...` with `DataTables`) for grids.
  * `Create (GET)`: returns a view prepared for creation (often reuses `_CreateOrUpdate`).
  * `Create (POST)`: returns the create view when model state is invalid; redirects to `List` (or `Edit` when `continueEditing`) on success.
  * `Edit (GET)`: returns a view for edit populated via factory-prepared model.
  * `Edit (POST)`: returns the edit view when model state is invalid; redirects to `List` (or `Edit` when `continueEditing`) on success.
  * `Delete`: returns a redirect to `List` (or JSON result for AJAX grids) after deletion.
* When an edit surface manages multiple related items (e.g., a parent entity with child rows/values), provide AJAX endpoints for the child grid (list/create/update/delete) instead of posting the full page; the view should call these endpoints without a reload.
* Use `[AutoValidateAntiforgeryToken]` on POST actions and decorate admin endpoints with `[CheckPermission(...)]` instead of manually calling `_permissionService.AuthorizeAsync` inside action bodies.
* Keep controllers thin: invoke services and factories; perform no domain calculations.

## Model Factories

* **Admin factories** should expose:
  * `Prepare<Domain><Search/List>ModelAsync` for grid/search initialization and paging defaults.
  * `Prepare<Domain>ModelAsync(model, entity, excludeProperties = false)` to populate view models for create/edit, honoring localized properties and store mappings.
  * `Prepare<Related>ModelsAsync` helpers for dropdowns, store lists, ACL roles, and multi-store bindings.
* **Public factories** should provide mappers that translate domain entities or settings into public-facing view models, including SEO/localized fields.
* Factories should never save data; they only prepare models using injected services.

## Services

* Services handle business logic and persistence via linq2db repositories or abstractions.
* Expose async CRUD methods (`Insert<Domain>Async`, `Update<Domain>Async`, `Delete<Domain>Async`, `Get<Domain>ByIdAsync`, `GetAll<DomainPlural>Async` with paging) and search helpers where needed.
* Validation occurs before calling services; services should guard invariants and return domain entities or paged lists.

## Views (Admin)

* `List.cshtml`: uses `_AdminLayout.cshtml`, renders grid/search form via tag helpers (`nop-grid`, `nop-column`, etc.), and posts to `List (POST)` endpoint.
* `Create.cshtml` / `Edit.cshtml`: load the `_CreateOrUpdate` partial with the model and render secondary buttons (`Save`, `Save and Continue`, `Delete`).
* `_CreateOrUpdate.cshtml`: orchestrates cards and partial sections. Use `nop-card` with `asp-name`, `asp-icon`, `asp-title`, `asp-hide-block-attribute-name`, `asp-hide` flags. Include partials for logical sections such as:
  * `_CreateOrUpdate.Info.cshtml` for core fields.
  * `_CreateOrUpdate.Seo.cshtml` for SEO fields when applicable.
  * `_CreateOrUpdate.[Section].cshtml` (e.g., `Properties`, `Mapping`, `Stores`, `ACL`) for additional domain-specific sections.
* Avoid inline scripts/styles; reference bundled JS/CSS under `wwwroot` and use `asp-location="Footer"` for script includes.

## Views (Public)

* Use shared layouts/theme components; keep scripts external and localized via `T()` helpers.
* Respect caching/output settings defined by the plugin and expose model validation summaries where needed.

## Validators

* Use `BaseNopValidator<TModel>` implementations per view model with rules for required fields, lengths, numeric ranges, and localized properties.

## Resources

* Define all resource keys in `GetPluginResources()` using `Admin.NopStation.{Plugin}.{Section}.{Name}` or `Plugins.NopStation.{Plugin}.{PublicArea}` formats; include keys for UI labels, hints, validation messages, success/error notifications, tab titles, and breadcrumbs.

## File Formats Reference

* Match patterns already present in existing NopStation plugins within this repository (controller/action shapes, factory methods, partial view structures, localization resource layout, and validator registrations). When unsure, mirror the prevalent format found under `src/Plugins/NopStation.Plugin.*` for comparable features.

Great — I will now **append the fully generic Section 12** (with NO domain-specific names) to the end of your existing `AGENTS.md` file.

Here is the **final merged output** containing your entire original file + the new **Section 12 (Generic File Code Formats)** exactly appended at the end.

---

# 📘 **8. `.cshtml` File Code Formats (Generic Templates for Any Plugin Domain)**

This section defines **universal patterns** for Razor views, validators, and model factories across **all NopStation plugins**.
Every template is **fully domain-neutral**, ensuring clean reuse for ANY entity.

Each subsection includes:  
✔ Key design rules  
✔ Required patterns  
✔ Implementation guidelines  
✔ Generic code templates  

## **8.1 FrontendEngineer – Razor View File Formats (Generic)**

All Razor pages must follow NopStation UI standards:

### ✅ **Key Patterns**

* Must set **Page Title** and **Active Menu Item**.
* Must use **cards layout** (`nop-cards`, `card card-default`).
* Must load grid via **DataTablesModel** for consistency.
* Must keep UI consistent across Create/Edit views by using `_CreateOrUpdate` partial.
* Must ensure resource keys follow:
  **Admin.{Plugin}.{DomainPlural}.Fields.***
* Try to avoid inline JS except for small toggle logic.

### **8.1.1 `List.cshtml` Format (Admin Grid Page – Generic)**

#### ✔ Keypoints of This Pattern

* Always prepare the search model and set page title/active menu.
* Render an **Add New** button in the header.
* Include a collapsible **card-search** block **only when filters are present**; otherwise omit the search card entirely. Persist the toggle state with a hide attribute key when present.
* Keep the search button ID identical to `DataTablesModel.SearchButtonId` so the grid reloads correctly.
* Declare grid filters with `nameof(Model.<SearchField>)` using **generic field names** that reflect the search model properties.

```cshtml
@model <Domain>SearchModel

@{
    ViewBag.PageTitle = T("Admin.{Plugin}.{DomainPlural}").Text;
    NopHtml.SetActiveMenuItemSystemName("{DomainPlural}");

    const string hideSearchBlockAttributeName = "{Domain}ListPage.HideSearchBlock";
    var hideSearchBlock = await genericAttributeService.GetAttributeAsync<bool>(await workContext.GetCurrentCustomerAsync(), hideSearchBlockAttributeName);
}

<form asp-controller="{ControllerName}" asp-action="List" method="post">
    <div class="content-header clearfix">
        <h1 class="float-left">@T("Admin.{Plugin}.{DomainPlural}")</h1>
        <div class="float-right">
            <a asp-action="Create" class="btn btn-primary">
                <i class="fas fa-square-plus"></i>
                @T("Admin.Common.AddNew")
            </a>
        </div>
    </div>

    <section class="content">
        <div class="container-fluid">
            <div class="form-horizontal">
                <div class="cards-group">

                    <!-- Optional search block: include only when the page exposes filters -->
                    <div class="card card-default card-search">
                        <div class="card-body">
                            <div class="row search-row @(!hideSearchBlock ? "opened" : string.Empty)" data-hideattribute="@hideSearchBlockAttributeName">
                                <div class="search-text">@T("Admin.Common.Search")</div>
                                <div class="icon-search"><i class="fas fa-magnifying-glass" aria-hidden="true"></i></div>
                                <div class="icon-collapse"><i class="far fa-angle-@(!hideSearchBlock ? "up" : "down")" aria-hidden="true"></i></div>
                            </div>
                            <div class="search-body @(hideSearchBlock ? "closed" : string.Empty)">
                                <div class="row">
                                    <div class="col-md-5">
                                        <div class="form-group row">
                                            <div class="col-md-4"><nop-label asp-for="SearchFieldOne" /></div>
                                            <div class="col-md-8"><nop-editor asp-for="SearchFieldOne" /></div>
                                        </div>
                                    </div>
                                    <div class="col-md-5">
                                        <div class="form-group row">
                                            <div class="col-md-4"><nop-label asp-for="SearchFieldTwo" /></div>
                                            <div class="col-md-8">
                                                <nop-select asp-for="SearchFieldTwo" asp-items="Model.AvailableFieldTwoOptions" />
                                            </div>
                                        </div>
                                    </div>
                                </div>
                                <div class="row">
                                    <div class="text-center col-12">
                                        <button type="button" id="search-<domain>" class="btn btn-primary btn-search">
                                            <i class="fas fa-magnifying-glass"></i>
                                            @T("Admin.Common.Search")
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="card card-default">
                        <div class="card-body">
                            @await Html.PartialAsync("Table", new DataTablesModel
                            {
                                Name = "<domain>-grid",
                                UrlRead = new DataUrl("List", "{ControllerName}", null),
                                SearchButtonId = "search-<domain>",
                                Length = Model.PageSize,
                                LengthMenu = Model.AvailablePageSizes,
                                Filters = new List<FilterParameter>
                                {
                                    new(nameof(Model.SearchFieldOne)),
                                    new(nameof(Model.SearchFieldTwo))
                                },
                                ColumnCollection = new List<ColumnProperty>
                                {
                                    new ColumnProperty(nameof(<Domain>Model.Name))
                                    {
                                        Title = T("Admin.{Plugin}.{DomainPlural}.Fields.Name").Text
                                    },
                                    new ColumnProperty(nameof(<Domain>Model.SystemName))
                                    {
                                        Title = T("Admin.{Plugin}.{DomainPlural}.Fields.SystemName").Text
                                    },
                                    new ColumnProperty(nameof(<Domain>Model.Id))
                                    {
                                        Title = T("Admin.Common.Edit").Text,
                                        Width = "100",
                                        ClassName = NopColumnClassDefaults.Button,
                                        Render = new RenderButtonEdit(new DataUrl("Edit"))
                                    }
                                }
                            })
                        </div>
                    </div>

                </div>
            </div>
        </div>
    </section>
</form>

<script asp-location="Footer">
    $(document).ready(function () {
        $('#search-<domain>').on('click', function () {
            $('#<domain>-grid').DataTable().ajax.reload();
            return false;
        });
    });
</script>
```

### **8.1.2 `Create.cshtml` Format (Generic)**

#### ✔ Keypoints of This Pattern

* Must use **same layout** as Edit.cshtml.
* Uses `_CreateOrUpdate` partial for consistent form design.
* Provides two primary actions:

  * **Save**
  * **Save & Continue**
* Includes **Back to List** for navigation consistency.

```cshtml
@model <Domain>Model

@{
    ViewBag.PageTitle = T("Admin.{Plugin}.{DomainPlural}.AddNew").Text;
    NopHtml.SetActiveMenuItemSystemName("{DomainPlural}");
}

<form asp-controller="{ControllerName}" asp-action="Create" method="post">
    <div class="content-header clearfix">
        <h1 class="float-left">
            @T("Admin.{Plugin}.{DomainPlural}.AddNew")
            <small>
                <i class="fas fa-arrow-circle-left"></i>
                <a asp-action="List">@T("Admin.{Plugin}.{DomainPlural}.BackToList")</a>
            </small>
        </h1>

        <div class="float-right">
            <button type="submit" name="save" class="btn btn-primary">
                <i class="far fa-save"></i>@T("Admin.Common.Save")
            </button>
            <button type="submit" name="save-continue" class="btn btn-primary">
                <i class="far fa-save"></i>@T("Admin.Common.SaveContinue")
            </button>
        </div>
    </div>

    @await Html.PartialAsync("_CreateOrUpdate", Model)
</form>
```

### **8.1.3 `Edit.cshtml` Format (Generic)**

#### ✔ Keypoints of This Pattern

* Must look identical to Create.cshtml for UX consistency.
* Adds **Delete button + Delete confirmation popup**.
* Uses `Save` / `Save and Continue Editing`.

```cshtml
@model <Domain>Model

@{
    ViewBag.PageTitle = T("Admin.{Plugin}.{DomainPlural}.EditDetails").Text;
    NopHtml.SetActiveMenuItemSystemName("{DomainPlural}");
}

<form asp-controller="{ControllerName}" asp-action="Edit" method="post">
    <div class="content-header clearfix">
        <h1 class="float-left">
            @T("Admin.{Plugin}.{DomainPlural}.EditDetails")
            <small>
                <i class="fas fa-arrow-circle-left"></i>
                <a asp-action="List">@T("Admin.{Plugin}.{DomainPlural}.BackToList")</a>
            </small>
        </h1>

        <div class="float-right">
            <button type="submit" name="save" class="btn btn-primary">
                <i class="far fa-save"></i>@T("Admin.Common.Save")
            </button>

            <button type="submit" name="save-continue" class="btn btn-primary">
                <i class="far fa-save"></i>@T("Admin.Common.SaveContinue")
            </button>

            <span id="<domain>-delete" class="btn btn-danger">
                <i class="far fa-trash-alt"></i>@T("Admin.Common.Delete")
            </span>
        </div>
    </div>

    @await Html.PartialAsync("_CreateOrUpdate", Model)
</form>

<nop-delete-confirmation asp-model-id="@Model.Id" asp-button-id="<domain>-delete" />
```

### **8.1.4 `_CreateOrUpdate.cshtml` Format (Generic)**

#### ✔ Keypoints of This Pattern

* Defines entire “edit surface” inside card-based UI.
* Dynamically hides or shows sections based on model properties.
* Uses:

  * `{DomainPage.HideInfoBlock}`
  * `{DomainPage.HideValuesBlock}`
* Allows adding unlimited sections through cards.
* When the edit page includes a collection of related items (e.g., values/rows), render them in a dedicated card and handle add/update/delete via AJAX (child grid or modal) instead of posting the full form.

```cshtml
@model <Domain>Model

<div asp-validation-summary="All"></div>
<input asp-for="Id" type="hidden" />

<section class="content">
    <div class="container-fluid">
        <div class="form-horizontal">

            <nop-cards id="<domain>-panels">

                <nop-card asp-name="<domain>-info"
                          asp-icon="fas fa-info"
                          asp-title="@T("Admin.{Plugin}.{DomainPlural}.Info")"
                          asp-hide-block-attribute-name="{DomainPage.HideInfoBlock}"
                          asp-hide="@hideInfoBlock">
                    @await Html.PartialAsync("_CreateOrUpdate.Info", Model)
                </nop-card>

                @if (Model.ShowValueSection)
                {
                    <nop-card asp-name="<domain>-values"
                              asp-icon="fa fa-list"
                              asp-title="@T("Admin.{Plugin}.{DomainPlural}.Values")"
                              asp-hide-block-attribute-name="{DomainPage.HideValuesBlock}"
                              asp-hide="@hideValuesBlock">
                        @await Html.PartialAsync("_CreateOrUpdate.Values", Model)
                    </nop-card>
                }

            </nop-cards>

        </div>
    </div>
</section>
```

### **8.1.5 `_CreateOrUpdate.Info.cshtml` Format (Generic)**

#### ✔ Keypoints of This Pattern

* Displays core fields of any entity.
* Uses `nop-label`, `nop-editor`, and `nop-select` helpers.
* Includes minimal inline JS only for **dynamic UI toggling** (allowed rule).
* Uses standardized form layout (3/9 grid).

```cshtml
@model <Domain>Model

<div class="card-body">

    <div class="form-group row">
        <div class="col-md-3"><nop-label asp-for="Name" /></div>
        <div class="col-md-9">
            <nop-editor asp-for="Name" asp-required="true" />
            <span asp-validation-for="Name"></span>
        </div>
    </div>

    <div class="form-group row">
        <div class="col-md-3"><nop-label asp-for="SystemName" /></div>
        <div class="col-md-9">
            <nop-editor asp-for="SystemName" asp-required="true" />
            <span asp-validation-for="SystemName"></span>
        </div>
    </div>

    <div class="form-group row">
        <div class="col-md-3"><nop-label asp-for="TypeId" /></div>
        <div class="col-md-9">
            <nop-select asp-for="TypeId"
                        asp-items="@Model.AvailableTypes"
                        onchange="ToggleSections()" />
            <span asp-validation-for="TypeId"></span>

            <script>
                function ToggleSections() {
                    var v = $("#TypeId").val();
                    if (v == '7' || v == '8')
                        $("#<domain>-values").hide();
                    else
                        $("#<domain>-values").show();
                }
            </script>
        </div>
    </div>

</div>
```

### **8.1.6 `_CreateOrUpdate.[Section].cshtml` for Child Collections (Values/Mappings/etc.)**

#### ✔ Keypoints of This Pattern

* Use a **dedicated partial** (for example `_CreateOrUpdate.Values.cshtml`, `_CreateOrUpdate.Mappings.cshtml`) to host child rows tied to the parent model.
* Child items are **managed via AJAX** (grid + popup) instead of full form posts. Add/edit operations open modal/pop-up windows and refresh the grid on close.
* Keep the card wrapper in `_CreateOrUpdate.cshtml`; the section partial should render only its card body content.
* Use grid columns that expose an **Edit** button wired to open the edit popup for the selected child item.
* Ensure the popup forms post back to popup endpoints that return JSON `{success:true}` and call `refreshGrid` in the opener to reload the list.

```cshtml
@model <Child>SearchModel

<div class="card-body">
    <div class="row mb-3">
        <div class="text-right col-12">
            <button type="button"
                    class="btn btn-primary"
                    onclick="OpenWindow('@Url.Action("ChildCreatePopup", new { parentId = Model.ParentId, btnId = "btnRefreshChildren", formId = "child-popup-form" })', 800, 700, true); return false;">
                <i class="fas fa-plus-square"></i>
                @T("Admin.Common.AddNew")
            </button>
            <button type="submit" id="btnRefreshChildren" class="d-none"></button>
        </div>
    </div>

    <div class="table-responsive">
        @await Html.PartialAsync("Table", new DataTablesModel
        {
            Name = "children-grid",
            UrlRead = new DataUrl("ChildList", "{ControllerName}", new RouteValueDictionary { ["parentId"] = Model.ParentId }),
            SearchButtonId = "btnRefreshChildren",
            Length = Model.PageSize,
            LengthMenu = Model.AvailablePageSizes,
            Filters = new List<FilterParameter>
            {
                new(nameof(Model.ParentId))
            },
            ColumnCollection = new List<ColumnProperty>
            {
                new ColumnProperty(nameof(<ChildModel>.Name))
                {
                    Title = T("Admin.{Plugin}.{DomainPlural}.Child.Fields.Name").Text
                },
                new ColumnProperty(nameof(<ChildModel>.Id))
                {
                    Title = T("Admin.Common.Edit").Text,
                    Width = "80",
                    ClassName = NopColumnClassDefaults.Button,
                    Render = new RenderButtonCustom("@Url.Action("ChildEditPopup")?id=", "OpenWindow('{0}', 800, 700, true); return false;")
                }
            }
        })
    </div>
</div>

<script asp-location="Footer">
    function refreshGrid() {
        $('#children-grid').DataTable().ajax.reload();
    }
</script>
```

### **8.1.7 `Configure.cshtml` Format (Admin Settings Page)**

#### ✔ Keypoints of This Pattern

* Sets page title and active menu system name at the top.
* Includes `StoreScopeConfigurationViewComponent` before the cards to surface multi-store override checkboxes.
* Shows `asp-validation-summary="All"` before rendering settings fields.
* Wraps settings inside `cards-group` → `card card-default` → `card-body` with `form-group` rows.
* Uses `name="save"` on the Save button and places it in the content header’s right section.

```cshtml
@using Nop.Web.Areas.Admin.Components
@model <ConfigurationModel>

@{
    ViewBag.PageTitle = T("Admin.{Plugin}.Configuration").Text;
    NopHtml.SetActiveMenuItemSystemName("{MenuSystemName}");
}

<form asp-controller="{ControllerName}" asp-action="Configure" method="post">
    <div class="content-header clearfix">
        <h1 class="float-left">
            @T("Admin.{Plugin}.Configuration")
        </h1>
        <div class="float-right">
            <button type="submit" name="save" class="btn btn-primary">
                <i class="far fa-save"></i>
                @T("Admin.Common.Save")
            </button>
        </div>
    </div>

    <section class="content">
        <div class="container-fluid">
            <div class="form-horizontal">
                @await Component.InvokeAsync(typeof(StoreScopeConfigurationViewComponent))
                <div asp-validation-summary="All"></div>
                <div class="cards-group">
                    <div class="card card-default">
                        <div class="card-body">
                            <div class="form-group row">
                                <div class="col-md-3">
                                    <nop-override-store-checkbox asp-for="<SettingA>_OverrideForStore" asp-input="<SettingA>" asp-store-scope="Model.ActiveStoreScopeConfiguration" />
                                    <nop-label asp-for="<SettingA>" />
                                </div>
                                <div class="col-md-9">
                                    <nop-editor asp-for="<SettingA>" />
                                    <span asp-validation-for="<SettingA>"></span>
                                </div>
                            </div>
                            <div class="form-group row">
                                <div class="col-md-3">
                                    <nop-override-store-checkbox asp-for="<SettingB>_OverrideForStore" asp-input="<SettingB>" asp-store-scope="Model.ActiveStoreScopeConfiguration" />
                                    <nop-label asp-for="<SettingB>" />
                                </div>
                                <div class="col-md-9">
                                    <nop-editor asp-for="<SettingB>" />
                                    <span asp-validation-for="<SettingB>"></span>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </section>
</form>
```

## **8.2 Validator Format (Generic)**

### ✔ Keypoints of This Pattern

* Ensures required fields comply with business rules.
* Must extend `BaseNopValidator<TModel>`.
* Must use `ILocalizationService` for all error messages.
* Uses NopCommerce FluentValidation standard.

```csharp
public class <Domain>Validator : BaseNopValidator<<Domain>Model>
{
    public <Domain>Validator(ILocalizationService localizationService)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(localizationService.GetResourceAsync("Admin.{Plugin}.{DomainPlural}.Fields.Name.Required").Result);

        RuleFor(x => x.SystemName)
            .NotEmpty()
            .WithMessage(localizationService.GetResourceAsync("Admin.{Plugin}.{DomainPlural}.Fields.SystemName.Required").Result);
    }
}
```

## **8.3 ModelFactory Format (Generic)**

### ✔ Keypoints of This Pattern

* Prepares all models for Razor views.
* Handles:

  * Dropdown loading
  * Derived flags (e.g., IsTextType)
  * Pagination
* Never writes to database (read-only).
* Converts Domain → ViewModel.

```csharp
public class <Domain>ModelFactory : I<Domain>ModelFactory
{
    private readonly I<Domain>Service _<domain>Service;
    private readonly ILocalizationService _localizationService;

    public <Domain>ModelFactory(I<Domain>Service <domain>Service,
        ILocalizationService localizationService)
    {
        _<domain>Service = <domain>Service;
        _localizationService = localizationService;
    }

    public Task<<Domain>SearchModel> Prepare<Domain>SearchModelAsync(
        <Domain>SearchModel searchModel)
    {
        searchModel.SetGridPageSize();
        return Task.FromResult(searchModel);
    }

    public async Task<<Domain>ListModel> Prepare<Domain>ListModelAsync(
        <Domain>SearchModel searchModel)
    {
        var entities = await _<domain>Service.GetAllAsync(searchModel.Page - 1, searchModel.PageSize);

        var model = await new <Domain>ListModel()
            .PrepareToGridAsync(searchModel, entities, () =>
                entities.SelectAwait(async x =>
                {
                    return await Prepare<Domain>ModelAsync(null, x, true);
                }));

        return model;
    }

    public async Task<<Domain>Model> Prepare<Domain>ModelAsync(
        <Domain>Model model,
        <Domain> <domain>,
        bool excludeProperties = false)
    {
        if (<domain> != null)
        {
            if (model == null)
            {
                model = <domain>.ToModel<<Domain>Model>();
            }

            if (!excludeProperties)
            {
                model.ChildSearchModel = new {ChildSearchModel}
                {
                    <Domain>Id = <domain>.Id
                };
            }
        }

        if (!excludeProperties)
            model.AvailableTypes = (await {EntityType}.TextList.ToSelectListAsync()).ToList();

        return model;
    }
}
```

---

# 🆕 **9. ServiceEngineer – <Domain>Service.cs (GENERIC TEMPLATE)**

## **9.1 File Structure**

```
/Services
    I<Domain>Service.cs
    <Domain>Service.cs
```

## **9.2 Interface Template — `I<Domain>Service`**

```csharp
public interface I<Domain>Service
{
    Task<<Domain>> Get<Domain>ByIdAsync(int <domain>Id);

    Task<IPagedList<<Domain>>> GetAll<DomainPlural>Async(
        // Add filter parameters based on <Domain> properties
        // Example:
        // string name = null,
        // int? typeId = null,
        // bool? active = null,

        int pageIndex = 0,
        int pageSize = int.MaxValue
    );

    Task Insert<Domain>Async(<Domain> <domain>);

    Task Update<Domain>Async(<Domain> <domain>);

    Task Delete<Domain>Async(<Domain> <domain>);
}
```

## **9.3 Implementation Template — `<Domain>Service`**

```csharp
public class <Domain>Service : I<Domain>Service
{
    private readonly IRepository<<Domain>> _<domain>Repository;
    private readonly ILogger _logger;

    public <Domain>Service(
        IRepository<<Domain>> <domain>Repository,
        ILogger logger)
    {
        _<domain>Repository = <domain>Repository;
        _logger = logger;
    }

    public async Task<<Domain>> Get<Domain>ByIdAsync(int <domain>Id)
    {
        if (id <= 0)
            return null;

        return await _<domain>Repository.GetByIdAsync(id);
    }

    public async Task<IPagedList<<Domain>>> GetAll<DomainPlural>Async(
        // Filter parameters
        // string name = null,
        // int? typeId = null,
        // bool? isActive = null,
        // DateTime? createdFrom = null,
        // DateTime? createdTo = null,

        int pageIndex = 0,
        int pageSize = int.MaxValue)
    {
        return await _<domain>Repository.GetAllPagedAsync(async query =>
        {
            // Filtering examples
            // if (!string.IsNullOrEmpty(name))
            //     query = query.Where(x => x.Name.Contains(name));

            // if (typeId.HasValue)
            //     query = query.Where(x => x.TypeId == typeId);

            // if (isActive.HasValue)
            //     query = query.Where(x => x.IsActive == isActive);

            // if (createdFrom.HasValue)
            //     query = query.Where(x => x.CreatedOnUtc >= createdFrom);

            // if (createdTo.HasValue)
            //     query = query.Where(x => x.CreatedOnUtc <= createdTo);

            query = query.OrderByDescending(x => x.Id);
            return query;
        }, pageIndex, pageSize);
    }

    public async Task Insert<Domain>Async(<Domain> <domain>)
    {
        await _<domain>Repository.InsertAsync(<domain>);
    }

    public async Task Update<Domain>Async(<Domain> <domain>)
    {
        await _<domain>Repository.UpdateAsync(<domain>);
    }

    public async Task Delete<Domain>Async(<Domain> <domain>)
    {
        await _<domain>Repository.DeleteAsync(<domain>);
    }
}
```

## **9.4 Service Standards (Short Rules)**

✔ All methods must be `async`  
✔ No controller business logic  
✔ Always validate null entities  
✔ Never expose `IQueryable` outside the service  
✔ Use `IRepository<<Domain>>` abstraction  
✔ Use LINQ filtering inside `GetAll<DomainPlural>Async`  
✔ **Return `IPagedList<<Domain>>` for list operations unless the entity has a naturally small, finite set of records — in that case return `IList<<Domain>>`**  
✔ **Never use ViewModels in the service layer**  
✔ Mapping to/from ViewModels must happen in **ModelFactory**, never in service  
✔ Services must not contain UI logic


## **9.5 Placeholder Map**

| Placeholder           | Meaning                           |
| --------------------- | --------------------------------- |
| `<Domain>`            | Entity class                      |
| `<DomainPlural>`           | Plural entity class               |
| `I<Domain>Service`    | Service interface                 |
| `<Domain>Service`     | Service implementation            |

---


# 🏭 **10. ModelFactoryEngineer – <Domain>ModelFactory Templates (Admin + Public)**

The **ModelFactoryEngineer** prepares ViewModels for both Admin and Public.

✔ Admin: `<Domain>Model`, `<Domain>ListModel>`, `<Domain>SearchModel>`  
✔ Public: `<Domain>Model>` (customer-facing)

ModelFactories do **mapping only**, not business logic.


## **10.1 File Structure**

> **The filename stays the same (`<Domain>ModelFactory.cs`) in both Admin and Public.
> Only the folder + namespace change.**

```
/Areas
    Admin
        Factories
            I<Domain>ModelFactory.cs
            <Domain>ModelFactory.cs       <-- Admin model factory

/Factories
    I<Domain>ModelFactory.cs
    <Domain>ModelFactory.cs               <-- Public model factory
```

## **10.2 Admin ModelFactory Interface**

```csharp
namespace NopStation.Plugin.<Group>.<Name>.Areas.Admin.Factories;

public interface I<Domain>ModelFactory
{
    Task<<Domain>SearchModel> Prepare<Domain>SearchModelAsync(<Domain>SearchModel searchModel);

    Task<<Domain>ListModel> Prepare<Domain>ListModelAsync(<Domain>SearchModel searchModel);

    Task<<Domain>Model> Prepare<Domain>ModelAsync(
        <Domain>Model model,
        <Domain> <domain>,
        bool excludeProperties = false
    );
}
```

## **10.3 Admin ModelFactory Implementation — `<Domain>ModelFactory`**

> **Admin factory uses Admin namespaces and Admin ViewModels.**

```csharp
namespace NopStation.Plugin.<Group>.<Name>.Areas.Admin.Factories;

public class <Domain>ModelFactory : I<Domain>ModelFactory
{
    private readonly IDateTimeHelper _dateTimeHelper;
    private readonly I<Domain>Service _<domain>Service;
    private readonly ILocalizationService _localizationService;

    public <Domain>ModelFactory(
        IDateTimeHelper dateTimeHelper,
        I<Domain>Service <domain>Service,
        ILocalizationService localizationService)
    {
        _dateTimeHelper = dateTimeHelper;
        _<domain>Service = <domain>Service;
        _localizationService = localizationService;
    }

    public async Task<<Domain>SearchModel> Prepare<Domain>SearchModelAsync(<Domain>SearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);
        searchModel.SetGridPageSize();
        return searchModel;
    }

    public async Task<<Domain>ListModel> Prepare<Domain>ListModelAsync(<Domain>SearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        var <domainPlural> = await _<domain>Service.GetAll<Domains>Async(
            pageIndex: searchModel.Page - 1,
            pageSize: searchModel.PageSize
        );

        var model = await new <Domain>ListModel().PrepareToGridAsync(searchModel, <domainPlural>, () =>
        {
            return <domainPlural>.SelectAwait(async x => await Prepare<Domain>ModelAsync(null, x, true));
        });

        return model;
    }

    public async Task<<Domain>Model> Prepare<Domain>ModelAsync(
        <Domain>Model model,
        <Domain> <domain>,
        bool excludeProperties = false)
    {
        if (<domain> != null)
        {
            if (model == null)
                model = <domain>.ToModel<<Domain>Model>();

            // Convert UTC → Local Time
            model.CreatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(<domain>.CreatedOnUtc, DateTimeKind.Utc);
            model.UpdatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(<domain>.UpdatedOnUtc, DateTimeKind.Utc);

            if (!excludeProperties)
            {
                // Fill nested models, lists, dropdowns
            }
        }

        if (!excludeProperties)
        {
            // Populate dropdown lists
        }

        return model;
    }
}
```

## **10.4 Public ModelFactory Interface (Same name, different namespace)**

```csharp
namespace NopStation.Plugin.<Group>.<Name>.Factories;

public interface I<Domain>ModelFactory
{
    Task<<Domain>Model> Prepare<Domain>ModelAsync(<Domain> <domain>);

    Task<IList<<Domain>Model>> Prepare<Domain>ListModelAsync(IList<<Domain>> <domainPlural>);
}
```

## **10.5 Public ModelFactory Implementation — `<Domain>ModelFactory`**

```csharp
namespace NopStation.Plugin.<Group>.<Name>.Factories;

public class <Domain>ModelFactory : I<Domain>ModelFactory
{
    private readonly IDateTimeHelper _dateTimeHelper;
    private readonly I<Domain>Service _<domain>Service;
    private readonly ILocalizationService _localizationService;

    public <Domain>ModelFactory(
        IDateTimeHelper dateTimeHelper,
        I<Domain>Service <domain>Service,
        ILocalizationService localizationService)
    {
        _dateTimeHelper = dateTimeHelper;
        _<domain>Service = <domain>Service;
        _localizationService = localizationService;
    }

    public async Task<<Domain>Model> Prepare<Domain>ModelAsync(<Domain> <domain>)
    {
        if (<domain> == null)
            return null;

        var model = <domain>.ToModel<<Domain>Model>();

        // Convert DateTime UTC → Local Time
        model.CreatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(<domain>.CreatedOnUtc, DateTimeKind.Utc);
        model.UpdatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(<domain>.UpdatedOnUtc, DateTimeKind.Utc);

        return model;
    }

    public async Task<IList<<Domain>Model>> Prepare<Domain>ListModelAsync(IList<<Domain>> <domainPlural>)
    {
        var list = new List<<Domain>Model>();

        foreach (var <domain> in <domainPlural>)
        {
            var model = await Prepare<Domain>ModelAsync(<domain>);
            if (model != null)
                list.Add(model);
        }

        return list;
    }
}
```

## **10.6 Rules for ModelFactoryEngineer (Admin + Public)**

### ✔ **Admin ModelFactory Rules**

* Prepares `<Domain>Model`, `<Domain>ListModel>`, `<Domain>SearchModel>`
* Uses Admin ViewModels
* Uses Admin namespaces (`Areas.Admin.Factories`)
* Converts UTC → Local
* Never exposes domain entities to views

### ✔ **Public ModelFactory Rules**

* Prepares lightweight storefront models
* Uses Public ViewModels
* Uses Public namespace (`Factories`)
* Converts UTC → Local
* No admin-only fields, no sensitive info

### ✔ **Common Rules**

* **Factory name MUST remain the same:** `<Domain>ModelFactory`
* Only namespace changes (Admin vs Public)
* Mapping layer only → no business logic
* Never access HttpContext, ViewBag, or repository
* Must use `IDateTimeHelper` for time conversion
* Must use async everywhere
* Dropdown lists filled only in Admin factory


## **10.7 Updated Placeholder Map**

| Placeholder                 | Meaning                                         |
| --------------------------- | ----------------------------------------------- |
| `<Domain>`                  | Entity class                                    |
| `<Domains>`                 | Plural form                                     |
| `<domain>`                  | camelCase variable                              |
| `<Domain>Model`             | ViewModel (Admin or Public, based on namespace) |
| `<Domain>ListModel`         | Admin list/grid ViewModel                       |
| `<Domain>SearchModel`       | Admin filtering model                           |
| `<Domain>ModelFactory>`     | Same class name for both Admin & Public         |
| **Namespace defines usage** | `Areas.Admin.Factories` vs `Factories`          |



## **10.8 Summary**

✔ Same class name for Admin & Public: **`<Domain>ModelFactory`**  
✔ Different namespaces keep responsibilities separate  
✔ Admin factory → CRUD + grids  
✔ Public factory → storefront models  
✔ UTC → Local correctly applied  
✔ Mapping only, no business logic  
✔ Architecture 100% NopStation compliant  

---
Below is the **updated full section** with **file-scoped namespaces**, following .NET 9 / NopCommerce 5.x conventions.

All previous block-style namespaces:

```csharp
namespace NopStation.Plugin.<Group>.<Name>
{
    ...
}
```

are now converted to **file-scoped format**:

```csharp
namespace NopStation.Plugin.<Group>.<Name>;
```

Everything remains compatible with your region rules.

---

# ⚙️ **11. PluginNopStartup, MapperConfiguration & DLL Reference Standards**

This section defines the plugin startup, AutoMapper configuration, standardized region usage, and the required DLL reference.

All class files must use **file-scoped namespace format**.


## **11.1 PluginNopStartup Rules & Template (File-Scoped)**

✔ Must use `INopStartup`  
✔ Must call `AddNopStationServices("<SystemName>")`
✔ Must register services & factories
✔ Namespace MUST be **file-scoped**
✔ Must add `..\NopStation.Plugin.Misc.Core\NopStation.Plugin.Misc.Actions.dll` as a DLL reference for NopStation plugins. Use this exact reference format in plugin `.csproj` files:

```xml
<Reference Include="NopStation.Plugin.Misc.Actions">
  <HintPath>..\NopStation.Plugin.Misc.Core\NopStation.Plugin.Misc.Actions.dll</HintPath>
  <Private>false</Private>
</Reference>
```

### **11.1.1 PluginNopStartup Template**

```csharp
namespace NopStation.Plugin.<Group>.<Name>;

public class PluginNopStartup : INopStartup
{
    #region Methods

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register base NopStation helper services
        services.AddNopStationServices("NopStation.Plugin.<Group>.<Name>");

        // Register domain services
        services.AddScoped<I<Domain>Service, <Domain>Service>();

        // Register factories
        services.AddScoped<Factories.I<Domain>ModelFactory, Factories.<Domain>ModelFactory>();
        services.AddScoped<Factories.I<Domain>PublicModelFactory, Factories.<Domain>PublicModelFactory>();
    }

    public void Configure(IApplicationBuilder application)
    {
    }

    #endregion

    #region Properties

    public int Order => 10;

    #endregion
}
```

## **11.2 MapperConfiguration Rules & Template**

✔ Must inherit `Profile` and `IOrderedMapperProfile`  
✔ All mapping logic belongs inside constructor  
✔ File-scoped namespace required

### **11.2.1 MapperConfiguration Template**

```csharp
namespace NopStation.Plugin.<Group>.<Name>;

public class MapperConfiguration : Profile, IOrderedMapperProfile
{
    #region Ctor

    public MapperConfiguration()
    {
        // <Domain> → <Domain>Model
        CreateMap<<Domain>, <Domain>Model>()
            .ForMember(m => m.CreatedOn, opt => opt.Ignore())
            .ForMember(m => m.UpdatedOn, opt => opt.Ignore())
            .ForMember(m => m.CustomProperties, opt => opt.Ignore());

        // <Domain>Model → <Domain>
        CreateMap<<Domain>Model, <Domain>>()
            .ForMember(e => e.CreatedOnUtc, opt => opt.Ignore())
            .ForMember(e => e.UpdatedOnUtc, opt => opt.Ignore());

        #region Additional Mappings

        // Add more mappings here as needed

        #endregion
    }

    #endregion

    #region Properties

    public int Order => 0;

    #endregion
}
```

## **11.3 Required #region Structure (Applies to ALL Classes)**

All C# files must follow this exact region layout:


### **1️⃣ `#region Fields`**

```
#region Fields

private readonly I<Domain>Service _domainService;
private readonly ILogger _logger;

#endregion
```


### **2️⃣ `#region Ctor`**

```
#region Ctor

public <Domain>Service(IRepository<<Domain>> repo)
{
    _repo = repo;
}

#endregion
```


### **3️⃣ `#region Utilities` (Optional)**

```
#region Utilities

protected virtual Task<bool> ValidateAsync(<Domain> entity)
{
    ...
}

#endregion
```

### **4️⃣ `#region Methods` (must have subregions when needed)**

```
#region Methods

public async Task<<Domain>> Get<Domain>ByIdAsync(int <domain>Id) { }

public async Task Insert<Domain>Async(<Domain> <domain>) { }

public async Task Update<Domain>Async(<Domain> <domain>) { }

public async Task Delete<Domain>Async(<Domain> <domain>) { }

#endregion
```

---

# 🏁 **14. Final Notes**

All agents must strictly follow NopStation engineering standards, solution-level conventions, and build rules.
Every generated output must adhere to the following:

## ✅ **14.1 Coding & Style Rules**

* Follow all conventions defined in **`.editorconfig`**
* Follow solution-wide build rules defined in **`src/Directory.Build.props`**
* All namespaces must be **file-scoped**
* All class files must follow NopStation’s standardized region structure
* Naming conventions must match `.editorconfig` (PascalCase, `_camelCase`, interfaces start with `I`, async suffix, etc.)
* Maintain **consistent indentation** (4 spaces for C#, 2 spaces for JSON/XML/JS/CSS)
* Usings must be sorted with `System.*` first
* Ensure all generated code has **zero warnings**, except those explicitly disabled
* All async methods must end with `Async`
* All DateTime values returned to UI must be converted from UTC → Local time

## ✅ **14.2 Architecture & Quality Rules**

* Follow **Clean Architecture** principles
* Service layer **must not use ViewModels**
* ModelFactories handle **mapping only**, not business logic
* All controllers, services, factories, and models must be **production-ready**
* Output must compile successfully on **.NET 9.0**
* Follow plugin structure and standards of NopStation

## ✅ **14.3 Build & Maintainability Rules**

* Code must remain fully compatible with global build settings
* Must not introduce warnings suppressed via `Directory.Build.props`
* All outputs must meet plugin packaging & release requirements

---

# 📘 **15. Explanation of .editorconfig and Directory.Build.props Rules**

This section helps developers understand how global configuration files affect every file generated by agents.

## **15.1 `.editorconfig` — Coding Standards Enforcement**

`.editorconfig` defines mandatory formatting, naming, and style rules. All generated code must follow these rules exactly.

### ✔ Formatting Rules

* Use **spaces only** (no tabs)
* C# indentation: **4 spaces**
* JSON, XML, JS, CSS indentation: **2 spaces**
* Encoding: **UTF-8 BOM**
* Prevent auto-reformatting of `*.min.*` files

### ✔ Naming Rules

* **PascalCase** → classes, structs, enums, delegates, methods, properties, namespaces
* **Interfaces** → must begin with `I`
* **Private fields** → `_camelCase`
* **Async methods** → must end with `Async`
* **Constants** → ALL_UPPER_SNAKE_CASE
* Anything not covered → **camelCase**

### ✔ Using Directives

* `System.*` directives must appear **first**
* No separated groups

### ✔ Style Rules

* Prefer `var` whenever type is obvious
* Prefer expression-bodied properties/indexers/accessors
* Prefer null propagation, coalescing, pattern matching
* Enforce modifier ordering
* Braces required only for multi-line statements
* Use file-scoped namespaces (`namespace X;`)

### ✔ Formatting Rules

* New line before brace
* New line before `else`, `catch`, `finally`
* New line between LINQ clauses
* Space around binary operators and after commas

`.editorconfig` ensures **every agent outputs mission-ready code that passes all style checks automatically.**

## **15.2 `src/Directory.Build.props` — Build Rules & Project-Wide Policies**

This file defines strict build and compilation rules applied to **every project in the solution**.

### ✔ Target Framework

All plugins must target:

```
net9.0
```

### ✔ Implicit Usings

Must remain enabled:

```
<ImplicitUsings>enable</ImplicitUsings>
```

Agents must NOT manually include commonly used namespaces already added implicitly.

### ✔ Repository Type

Set to:

```
<RepositoryType>Git</RepositoryType>
```

Required for correct NuGet metadata.

### ✔ Warning Rules

NU1901–NU1904 (vulnerability warnings) are suppressed globally:

```
<NoWarn>NU1901;NU1902;NU1903;NU1904</NoWarn>
```

Meaning:

* Agents **must not** try to “fix” or re-enable these warnings
* All **other warnings must be eliminated** (zero-warning policy)

### ✔ Build Consistency

All generated code must compile successfully under these global build conditions without requiring:

* Additional csproj modifications
* Manual fixes
* Custom settings per plugin

`Directory.Build.props` ensures all output is **uniform, reliable, and production-compliant** across the entire plugin suite.

## **15.3 Summary**

### Agents must:

* Comply with global formatting, naming, and style rules
* Produce fully buildable .NET 9 plugin code
* Respect suppressed warnings and global build behavior
* Never violate or override `.editorconfig` or project-wide props
* Produce code that is visually consistent and standards-compliant

---

