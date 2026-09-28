  
**NOP-STATION**

**Plugin Development Framework**

Common Structure & Agent Prompt Templates

*Based on AGENTS.md Convention*

nopCommerce 4.90  ·  .NET 9  ·  NopStation Core

Version 1.0  ·  2025

| 11 Specialized Agents | Full SDLC Coverage | Production-Ready Output |
| :---: | :---: | :---: |

# **How to Use This Document**

This document is your complete copy-paste reference for building any NopStation plugin with AI agents. Every section maps directly to a phase in the SDLC and contains ready-to-use prompt templates.

| Step | What You Do | Output |
| ----- | ----- | ----- |
| 1 | Fill in your plugin identity (Section 2\) | Plugin name, namespace, group |
| 2 | Write CONTEXT.md (Section 3\) | AI understands your plugin |
| 3 | Write AGENT.md (Section 4\) | AI knows how to behave |
| 4 | Run SDLC Discovery prompts (Section 5\) | Business \+ requirements docs |
| 5 | Run each Agent session (Section 6\) | Code files for your plugin |
| 6 | Update TASKS.md after each session (Section 7\) | Sprint board always current |

| KEY | Replace every \[PLACEHOLDER\] in brackets with your actual plugin information before pasting into AI. |
| :---: | :---- |

# **Section 2 — Plugin Identity**

Fill this in once. Copy it to the top of every AI session for this plugin.

| \#\# Plugin Identity SystemName   : NopStation.Plugin.\[GROUP\].\[NAME\] Namespace    : NopStation.Plugin.\[GROUP\].\[NAME\] Group        : \[Misc / Widgets / Payments / Shipping / etc.\] FriendlyName : \[Human readable name\] Version      : 4.90.1.0 Table Prefix : NS\_\[ABBREVIATION\]\_ \#\# Example (ComboDiscount) SystemName   : NopStation.Plugin.Misc.ComboDiscount Namespace    : NopStation.Plugin.Misc.ComboDiscount Group        : Misc FriendlyName : Combo Discount Version      : 4.90.1.0 Table Prefix : NS\_ComboDiscount\_ |
| :---- |

## **Group Reference**

| Group | Use When Plugin Is About |
| ----- | ----- |
| Misc | General features, discounts, utilities, automation |
| Widgets | Frontend UI components, banners, popups, sliders |
| Payments | Payment gateway integrations |
| Shipping | Shipping provider integrations |
| Tax | Tax calculation or provider integrations |
| ExternalAuth | OAuth / social login integrations |
| SMS | SMS notification providers |

# **Section 3 — CONTEXT.md Template**

Write this file once per plugin. Paste it at the top of every AI session. It tells the AI what your plugin does, what the domain concepts are, and what the critical rules are.

| PATH | plugins/\[PluginName\]/\_ai/CONTEXT.md |
| :---: | :---- |

| \# Plugin Context: NopStation.Plugin.\[GROUP\].\[NAME\] \#\# What This Plugin Does \[One clear paragraph. What problem does it solve?  Who uses it (admin / customer)? What does it do automatically?\] \#\# Plugin Identity \- SystemName : NopStation.Plugin.\[GROUP\].\[NAME\] \- Namespace  : NopStation.Plugin.\[GROUP\].\[NAME\] \- Group      : \[GROUP\] \- Version    : 4.90.1.0 \#\# Domain Concepts \[EntityName\]   Definition: \[What is this entity in business terms?\]   Fields: \[list key fields and what they mean\] \[EntityName2\]  \<- add more if needed   Definition: ...   Fields: ... \#\# Business Rules BR-001  \[Rule written as plain English statement\] BR-002  \[Rule written as plain English statement\] BR-003  \[Add as many as needed\] \#\# What Is NOT In This Plugin (v1) \- \[Feature explicitly excluded\] \- \[Feature explicitly excluded\] \#\# Critical Files AI Must Know \- Domain/\[Entity\].cs                   \-\> the main entity \- Services/I\[Entity\]Service.cs         \-\> service contract \- Services/\[Entity\]Service.cs          \-\> especially \[method name\] \- Events/\[EventConsumer\].cs            \-\> integration hook \#\# Integration Points \- Hooks into : \[nopCommerce event or service\] \- Uses       : \[IOrderService, IProductService, etc.\] \- Permission : \[PluginName\]PermissionProvider.Manage\[PluginName\] |
| :---- |

