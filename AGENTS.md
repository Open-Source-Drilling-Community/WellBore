<!-- Canonical source: Open-Source-Drilling-Community/Microservice-Agent-Guidance. Synchronised copies should not be edited directly. -->

# DigiWells Repository Guidance

## Scope

These instructions govern OSDC/DigiWells microservice repositories. When this file is synchronised into a repository, a more deeply nested `AGENTS.md` may add narrower guidance for its directory tree. If it is loaded globally while working outside an OSDC repository, apply only its general safe-working rules; do not infer that the unrelated project follows the OSDC architecture.

This common guidance does not govern `DotNetLibraries`, `PipeMovementReconstruction`, `Simulator4nDOF`, `StatisticalInformationDistanceBetweenTwoTrajectories`, or `YPLCalibrationFromRheometer`. If a global or ancestor instruction loader presents this file while working in one of those repositories, ignore the remaining project-specific instructions in this document.

## Working Principles

- Inspect the target repository, its README files, solution structure, and existing implementation before editing.
- Follow established patterns in the sibling OSDC microservices when adding a capability shared by Field, Cluster, Well, WellBore, Rig, WellBore Architecture, Survey Instrument, Trajectory, or similar services.
- Preserve unrelated and pre-existing working-tree changes. Never discard or rewrite user work merely to simplify a change.
- Make the smallest coherent change that completes the feature across model, service, generated contract, client, UI, tests, deployment assets, and documentation where those layers are affected.
- Prefer evidence from source code and generated schemas over assumptions based on tool descriptions or old documentation.

## Naming and Service Identity

- New and migrated code uses the `OSDC.Drilling.*` identity. Do not introduce new `NORCE.Drilling.*` namespaces, packages, image names, routes, or workflow references.
- During an identity migration, search the entire repository, including source, project files, solution files, generated inputs, GitHub Actions, Dockerfiles, Helm charts, configuration, and documentation.
- Keep assembly names, root namespaces, NuGet references, Docker image names, Kubernetes resources, and public routes consistent with one another.
- Identity migrations must not add compatibility namespaces, routes, services, Helm aliases, forwarding packages, or other legacy identities unless the user explicitly authorizes a specific compatibility requirement. Document every authorized exception and its removal condition.

## Domain Models and Shared Types

- Treat the appropriate OSDC NuGet package as the source of truth for shared drilling-domain types. Do not redefine package-owned types locally merely to work around generation or serialization issues.
- Before adding a local class or enum, verify that it is not already supplied by an OSDC package or another authoritative shared contract.
- Keep local model projects focused on microservice-owned DTOs, persistence extensions, catalog assignments, lightweight projections, statistics, and batch contracts.
- Use SI units in persisted and wire-level engineering data unless the established contract explicitly says otherwise. State units clearly in schemas and documentation. For OSDC depth data, persist and expose SI metres relative to WGS84; MSL, RKB, wellhead, ground-level, mud-line, and other depth references are presentation-layer transformations that must be converted back to WGS84 before saving.
- Do not rely only on property-name heuristics to describe public engineering quantities. Define explicit physical-quantity and SI-unit metadata for ambiguous properties, then verify that the model, REST/OpenAPI and MCP schemas, generated clients, and Web UI all agree. Display-unit conversion must never alter the canonical stored value.
- Do not describe every coordinate representation as if it had one universal axis order. OSDC local drilling coordinates normally follow North-East-Depth, projected CRS values use their explicit easting/northing contract, and geographic values use latitude/longitude plus the documented vertical reference. Verify the model and calculation implementation, then keep REST schemas, MCP schemas/descriptions, web labels, Home pages, and READMEs consistent with that representation.
- Model mutually incompatible variants as genuinely enforced discriminated unions, not prose-only conventions. Reject fields that do not belong to the selected variant.
- Prefer finite enums for standardized vocabularies. Do not use unrestricted strings where the domain vocabulary is known and closed.

## Identity and Feature Catalogs

