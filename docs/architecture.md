# Architecture

Lazzy MCP follows **hexagonal architecture** (ports and adapters).
The rules that decide "what is yesterday?" or "how long did this review take?" live in the middle, the **hexagon**,
and know nothing about MCP, GitHub, Azure DevOps or YAML files.
Everything that talks to the outside world is an **adapter** plugged into a **port** the hexagon defines.

```text
        driving side                                         driven side
  (who calls Lazzy)                                     (who Lazzy calls)

  ┌──────────────────┐      ┌────────────────────────┐      ┌──────────────────────┐
  │ MCP client       │      │        hexagon         │      │ GitHub, Azure Boards │
  │ (Claude, VS Code)│      │                        │      │ demo data, ...       │
  └────────┬─────────┘      │  Lazzy.Application     │      └──────────▲───────────┘
           │                │   use cases, services  │                 │
  ┌────────▼─────────┐      │          │             │      ┌──────────┴───────────┐
  │ driving adapter  ├─────►│  driving + driven ports│◄─────┤ driven adapters      │
  │ Adapters.Mcp     │      │          │             │      │ Adapters.GitHub, ... │
  └──────────────────┘      │  Lazzy.Domain          │      └──────────────────────┘
                            │   model                │
                            └────────────────────────┘
```

## Projects

| Project | Folder | Hexagonal role | Contains | May reference |
|---|---|---|---|---|
| `Lazzy.Domain` | `src/Lazzy.Domain` | Inner hexagon: **domain model** | Value objects such as `TimeRange` and `BusinessCalendar` | nothing |
| `Lazzy.Application` | `src/Lazzy.Application` | Hexagon: **use cases, ports** and application services | `Ports/Driving` (one interface per use case), `Ports/Driven` (what providers implement), `UseCases`, `Periods/PeriodParser`, the configuration model | `Lazzy.Domain` |
| `Lazzy.Adapters.<Provider>` | `src/Adapters/Driven/` | **Driven (secondary) adapters** | One project per provider: `Demo` today, `GitHub` and `AzureDevOps` next | `Lazzy.Domain` and the driven ports of `Lazzy.Application` |
| `Lazzy.Adapters.Mcp` | `src/Adapters/Driving/` | **Driving (primary) adapter** and **composition root** | MCP tools and prompts that translate protocol calls into driving port calls; `Program.cs` wires every adapter to its port | everything, but only `Program.cs` uses use cases and driven adapters |

Test projects mirror this: `Lazzy.Domain.Tests`, `Lazzy.Application.Tests`, `Lazzy.Adapters.Contract`
(the contract suite every driven adapter must pass) and `Lazzy.Architecture.Tests` (the dependency rules below).

## Vocabulary

- **Hexagon (core):** `Lazzy.Domain` + `Lazzy.Application`. Pure .NET, no I/O, testable with a fake clock.
- **Port:** an interface owned by the hexagon.
  - **Driving port** (primary, inbound): what the outside world can ask Lazzy to do, such as "get my activity for a period".
    Lives in `Lazzy.Application/Ports/Driving`.
  - **Driven port** (secondary, outbound): what Lazzy needs from the outside world, such as "list the pull requests in this repository".
    Lives in `Lazzy.Application/Ports/Driven`, named by need (`IActivitySource`), never by technology.
    A provider implements it without seeing the use cases.
- **Adapter:** code that connects one technology to one port.
  - **Driving adapter:** turns an external request into a call to a driving port. The MCP server is one; a CLI could be another.
  - **Driven adapter:** implements a driven port against a real system. Every provider (GitHub, Azure Boards, Demo) is one.
- **Composition root:** the single place that chooses which adapter backs each port (for example, the Demo adapter when `--demo` is set).
  It is `Program.cs` in `Lazzy.Adapters.Mcp`.

## Dependency rule

Dependencies always point **inward**: adapters depend on the hexagon, never the other way around.
`Lazzy.Architecture.Tests` enforces these rules on every build; if one fails, fix the dependency, not the test.

- `Lazzy.Domain` and `Lazzy.Application` never reference an adapter, MCP, an HTTP client or a configuration format.
- A driven adapter references `Lazzy.Domain` and only the driven ports of `Lazzy.Application`.
  It never sees another adapter, the use cases, the driving ports or the MCP adapter.
- MCP tools and prompts depend on driving ports. Only the composition root knows every concrete type.
- Time is a port too: the hexagon receives a `TimeProvider` instead of reading `DateTime.Now`, and the composition root registers the system clock.

## Adding a provider

1. Create `src/Adapters/Driven/Lazzy.Adapters.<Name>`, referencing `Lazzy.Domain` and `Lazzy.Application`
   (for the driven ports only).
2. Implement the driven ports that provider supports (version control, task board, or both).
3. Register it in the composition root.
4. Reference it from `Lazzy.Architecture.Tests`, so the dependency rules inspect it.
5. Make it pass `Lazzy.Adapters.Contract`.