### **The 5 Questions Method — Fill CONTEXT.md in 15 Minutes**

Answer these 5 questions first, then paste the answers into the template above:

| \# | Question | Example Answer (ComboDiscount) |
| ----- | ----- | ----- |
| 1 | What does this plugin DO in one sentence? | Applies % discounts based on which products are in the cart |
| 2 | What are the KEY nouns (entities)? | ComboDiscount (rule with ProductAId, ProductBId, 3 discount tiers) |
| 3 | What are the KEY logic rules? | A+B in cart \= combo discount; highest across rules wins |
| 4 | What is OUT OF SCOPE v1? | Category combos, 3+ products, time-limited rules |
| 5 | Which files are most critical? | ComboDiscountService.cs → GetApplicableDiscountPercentAsync |

# **Section 4 — AGENT.md Template**

This file tells the AI HOW to behave for your specific plugin — its critical logic, its ask-before list, and its non-negotiable rules.

| PATH | plugins/\[PluginName\]/\_ai/AGENT.md |
| :---: | :---- |

| \# Agent Instructions: \[PluginName\] Plugin \#\# This Plugin's Critical Logic (Do Not Simplify) \[Describe the most important method or algorithm.  Be specific about inputs, steps, and output.  The AI must not change this without asking.\] Example: GetApplicableDiscountPercentAsync(IList\<int\> cartProductIds) must:   Step 1: Load ALL active rules (not just first match)   Step 2: For each rule evaluate hasA, hasB, hasA&\&hasB   Step 3: Pick highest applicable discount across all rules   Step 4: Return that value (0 if nothing matches) \#\# Admin Grid — Required Columns \[Column 1\] | \[Column 2\] | ... | Edit button \#\# Search Filters on List Page \- \[FilterName\] (\[type\], \[description\]) \- \[FilterName\] (\[type\], \[description\]) \#\# Settings This Plugin Uses \- \[SettingName\] (\[type\]) — \[what it controls\] \- \[SettingName\] (\[type\]) — \[what it controls\] \#\# Ask Before Doing These \- Any change to \[Entity\] fields \- Any new NuGet dependency \- Any deviation from AGENTS.md patterns \- Any change to the core \[algorithm name\] logic \#\# Known Constraints \- \[Constraint 1\] \- \[Constraint 2\] |
| :---- |

# **Section 5 — SDLC Discovery Prompts**

Run these prompts BEFORE writing any code. They convert your rough idea into structured requirements that all later agents depend on.

## **Phase 1 — Business Discovery Prompt**

| WHEN | Day 1, before anything else. Use when you have only a rough idea. |
| :---: | :---- |

| \#\# Business Discovery Request \[PASTE CONTEXT.md here\] My rough idea: \[describe in plain language\] Please extract and return: 1\. Business Problem Statement (1 paragraph) 2\. Stakeholders (who benefits?) 3\. Pain this solves 4\. What happens if we do NOT build it? 5\. Success metrics (how do we know it worked?) 6\. Risks and assumptions 7\. Out of scope (what are we NOT building?) 8\. Open questions that must be answered before coding |
| :---- |

## **Phase 2 — Requirements Analysis Prompt**

| WHEN | After Phase 1 output is reviewed and open questions answered. |
| :---: | :---- |

| \#\# Requirements Analysis Request \[PASTE CONTEXT.md here\] \[PASTE Phase 1 output here\] Please generate: \#\# Functional Requirements Format: FR-001  \[requirement description\] Cover: Admin features, Storefront behavior, System/automated behavior \#\# Non-Functional Requirements Format: NFR-001  \[requirement description\] Cover: Performance, Security, Scalability, Compatibility \#\# Business Rules Format: BR-001  \[rule as plain English statement\] \#\# User Stories Format: US-001  As a \[role\], I want \[action\], so that \[benefit\] Include acceptance criteria for each story \#\# Edge Cases Format: EC-001  \[unusual input or state the system must handle\] |
| :---- |