- Use the common identity/feature-definition and per-resource-assignment pattern already established across the OSDC resource microservices.
- Keep catalog definitions separate from assignments. Validate referenced definition and option UUIDs before persistence.
- Enforce exclusivity and validity-period rules consistently in every resource type that uses the shared catalog.
- Prevent deletion of definitions that remain referenced, unless an explicit, safe migration workflow exists.
- Keep defaults and seeded definitions deterministic and stable so upgrades do not create duplicates or invalidate existing assignments.

## Persistence and Upgrade Safety

- A microservice should normally use one durable database and one transaction boundary for its related entities. Introduce another database only for a clear operational or ownership reason.
- Kubernetes upgrades must preserve existing persistent volumes and data. Do not replace stable storage paths, claims, database filenames, table names, or serialized shapes without an explicit migration.
- Preserve compatibility with deployed data and published, supported contracts. Pre-release or explicitly retired legacy shapes may be removed when the user confirms they are no longer relevant. Any incompatible change to retained data must have a tested migration.
- Do not solve compatibility problems by deleting databases or clearing persistent volumes.
- Multi-entity writes, imports, and restores must be atomic: validate the complete request first, then commit all changes in one transaction or make no changes.
- Preserve caller-generated UUIDs where the existing resource pattern depends on them.
- Durable-data migrations belong to service startup or an explicit administrative operation, never to WebApp initialization. Make them idempotent and transactional, fail closed on missing or ambiguous reviewed mappings, and log a clear summary of examined and changed records. Take and verify an independent backup before enabling a destructive or identity-rewriting migration.
- Treat usage statistics as durable operational state when sibling services do so. Store their JSON history under the same persistent `/home` volume, load it during startup, initialize counters added by newer versions, and save periodically and during graceful shutdown where practical. Use atomic file replacement so interruption cannot leave a partially written history file; persistence failure must be logged without taking the domain API down.

## Backup and Restore

- Follow the schema-versioned JSON backup/restore pattern used by the established resource microservices.
- Export every dependency needed to restore the selected resources. For dependent resources, either include their required parents automatically or reject an incomplete selection clearly.
- Validate document format and version, UUID uniqueness, domain semantics, references, catalog compatibility, and conflicts before writing anything.
- Support explicit catalog handling when catalog definitions are included:
  - `MapExisting` requires every exact catalog UUID to exist locally.
  - `MapOrCreateMissing` creates absent definitions while rejecting different content at an existing UUID.
- Support explicit resource UUID conflict handling such as `FailIfExists` and `ReplaceExisting`.
- Treat exact catalog UUID matching as the safe default. When creating a missing definition, preserve its source UUID. Never map different UUIDs by normalized name unless the caller explicitly opts in, and reject ambiguous or semantically incompatible matches.
- Keep restoration all-or-nothing and report structured, position-aware validation errors.
- In restore UIs, ask about catalog-definition handling before asking about resource UUID conflicts, matching the other microservices.
- Embedded engineering snapshots must remain frozen when auditability requires it. Catalog/template edits must not silently rewrite historical resource data.

## REST, OpenAPI, Generated Models, and Clients

- The service implementation and its OpenAPI output are authoritative. Generated client/model files are derived artifacts.
- Do not permanently hand-edit NSwag-generated C# merely to repair a contract. Fix the controller/model/schema or generator, then regenerate.
- Dependency schemas are normally taken directly from each dependency repository under `<microservice>\Service\wwwroot\json-schema\*.json`. Use the repository's actual generator workflow rather than inventing parallel DTOs.
- When present, run the `ModelSharedIn` and/or `ModelSharedOut` programs after refreshing their JSON schema inputs. Inspect each repository's README and generator before running it because inputs and interactive overwrite behavior vary.
- After any public contract change, update and verify all applicable artifacts:
  - service OpenAPI/schema output
  - merged JSON schema
  - generated C# client/model
  - consuming WebPages/WebApp code
  - service and contract tests
- Check generated output for duplicate short type names. Resolve collisions at the schema merge/type-source level rather than maintaining competing local definitions.
- Keep JSON naming and serialization behavior consistent with the established contract. Avoid unplanned casing changes because they break generated clients and LLM callers.
- If generated collections are edited or index-bound by Razor consumers, configure the generator to emit a mutable collection type such as `List<T>` consistently for properties, instances, and responses. Do not patch generated collection types afterward.

