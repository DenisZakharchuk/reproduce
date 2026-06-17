# Copilot Workspace Instructions

## Workspace Purpose
This workspace is used to **reproduce software development tasks** from projects unreachable from the public internet. Each sub-folder corresponds to one reproduced project. Tasks include drafting fixes, refactorings, and new developments using Copilot.

## Workspace Layout

```
taskreproduce/
├── .github/
│   └── copilot-instructions.md   ← this file (global rules)
├── .vscode/
│   └── settings.json
├── ClientAPI/                    ← "API Service" project type
│   ├── .instructions.md          ← project-scoped Copilot context
│   ├── ClientAPI.sln
│   └── src/
│       └── ClientAPI/
│           └── ClientAPI.csproj
└── <NextProject>/                ← future projects follow the same pattern
```

Each project folder contains a `.instructions.md` file with project-specific context (namespaces, service registrations, config sections, known dependencies).

---

## Project Type: "API Service"

### Stack
- **Language:** C# / .NET (ASP.NET Core Web API)
- **Pattern:** MVC-style with dedicated service layer — NOT minimal-API
- **DI container:** Microsoft.Extensions.DependencyInjection (built-in)

### Solution Structure

```
<ProjectName>/
├── <ProjectName>.sln
└── src/
    └── <ProjectName>/
        ├── Controllers/           ← ASP.NET Core API controllers
        ├── Services/
        │   ├── I<Name>Service.cs  ← interface
        │   ├── <Name>Service.cs   ← implementation
        │   └── Integrations/      ← external system adapters / HTTP clients
        ├── Configuration/         ← Options classes (one per appsettings section)
        ├── Models/
        │   ├── Enums/
        │   └── Exceptions/
        ├── Program.cs
        ├── appsettings.json
        └── appsettings.Development.json
```

### Rules

#### Controllers
- Inherit from `ControllerBase`, decorated with `[ApiController]` and `[Route]`.
- **Each controller has exactly one associated service** injected via constructor.
- Controllers contain **no business logic** — delegate entirely to the service.
- Return `ActionResult<T>` or `IActionResult`.

#### Services
- **Interface + implementation pair** for every service (`IFooService` / `FooService`).
- Business logic lives here.
- Services may depend on other services, integration clients, and `IOptionsMonitor<T>`.

#### Configuration
- App settings sections are represented as **typed Options classes** in `Configuration/`.
- Injected using `IOptionsMonitor<TOptions>` (not `IOptions<T>` or raw `IConfiguration`).
- Registration pattern in `Program.cs`:
  ```csharp
  builder.Services.Configure<FooOptions>(builder.Configuration.GetSection("Foo"));
  ```
- Accessing in services:
  ```csharp
  public FooService(IOptionsMonitor<FooOptions> options) { ... }
  // Use: options.CurrentValue.SomeProperty
  ```

#### Models
- **Enums** go in `Models/Enums/` — each enum in its own file.
- **Custom exceptions** go in `Models/Exceptions/` — inherit from `Exception` or a custom base.
- Other DTOs / domain models go directly in `Models/`.

#### Dependency Registration
- All services registered in `Program.cs` (or extracted to extension methods for large projects).
- Prefer `AddScoped` for services, `AddSingleton` for options-backed singletons (if needed).

---

## How to Work on a Reproduction Task

1. Open the project sub-folder and read its `.instructions.md` for project-specific context.
2. The task description describes the bug / feature to reproduce.
3. Follow the "API Service" rules above when generating or modifying code.
4. Do not change the project's architectural conventions unless the task explicitly requires it.