## **Phase 3 — System Design Prompt**

| WHEN | After requirements are finalized. Before any agent sessions start. |
| :---: | :---- |

| \#\# System Design Request \[PASTE CONTEXT.md here\] \[PASTE AGENT.md here\] \[PASTE Phase 2 output here\] Stack: NopStation Plugin, nopCommerce 4.90, .NET 9,        linq2db, Razor Admin UI, AGENTS.md conventions Please generate: 1\. Entity Design — all domain models with fields and types 2\. Service Layer Design — all service method signatures 3\. Admin UI Flow — screen list and navigation flow 4\. Integration Points — where does this hook into nopCommerce? 5\. Data Flow — how does data move from Admin → cart → discount? 6\. Dependency Map — which nopCommerce services do we use? 7\. File Structure — full folder \+ file tree |
| :---- |

# **Section 6 — Agent Session Templates**

Every session opens with the same structure. The only thing that changes is the task instruction at the bottom and which previous files you paste in.

## **Universal Session Opening Block**

| RULE | Paste this at the TOP of EVERY AI session, without exception. |
| :---: | :---- |

| ════════════════════════════════════════════════   SESSION OPENING — PASTE THESE IN ORDER ════════════════════════════════════════════════ \[1\] MASTER-CONTEXT.md     \<- paste full file \[2\] MASTER-AGENT.md       \<- paste full file \[3\] AGENTS.md             \<- paste relevant section(s) only \[4\] Plugin CONTEXT.md     \<- paste plugin-specific file \[5\] Plugin AGENT.md       \<- paste plugin-specific file \[6\] Previous output files \<- paste actual file content \[7\] Your task instruction \<- what to do this session ════════════════════════════════════════════════ |
| :---- |

  **SESSION 1 — PluginArchitect**


| AGENTS.md Section | 2.1 — PluginArchitect |
| :---- | :---- |
| Previous Files Needed | None — this is the first session |
| Run In Parallel With | Nothing — must run first |
| Output Goes To | All subsequent sessions |

| \[PASTE MASTER-CONTEXT.md\] \[PASTE MASTER-AGENT.md\] \[PASTE AGENTS.md Section 2.1\] \[PASTE CONTEXT.md\] \[PASTE AGENT.md\] ════════════════════════════════════════════════ You are: PluginArchitect Plugin: NopStation.Plugin.\[GROUP\].\[NAME\] Generate: 1\. Complete folder \+ file tree (every single file listed) 2\. plugin.json (complete, production-ready) 3\. \[Name\]Plugin.cs skeleton    — Install(), Uninstall(), GetConfigurationPageUrl() 4\. \[Name\]PermissionProvider.cs    — Permission: PublicPortal.Manage\[Name\] 5\. AdminMenuCreatedEventConsumer.cs    — Menu registration skeleton 6\. PluginNopStartup.cs    — Skeleton with empty ConfigureServices() 7\. MapperConfiguration.cs    — Skeleton, no mappings yet 8\. \_ViewImports.cshtml for Admin and Public views DO NOT write service, controller, or database code. Produce structure and skeletons only. End with a complete Handoff Block. |
| :---- |

  **SESSION 2 — DatabaseEngineer**


| AGENTS.md Section | 2.3 — DatabaseEngineer |
| :---- | :---- |
| Previous Files Needed | Session 1: folder tree |
| Run In Parallel With | Nothing — needs Session 1 first |