## Shared Package Upgrades

- When an OSDC NuGet package version changes, search every repository below `C:\OSDC` for references in `*.csproj`, `Directory.Packages.props`, lock files, generators, and documentation; do not assume only the package's own microservice consumes it.
- Update all intended consumers to the same explicitly requested version, restore them, and build representative or affected solutions. Preserve unrelated package edits already present in those working trees.
- For reusable `*.WebPages` packages, verify that consuming hosts still register required services, configuration interfaces, static assets, and routed assemblies after the upgrade. A successful restore alone does not prove runtime compatibility.
- When packing a Razor class library, ensure its static-web-assets manifest has been generated: either build before `dotnet pack --no-build` or allow `dotnet pack` to perform the required build. Do not use `--no-restore` or `--no-build` unless the workflow has produced every prerequisite artifact for the same configuration and target framework.
- Where a package family uses generated catalog source (for example, physical-quantity enumerations), treat that source as generated output: change the authoritative catalog definitions first, regenerate with the repository's generator, and inspect the resulting diff. Use local project references for the restore, generation, build, and test sequence; otherwise a restore can select an older published package and conceal newly added types.
- For a multi-package release, align the requested version in package projects and inter-package references, then pack in dependency order. Inspect each `.nupkg` manifest before publication to confirm its package ID, version, and internal dependency versions are exactly the intended ones. Do not publish projects explicitly marked non-packable or documented as local-only.
- Publishing to a public feed is an external state change and requires explicit authorization. Take the package API key only from the environment or approved secret store; never print, log, or place it in repository files. Publish packages in dependency order and distinguish a feed's upload acceptance from public-index availability, which can be asynchronous.

## MCP Contracts

- Keep MCP behavior aligned with the REST/domain contract, including validation, concurrency, backup/restore policies, and error semantics.
- Do not accept caller-controlled values for facts that are deterministically derived from submitted mappings or catalog data. Validate every referenced identifier and relationship on the server, derive such facts there, and return the computed values in successful mutation responses.
- Give every tool a precise description, strict input schema, explicit output schema, and accurate safety annotations.
- Reject unknown top-level arguments when the tool contract is intended to be closed.
- Encode discriminators, required properties, forbidden variant fields, enums, and nullability in the schema itself; descriptions alone are insufficient.
- Return stable, sanitized error envelopes for validation, not-found, conflict, stale-write, and unexpected failures.
- Use optimistic concurrency on mutating calls. Require the latest modification timestamp or version token where the resource supports it.
- Treat an optimistic-concurrency timestamp or version as an opaque token copied exactly from the latest read. Do not parse, normalize, truncate, or reformat it. Apply the same protection to destructive deletes, and return the updated resource after granular mutations so callers receive the next token.
- Provide payload-conscious discovery operations such as IDs, metadata, and light records before an unrestricted full-list operation.
- For large aggregates, give nested objects stable UUIDs such as `ComponentID` instead of addressing them only by array position. Provide focused add, update, delete, and reorder tools for frequently edited collections; require reorder requests to contain every existing component exactly once and preserve documented domain ordering.
- Keep full-replacement operations only where useful and document their overwrite risk. Prefer granular mutations for routine edits so callers cannot accidentally drop unrelated nested data.
- Clearly identify references owned by other microservices. When synchronous write-time validation would create tight coupling, provide separate single-record validation and deterministic bounded audit tools. Distinguish missing references from unavailable dependencies, never classify dependency failure as invalid data, and explicitly allow unlinked drafts when the relationship is optional.
- When changing MCP tools, update registration descriptions, argument schemas, transport/integration tests, and relevant README sections together.

## Web UI