| \[PASTE MASTER-CONTEXT.md\] \[PASTE MASTER-AGENT.md\] \[PASTE AGENTS.md Section 2.3\] \[PASTE CONTEXT.md\] \[PASTE AGENT.md\] \[PASTE folder tree from Session 1\] ════════════════════════════════════════════════ You are: DatabaseEngineer Generate the complete data layer: 1\. Domain/\[Entity\].cs    Fields:    \- \[FieldName\] (\[type\]) — \[description\]    \- \[FieldName\] (\[type\]) — \[description\]    \- CreatedOnUtc (DateTime)    \- UpdatedOnUtc (DateTime) 2\. Data/Builders/\[Entity\]Builder.cs    \- \[FieldName\]: \[precision/length/nullable rules\] 3\. Data/Migrations/SchemaMigration.cs    \- Migration date: \[YYYY-MM-DD HH:mm:ss\]    \- Description: "\[PluginName\] base schema" 4\. Data/Migrations/BaseNameCompatibility.cs    \- Table name: NS\_\[ABBREVIATION\]\_\[Entity\] End with complete Handoff Block. |
| :---- |

  **SESSION 3 — ServiceEngineer**


| AGENTS.md Section | 2.6 \+ Section 9 |
| :---- | :---- |
| Previous Files Needed | Session 2: Domain/\[Entity\].cs |
| Run In Parallel With | Nothing — needs entity file first |

| \[PASTE MASTER-CONTEXT.md\] \[PASTE MASTER-AGENT.md\] \[PASTE AGENTS.md Section 2.6 \+ Section 9\] \[PASTE CONTEXT.md\] \[PASTE AGENT.md\] \[PASTE Domain/\[Entity\].cs — actual file content\] ════════════════════════════════════════════════ You are: ServiceEngineer Generate: 1\. Services/I\[Entity\]Service.cs 2\. Services/\[Entity\]Service.cs Standard CRUD methods required:   Get\[Entity\]ByIdAsync(int id)   GetAll\[Entities\]Async(filters..., pageIndex, pageSize)   Insert\[Entity\]Async(\[Entity\] entity)   Update\[Entity\]Async(\[Entity\] entity)   Delete\[Entity\]Async(\[Entity\] entity) Additional business methods:   \[MethodName\](\[params\]) — \[what it does\]   \[MethodName\](\[params\]) — \[what it does\] Critical logic for \[MethodName\]:   \[Describe the algorithm step by step\]   \[The AI must not simplify this\] End with complete Handoff Block. |
| :---- |

  **SESSIONS 4a \+ 4b — Run in Parallel**


| PARALLEL | Open two browser tabs at the same time. Session 4a and 4b have no dependency on each other. |
| :---: | :---- |

### **Session 4a — ModelFactoryEngineer**

| AGENTS.md Section | Section 10 — ModelFactoryEngineer |
| :---- | :---- |
| Previous Files Needed | Session 2: Entity.cs \+ Session 3: IService.cs |

| \[PASTE MASTER-CONTEXT.md\] \[PASTE MASTER-AGENT.md\] \[PASTE AGENTS.md Section 10\] \[PASTE CONTEXT.md\] \[PASTE AGENT.md\] \[PASTE Domain/\[Entity\].cs\] \[PASTE Services/I\[Entity\]Service.cs\] ════════════════════════════════════════════════ You are: ModelFactoryEngineer (Admin) Generate Admin factory layer: 1\. Areas/Admin/Models/\[Entity\]Model.cs    Fields to expose in form:    \- \[FieldName\] (\[type\], required/optional)    \- \[FieldName\] (\[type\], required/optional) 2\. Areas/Admin/Models/\[Entity\]SearchModel.cs    Search fields:    \- \[SearchFieldName\] (\[type\]) — \[filter description\] 3\. Areas/Admin/Models/\[Entity\]ListModel.cs 4\. Areas/Admin/Factories/I\[Entity\]ModelFactory.cs 5\. Areas/Admin/Factories/\[Entity\]ModelFactory.cs Grid columns needed:   \[Column1\] | \[Column2\] | \[Column3\] | Edit button End with Handoff Block. |
| :---- |

### **Session 4b — FrontendEngineer**

| AGENTS.md Section | Section 8.1 — FrontendEngineer Razor patterns |
| :---- | :---- |
| Previous Files Needed | Describe model fields — 4a may still be running |

| \[PASTE MASTER-CONTEXT.md\] \[PASTE MASTER-AGENT.md\] \[PASTE AGENTS.md Section 8.1\] \[PASTE CONTEXT.md\] \[PASTE AGENT.md\] \#\# Model Structure (describe manually since 4a may be running) \[Entity\]Model fields:   \[FieldName\] (\[type\])   \[FieldName\] (\[type\]) \[Entity\]SearchModel fields:   \[SearchField\] (\[type\]) ════════════════════════════════════════════════ You are: FrontendEngineer Generate all Admin Razor views: 1\. Areas/Admin/Views/\[Entity\]/List.cshtml    Search filters: \[list filters\]    Grid columns: \[list columns\] 2\. Areas/Admin/Views/\[Entity\]/Create.cshtml 3\. Areas/Admin/Views/\[Entity\]/Edit.cshtml    Include delete confirmation popup 4\. Areas/Admin/Views/\[Entity\]/\_CreateOrUpdate.cshtml    Cards needed: \[Info\] \[list other cards if any\] 5\. Areas/Admin/Views/\[Entity\]/\_CreateOrUpdate.Info.cshtml    Fields: \[list each field with required/optional\] End with Handoff Block. |
| :---- |

  **SESSION 5 — ApiEngineer (Controller)**


| AGENTS.md Section | Section 7 — Controllers |
| :---- | :---- |
| Previous Files Needed | Sessions 3 \+ 4a: IService \+ IFactory \+ Models \+ Validator |

| \[PASTE MASTER-CONTEXT.md\] \[PASTE MASTER-AGENT.md\] \[PASTE AGENTS.md Section 7\] \[PASTE CONTEXT.md\] \[PASTE AGENT.md\] \[PASTE Services/I\[Entity\]Service.cs\] \[PASTE Areas/Admin/Models/\[Entity\]Model.cs\] \[PASTE Areas/Admin/Models/\[Entity\]SearchModel.cs\] \[PASTE Areas/Admin/Factories/I\[Entity\]ModelFactory.cs\] \[PASTE Areas/Admin/Validators/\[Entity\]Validator.cs\] ════════════════════════════════════════════════ You are: ApiEngineer Generate: Areas/Admin/Controllers/\[Entity\]Controller.cs Actions required:   List (GET)    → return view with search model   List (POST)   → return JSON for DataTables   Create (GET)  → return empty create form   Create (POST) → save, redirect to List or Edit   Edit (GET)    → load entity, return filled form   Edit (POST)   → update, redirect to List or Edit   Delete (POST) → delete, redirect to List Permission: \[Name\]PermissionProvider.Manage\[Name\] Use \[AutoValidateAntiforgeryToken\] on all POST actions Use ParameterBasedOnFormName for save-continue behavior End with Handoff Block. |
| :---- |

  **SESSION 6 — ConfigEngineer**


| AGENTS.md Section | 2.5 — ConfigEngineer |
| :---- | :---- |
| Previous Files Needed | Session 5: Controller exists for Configure action |

| \[PASTE MASTER-CONTEXT.md\] \[PASTE MASTER-AGENT.md\] \[PASTE AGENTS.md Section 2.5\] \[PASTE CONTEXT.md\] \[PASTE AGENT.md\] ════════════════════════════════════════════════ You are: ConfigEngineer Plugin settings needed:   \- \[SettingName\] (\[type\]) — \[what it controls\]   \- \[SettingName\] (\[type\]) — \[what it controls\] Generate: 1\. \[Name\]Settings.cs 2\. Areas/Admin/Models/ConfigurationModel.cs    Each property must be followed by its \_OverrideForStore bool 3\. Configure (GET \+ POST) action in \[Name\]Controller    With full multi-store support 4\. Areas/Admin/Views/\[Name\]/Configure.cshtml    Include StoreScopeConfigurationViewComponent End with Handoff Block. |
| :---- |

  **SESSION 7 — Startup Wiring \+ Mapper**