- Match the established MudBlazor look and interaction patterns of the sibling resource microservices.
- Keep reusable pages in `WebPages` and host/deployment concerns in `WebApp` when that split exists.
- Keep route declarations, navigation links, path bases, ingress paths, and configured service URLs aligned. Search for both old namespaces and old route casing during migrations.
- Keep dependency URLs in the established `appsettings.Development.json` and `appsettings.Production.json` sections and expose them through the same configuration interfaces used by sibling services. Verify local source execution, in-cluster DNS names, public `PathBase`, ingress rewriting, route casing, and navigation URLs as one route contract.
- Avoid ambiguous Blazor routes contributed by referenced Razor assemblies. A reusable package must not accidentally claim a host's root or generic `/Home` route.
- Register `AddHttpClient()` whenever local or imported pages require `IHttpClientFactory`. If imported assemblies still contribute conflicting routes, expose the required calculator or feature through local wrapper pages and include only assemblies needed for route discovery.
- Initialize nullable assignment and nested collection properties before rendering or mutation; Razor markup must not assume deserialized legacy records contain newly introduced lists.
- In reflection-driven or polymorphic editors, key components by selected model/node, declaring type, and property identity rather than relying on render position. Test switching between similarly shaped panels: Blazor component reuse can otherwise retain stale labels, quantities, units, validation state, or values.
- Apply the shared unit and depth-reference selection to every depth input in both simplified and detailed editors. Labels, displayed values, and values converted back for persistence must change together.
- Use the OSDC unit-conversion components for every unit-bearing UI value, including angles, lengths, depths, elevations, accuracies, and uncertainties. Persist and submit the canonical SI value, and convert both the displayed value and its unit label from the selected unit system; do not leave isolated raw numeric fields for quantities such as meridian longitude or ensemble accuracy.
- Test navigation beneath the deployed `PathBase`, not only from the application root.
- Give dialogs a practical maximum width and responsive behavior; do not let restore dialogs expand to the full viewport unnecessarily.
- External calculation or contextual-data calls should fail gracefully when optional. A failure to calculate derived reference data should not crash an otherwise usable edit page.
- Keep usage-statistics pages visually consistent across resource services: a compact title and persistence-aware subtitle, icon-bearing refresh action, linear loading indicator, explicit error state, responsive summary metrics, and one dense, hoverable, bordered, striped, sortable endpoint table. Show HTTP methods as consistently colored outlined chips and add a functional-area column when a service has several endpoint families. Derive rows from all public `History` properties ending in `PerDay` so newly added counters are not silently omitted; decide deliberately whether daily and last-used data belong in that service rather than copying or removing them incidentally.
- A standalone WebApp should normally provide a host-owned `/Home` page and put it first in navigation. Keep it operational and service-specific: explain the owned data, important conventions, persistence, primary workflows, and REST/MCP/web access paths with direct links. Do not put a generic `/Home` route in a reusable `WebPages` assembly, because consuming hosts may load several such assemblies and create route conflicts.

## C# Style

- Respect the style of the project being edited. Do not perform unrelated whole-file formatting.
- Keep nullable reference types enabled and address nullability intentionally.
- Prefer file-scoped namespaces in new C# files when consistent with the surrounding project.
- Use descriptive domain names and append `Async` to asynchronous methods.
- Use `System.Text.Json` where the service already standardizes on it.
- Keep controllers thin; put persistence, validation, catalog, and batch orchestration in focused services or managers.
- Prefer structured outcomes and stable error codes over exception-driven control flow for expected API failures.
- Add comments for non-obvious constraints and compatibility decisions, not for code that is already self-explanatory.

## Testing and Verification

- Build or test an explicit solution/project path; repository roots may contain multiple project or solution files in CI environments.
- Typical commands, adjusted to the repository's actual solution name, are:

  ```powershell
  dotnet restore .\<Solution>.sln
  dotnet build .\<Solution>.sln --no-restore
  dotnet test .\<Solution>.sln --no-build
  ```

- Add focused tests for every bug fix and contract rule. Include success, invalid input, not found, conflicts, concurrency, serialization, and transactional rollback as relevant.
- For generated-client failures, inspect the actual HTTP status and response body from the service before changing the tests.
- Verify both local in-process behavior and behavior against configured deployed routes when the defect is environment-dependent.
- Before running out-of-process integration tests, inspect their configured base URL and launch the service with the matching profile, port, protocol, and development certificate. Classify connection refusal, HTTP-to-HTTPS handshake errors, unavailable dependencies, and persistent test-data conflicts as environment/setup failures unless source evidence shows a regression.
- Give destructive integration tests an isolated test database or test-specific `home` directory. Never clear a developer, deployed, or shared persistent database merely to make a test repeatable.
- Run `git diff --check` and inspect `git status --short` before handing off.
- Report tests that were run and any verification that could not be completed.
- If endpoint security flags an internally built artifact, do not disable, evade, or suppress the protection. Record the executable, configuration, command, hash, and source provenance; prefer a non-production generator configuration only when the generated source is configuration-independent, and involve the organization's security team for an allow-list or verdict.