| \[PASTE MASTER-CONTEXT.md\] \[PASTE MASTER-AGENT.md\] \[PASTE AGENTS.md Section 11\] \[PASTE CONTEXT.md\] \[PASTE AGENT.md\] \[PASTE Services/I\[Entity\]Service.cs\] \[PASTE Areas/Admin/Factories/I\[Entity\]ModelFactory.cs\] \[PASTE Areas/Admin/Validators/\[Entity\]Validator.cs\] \[PASTE MapperConfiguration.cs skeleton from Session 1\] ════════════════════════════════════════════════ You are: CodeEngineer Generate final wiring files: 1\. Infrastructure/PluginNopStartup.cs (complete)    Register:    \- I\[Entity\]Service → \[Entity\]Service    \- Admin I\[Entity\]ModelFactory → \[Entity\]ModelFactory    \- Public I\[Entity\]ModelFactory → \[Entity\]ModelFactory    \- \[Entity\]Validator    \- EventConsumer (if any) 2\. MapperConfiguration.cs (complete with all mappings)    \[Entity\] → \[Entity\]Model      Ignore: CreatedOn, UpdatedOn (local time conversion)    \[Entity\]Model → \[Entity\]      Ignore: CreatedOnUtc, UpdatedOnUtc End with Handoff Block. |
| :---- |

  **SESSION 8 — TestCaseEngineer**


| \[PASTE MASTER-CONTEXT.md\] \[PASTE MASTER-AGENT.md\] \[PASTE AGENTS.md Section 2.7\] \[PASTE CONTEXT.md\] \[PASTE AGENT.md\] \#\# Business Rules to Cover BR-001  \[paste from CONTEXT.md\] BR-002  \[paste from CONTEXT.md\] ... (paste all BRs) ════════════════════════════════════════════════ You are: TestCaseEngineer Generate full test suite in Markdown table format. Case ID format: \[PLUGINABBR\]\_TC\_\#\#\# Minimum coverage:   \- Happy path (all main scenarios)   \- Each business rule (BR-001, BR-002...)   \- Edge cases (empty input, boundary values)   \- Admin CRUD (create, edit, delete, validation)   \- Security (access without permission)   \- Negative cases (invalid input) Minimum 20 test cases. End with Handoff Block. |
| :---- |

  **SESSION 9 — DocumentEngineer**


| \[PASTE MASTER-CONTEXT.md\] \[PASTE MASTER-AGENT.md\] \[PASTE AGENTS.md Section 2.8\] \[PASTE CONTEXT.md\] \[PASTE AGENT.md\] \#\# Plugin Summary for Documentation \[Write 2–3 paragraphs describing what the plugin does,  who it is for, and what value it delivers\] \#\# Feature List \- \[Feature 1\] \- \[Feature 2\] \- \[Feature 3\] ════════════════════════════════════════════════ You are: DocumentEngineer Generate all 4 documentation files: 1\. doc.html    Path: /files/NopStation.Plugin.\[GROUP\].\[NAME\]/doc.html    Include: Overview, Features, Requirements,             Installation, Configuration, How it works,             Troubleshooting, FAQs, Version history 2\. release\_note.txt    Path: /files/NopStation.Plugin.\[GROUP\].\[NAME\]/release\_note.txt    Version: 4.90.1.0 | Date: \[today\] 3\. nop-station-description.html    Path: /files/NopStation.Plugin.\[GROUP\].\[NAME\]/nop-station-description.html 4\. nopcommerce-description.html    Path: /files/NopStation.Plugin.\[GROUP\].\[NAME\]/nopcommerce-description.html End with Handoff Block. |
| :---- |

  **SESSION 10 — ReleaseEngineer**


| \[PASTE MASTER-CONTEXT.md\] \[PASTE MASTER-AGENT.md\] \[PASTE CONTEXT.md\] \[PASTE AGENT.md\] \#\# Final File List \[Paste your complete file tree with all generated files\] ════════════════════════════════════════════════ You are: ReleaseEngineer Plugin: NopStation.Plugin.\[GROUP\].\[NAME\] Version: 4.90.1.0 Generate: 1\. Verify plugin.json is correct and complete 2\. Build checklist — what must be true before zipping 3\. Release zip structure    NopStation.Plugin.\[GROUP\].\[NAME\]/    └── \[all plugin files\] Build checklist must cover:   \- Zero compiler warnings   \- All migrations registered   \- All services registered in startup   \- plugin.json version matches release\_note.txt   \- Clean install test passed End with Handoff Block. |
| :---- |