## Documentation

- Update every first-party README affected by a change: root overview, model, generator/shared model, service, tests, WebPages, WebApp, and deployment documentation as applicable.
- Do not edit vendored or third-party README/license files to describe project behavior.
- Document source-of-truth ownership, generation steps, routes, persistence implications, backup semantics, and deployment prerequisites when they change.
- Keep README statements synchronized with the actual schema and implementation, especially tool counts, routes, supported policies, and concurrency requirements.
- Treat a WebApp Home page as user-facing documentation. Whenever service behavior, coordinate/unit conventions, persistence, routes, or access methods change, check the Home page together with every affected first-party README.

## Deployment

- Do not deploy merely because source changes are complete; deployment requires explicit user authorization and published images.
- Keep GitHub Actions, Docker build contexts, project/solution paths, image repositories, Helm charts, routes, and namespaces synchronized.
- Docker restore layers must copy every referenced project, central build file, NuGet configuration, and required lock file before restore. Ensure repository-only files such as nested `packages.lock.json` files are not emitted twice to the same publish path. Validate both service and WebApp Dockerfiles after project-reference or package changes.
- Publish the mutable tag expected by operations, such as `stable`, together with an immutable version or commit-SHA tag. Confirm the Deployment's effective image and `imagePullPolicy`; a rollout restart alone does not prove that a mutable tag was pulled.
- Use the intended Kubernetes context explicitly. Deploy and verify development before production and AWE unless the user directs otherwise.
- Use the correct context option for each CLI: `kubectl --context <name>` and `helm --kube-context <name>`.
- A rollout restart reuses the image reference already present in the Deployment. It retrieves a mutable tag such as `stable` only when the effective `imagePullPolicy` requires a pull; prefer immutable version or commit-SHA tags when reproducibility matters.
- Confirm that PVC bindings and database paths remain unchanged before an upgrade that touches persistence.
- For SQLite-backed services, use a `Recreate` deployment strategy and confirm the old pod has fully terminated before the replacement mounts the database. Never allow overlapping writers to the same SQLite file.
- Before a Helm identity cutover, save `helm get values --all`, the current manifest, workload/service/ingress identities, and PVC metadata. Treat service and WebApp releases separately. If a stable PVC is owned by the legacy release, deliberately transfer its Helm ownership annotations and labels while preserving the claim name, volume binding, mount path, and database filename; do not let a new chart recreate it.
- Do not assume the active Kubernetes identity may patch `deployments/scale`. If scaling is forbidden, use an authorized Helm upgrade or another permitted rollout mechanism and verify pod termination directly. `helm template` does not support `--reuse-values`; render with a previously exported values file instead.
- Before a persistence-sensitive migration, verify both an application-level export and a recoverable PVC/database snapshot. Do not rely on an untested backup.
- If a rollout remains stuck with an old replica pending termination, inspect pod status and events, termination state, rollout strategy, and volume attachment before considering forced deletion.
- After deployment, verify health plus representative create/read/update flows and affected WebApp routes.
- Service launch profiles and startup settings should not open Swagger, a WebApp, or another browser page unless that behavior is explicitly requested.

## Completion Checklist

Before declaring a cross-layer change complete, check the applicable items:

1. Domain source of truth and local model ownership are correct.
2. Existing data and upgrade paths are safe.
3. REST and MCP contracts enforce the same rules.
4. OpenAPI inputs and generated outputs are synchronized.
5. UI routes, configuration, and workflows match the service.
6. Automated tests cover the new behavior and regressions.
7. Relevant first-party READMEs are current.
8. CI, Docker, and Helm references use the current OSDC identity.
9. The solution builds, tests pass, and the diff has been inspected.