# **Section 7 — TASKS.md Template**

Copy this file for every new plugin. Update it after every session. This is your live sprint board.

| PATH | plugins/\[PluginName\]/\_ai/TASKS.md |
| :---: | :---- |

| \# TASKS.md — NopStation.Plugin.\[GROUP\].\[NAME\] Last Updated : \[date\] Current Phase: \[Sprint number and name\] ══════════════════════════════════════════════ \#\# Sprint Status ══════════════════════════════════════════════ | Sprint | Agent                | Status          | Output Files | |--------|----------------------|-----------------|--------------| | 1      | PluginArchitect      | ⏳ Not started  | —            | | 2      | DatabaseEngineer     | ⏳ Not started  | —            | | 3      | ServiceEngineer      | ⏳ Not started  | —            | | 4a     | ModelFactoryEngineer | ⏳ Not started  | —            | | 4b     | FrontendEngineer     | ⏳ Not started  | —            | | 5      | ApiEngineer          | ⏳ Not started  | —            | | 6      | ConfigEngineer       | ⏳ Not started  | —            | | 7      | Startup Wiring       | ⏳ Not started  | —            | | 8      | TestCaseEngineer     | ⏳ Not started  | —            | | 9      | DocumentEngineer     | ⏳ Not started  | —            | | 10     | ReleaseEngineer      | ⏳ Not started  | —            | ══════════════════════════════════════════════ \#\# Open Decisions (You Must Answer These) ══════════════════════════════════════════════ \- \[ \] \[Decision question 1\] \- \[ \] \[Decision question 2\] ══════════════════════════════════════════════ \#\# Blockers ══════════════════════════════════════════════ None. ══════════════════════════════════════════════ \#\# Completed Handoff Notes ══════════════════════════════════════════════ \[Paste Handoff Blocks here as you complete each sprint\] |
| :---- |

# **Section 8 — Handoff Block Template**

Every AI session must end with this block. Add it to your AGENT.md so the AI knows to produce it.

| ADD TO AGENT.md | Include the instruction below so every agent knows to produce a Handoff Block. |
| :---: | :---- |

| \#\# Handoff Rule (Add This to AGENT.md) At the end of EVERY task, produce a Handoff Block: \--- \#\# ✅ Handoff Block \#\#\# Agent Role This Session \[Which agent you were\] \#\#\# Files Produced \- \[filename\] → \[path\] ✅ \- \[filename\] → \[path\] ✅ \#\#\# Decisions Made Independently \- \[decision\]: \[reason\] \#\#\# Open Questions (Need Your Answer Before Next Session) \- ⚠️ \[question\] \#\#\# What to Paste Into Next Session \- This handoff block \- \[list specific files to paste\] \#\#\# Next Agent \[Agent name and what to ask them\] \--- |
| :---- |

# **Section 9 — After Every Session: Your 5-Step Routine**

Do this every time, without exception. It takes 5 minutes and prevents all context loss.

| Step | Action | Detail |
| ----- | ----- | ----- |
| 1 | READ output | Skim for obvious mistakes. Check file names and namespaces match the plugin. |
| 2 | SAVE files | Each file goes to its exact correct path in your project. Do not rename anything. |
| 3 | SAVE chat | Save full chat to \_sessions/\[PluginName\]/session-0X-agentname.md |
| 4 | UPDATE TASKS.md | Change status to ✅ Done. Paste Handoff Block. Note any decisions. |
| 5 | ANSWER blockers | The Handoff Block lists open questions. Decide and add answers to TASKS.md before next session. |

# **Section 10 — Parallel Agent Quick Reference**

Some agents can run at the same time. Open separate browser tabs. Merge outputs when both complete.

## **Can Run In Parallel**

| Agent A | Agent B | Why Safe to Parallelize |
| ----- | ----- | ----- |
| 4a ModelFactoryEngineer | 4b FrontendEngineer | Views only need model field names, not actual model files |
| 8 TestCaseEngineer | Any code session | Test cases are written from requirements, not code |
| 9 DocumentEngineer | 8 TestCaseEngineer | Docs written from feature list, independent of code |

## **Cannot Run In Parallel**

| Agent | Must Wait For |
| ----- | ----- |
| 2 DatabaseEngineer | Session 1 (PluginArchitect) — needs folder structure |
| 3 ServiceEngineer | Session 2 (DatabaseEngineer) — needs Domain entity file |
| 5 ApiEngineer | Sessions 3+4a — needs IService \+ IFactory interfaces |
| 7 Startup Wiring | Sessions 3+4a+6 — needs all services and factories |
| 10 ReleaseEngineer | Everything — final step only |

# **Section 11 — New Plugin Start Checklist**

Use this checklist every time you start a brand new plugin from scratch.

## **Day 1 — Discovery (30 min)**

* Write rough idea in plain language

* Run Phase 1 Business Discovery prompt (Section 5\)

* Review output, answer open questions

* Run Phase 2 Requirements Analysis prompt

* Save FR, NFR, BR, User Stories, Edge Cases

## **Day 1 — Setup (20 min)**

* Create plugins/\[YourPlugin\]/ folder

* Create plugins/\[YourPlugin\]/\_ai/ folder

* Fill in Plugin Identity (Section 2\)

* Write CONTEXT.md using the 5 Questions Method (Section 3\)

* Write AGENT.md with critical logic \+ ask-before list (Section 4\)

* Copy TASKS.md template, update all sprint names (Section 7\)

## **Day 2 — Foundation**

* Session 1: PluginArchitect → folder \+ skeletons

* Review output \+ save files \+ update TASKS.md

* Session 2: DatabaseEngineer → entity \+ migration

* Review output \+ save files \+ update TASKS.md

## **Day 3 — Core Logic**

* Session 3: ServiceEngineer → service layer

* Manually verify logic in your head before proceeding

## **Day 4 — Admin UI (Run 4a \+ 4b in parallel)**

* Tab 1: Session 4a — ModelFactoryEngineer

* Tab 2: Session 4b — FrontendEngineer (parallel)

* Merge both outputs into project

* Session 5: ApiEngineer → controller

## **Day 5 — Wiring \+ Config**

* Session 6: ConfigEngineer → settings \+ configure page

* Session 7: Startup wiring \+ mapper (complete)

* Build project — zero warnings check

## **Day 6 — QA \+ Docs \+ Release**

* Session 8: TestCaseEngineer → full test suite

* Session 9: DocumentEngineer → all 4 doc files

* Session 10: ReleaseEngineer → package \+ checklist

* Install on clean nopCommerce → smoke test all features

* All test cases pass ✅ → Ship 🚀

# **Section 12 — Quick Reference Card**

Print this page and keep it on your desk.

## **Every Session Opening Checklist**

| ✓ | Item |
| ----- | ----- |
| □ | MASTER-CONTEXT.md pasted |
| □ | MASTER-AGENT.md pasted |
| □ | AGENTS.md relevant section pasted |
| □ | Plugin CONTEXT.md pasted |
| □ | Plugin AGENT.md pasted |
| □ | Previous output files pasted (actual content, not description) |
| □ | Clear task instruction written |

## **Every Session Closing Checklist**

| ✓ | Item |
| ----- | ----- |
| □ | Output reviewed for obvious errors |
| □ | Files saved to correct project paths |
| □ | Full chat saved to \_sessions/ folder |
| □ | TASKS.md updated (status, files, handoff block pasted) |
| □ | Open questions answered before starting next session |

## **The Golden Rules**

| \# | Rule |
| ----- | ----- |
| 1 | Always paste ACTUAL files — never describe from memory. Agents hallucinate field names. |
| 2 | One agent \= one responsibility. Never ask ServiceEngineer to also write views. |
| 3 | Fresh session \= fresh context. Never assume the agent remembers anything. |
| 4 | YOU resolve blockers. Agents raise questions → you answer → agents proceed. |
| 5 | Save outputs to disk immediately. Do not rely on chat history. |
| 6 | Validate before handing off. Skim each output for obvious mistakes first. |
| 7 | TASKS.md is your source of truth. Update it every single session. |

