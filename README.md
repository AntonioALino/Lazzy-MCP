# Lazzy MCP

[Português](README.pt-BR.md) · **English**

**Be lazy about your stand-up, not about your team.**
Lazzy MCP builds your daily stand-up and your project metrics straight from your boards and repositories, inside any MCP client.
Read-only. Local-first. Vendor-neutral.

> **Status: early (v0.1).** The daily stand-up works with GitHub and Azure Boards. Project metrics are next.
> Expect breaking changes until 1.0.

---

## Table of contents

- [Why](#why)
- [Principles](#principles)
- [What it can do today](#what-it-can-do-today)
- [Quick start](#quick-start)
- [Configuration](#configuration)
  - [Keeping work and personal projects apart](#keeping-work-and-personal-projects-apart)
- [What Lazzy MCP does not do](#what-lazzy-mcp-does-not-do)
- [Roadmap](#roadmap)
- [Contributing](#contributing)
- [License](#license)

---

## Why

Every morning, someone opens the board, scrolls through yesterday's commits and pull requests, and tries to remember what they did.
Every month, a lead exports data into a spreadsheet to answer "how many PRs did we merge, and how long did reviews take?".

Lazzy MCP is an [MCP](https://modelcontextprotocol.io) server that reads your git host and your board and answers those questions in the AI assistant you already use: Claude, VS Code, Cursor or any other MCP client.

```text
You:    /daily
Claude: Since Friday
        • PR #87 "Add Pix as a payment method" was merged; task 1201 moved to Ready to Test.
        • Started task 1207 "Show estimated delivery date" and opened PR #89.
        • Reviewed PR #88 "Upgrade to .NET 10".
        Today: continue PR #89 (still open). Anything blocking you?
```

## Principles

These are deliberate design decisions, not temporary limitations:

- **Read-only.** Lazzy never moves a card, comments, pushes or merges. It only asks for read scopes.
- **Local-first, no telemetry.** It runs on your machine. Your code, your board and your tokens never pass through a server of ours.
- **Your token, your permissions.** Each person uses their own token, so Lazzy can only see what that person can already see in the provider. Access control is inherited, not reinvented.
- **Teams, not leaderboards.** Metrics are aggregated per project. Lazzy will not rank people by number of PRs or commits: those numbers punish the people who review, mentor and investigate.
- **Vendor-neutral.** Any git host, any board, any MCP client. Providers are small adapters anyone can add.
- **The numbers come from code, not from the model.** Tools compute every count and duration; the language model only explains them. LLMs are bad at counting, and a lead who catches one wrong number stops trusting the tool.

## What it can do today

| Kind | Name | What it does |
|---|---|---|
| Prompt | `daily` | Prepares your stand-up: what you did, what you're doing, blockers. Shows up as `/daily` in clients that support MCP prompts. |
| Tool | `get_activity` | Your commits, PRs opened/merged/reviewed and work item state changes in a period (`yesterday`, `today`, `7d`, `2026-10-01`). |
| Tool | `list_projects` | The projects you configured, with their aliases and where they live. |

"Yesterday" means **the previous business day** in your time zone: on Monday it reaches back to Friday, and configured holidays are skipped.

### Supported providers

| Provider | Role | Status |
|---|---|---|
| GitHub | Source control | ✅ v0.1 |
| Azure Boards | Work tracking | ✅ v0.1 |
| Demo (fake data) | Both | ✅ v0.1 |
| Jira | Work tracking | Planned (v0.4) |
| Azure Repos, GitLab | Source control | Help wanted |

## Quick start

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

### 1. Try it with fake data (no tokens needed)

```bash
git clone https://github.com/AntonioALino/lazzy-mcp
cd lazzy-mcp
dotnet build
```

Then register it in your MCP client in demo mode.

**Claude Code**

```bash
claude mcp add lazzy -- dotnet run --project /path/to/lazzy-mcp/src/Lazzy.Server -- --demo
```

**Claude Desktop** (`claude_desktop_config.json`)

```json
{
  "mcpServers": {
    "lazzy": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/lazzy-mcp/src/Lazzy.Server", "--", "--demo"]
    }
  }
}
```

**VS Code** (`.vscode/mcp.json`)

```json
{
  "servers": {
    "lazzy": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/lazzy-mcp/src/Lazzy.Server", "--", "--demo"]
    }
  }
}
```

Ask *"what did I do since yesterday?"* or run `/daily`.

### 2. Use it with your real projects

1. Copy [`lazzy.example.yaml`](lazzy.example.yaml) to `lazzy.yaml` and describe your projects (see [Configuration](#configuration)).
2. Create read-only tokens and expose them as environment variables:

   | Variable | How to create it |
   |---|---|
   | `GITHUB_TOKEN` | [docs/providers/github.md](docs/providers/github.md) |
   | `AZURE_DEVOPS_PAT` | [docs/providers/azure-devops.md](docs/providers/azure-devops.md) |

3. Register the server without `--demo`:

   ```bash
   claude mcp add lazzy \
     -e GITHUB_TOKEN=... -e AZURE_DEVOPS_PAT=... -e LAZZY_CONFIG=/path/to/lazzy.yaml \
     -- dotnet run --project /path/to/lazzy-mcp/src/Lazzy.Server
   ```

### Installing as a .NET tool

Until the package is published on NuGet, you can install it from source:

```bash
dotnet pack src/Lazzy.Server -o ./nupkg
dotnet tool install -g Lazzy.Mcp --add-source ./nupkg
```

The client command then becomes simply `lazzy-mcp` (add `--demo` to try it).

## Configuration

```yaml
timeZone: America/Sao_Paulo        # IANA id; decides what "yesterday" means
holidays: [2026-11-02, 2026-11-20] # non-working days, yyyy-MM-dd

identity:                          # who "me" is on each provider
  github: your-github-login
  ado: you@company.com             # Azure DevOps unique name, usually your sign-in email

projects:
  - name: Checkout
    aliases: [checkout, payments]  # names people actually type
    targets:                       # where the project lives: provider:location
      - github:my-org/checkout-api # owner/repo
      - github:my-org/checkout-web
      - ado:my-org/Checkout        # organization/project
```

This file holds **no secrets** and can be committed and shared with your team. Tokens are always read from environment variables.

Lazzy looks for the configuration in this order:

1. the `--config <path>` argument;
2. the `LAZZY_CONFIG` environment variable;
3. `./lazzy.yaml` in the current directory;
4. `~/.config/lazzy/lazzy.yaml`.

`--demo` (or `LAZZY_DEMO=1`) ignores all of the above and uses a built-in fake project.

### Keeping work and personal projects apart

Lazzy never searches "everything you did". Every query is bound to a target listed in your configuration: GitHub calls are made on the repository's own endpoint or filtered with `repo:owner/repo`, and Azure Boards queries are scoped to the project. A repository that is not in your configuration does not exist for Lazzy, even if you use the same GitHub account for work and personal projects.

Mixing can happen in only one situation: work and personal projects in the **same** configuration file, and `/daily` run **without a project**, which means "all configured projects".

**Recommended until workspaces arrive in v0.2: one configuration file per context**, registered as two separate servers.

```bash
claude mcp add lazzy-work \
  -e GITHUB_TOKEN=<work token> -e AZURE_DEVOPS_PAT=<work PAT> -e LAZZY_CONFIG=/path/to/lazzy.work.yaml \
  -- dotnet run --project /path/to/lazzy-mcp/src/Lazzy.Server

claude mcp add lazzy-personal \
  -e GITHUB_TOKEN=<personal token> -e LAZZY_CONFIG=/path/to/lazzy.personal.yaml \
  -- dotnet run --project /path/to/lazzy-mcp/src/Lazzy.Server
```

Each server knows only its own projects and its own tokens, so the two contexts never meet. MCP clients namespace tools and prompts by server name, so each context gets its own daily command (in Claude Code, for example, `/mcp__lazzy-work__daily`).

Two extra safeguards:

- **Name the project** when a single file holds several: "run the daily for Checkout".
- **Use fine-grained tokens restricted to that context's repositories.** Even if a repository is added to the wrong file by mistake, Lazzy cannot read it and only reports a "no access" warning.

> **Coming in v0.2: workspaces.** A single file will be able to hold both contexts, with `/daily` defaulting to one of them. It is tracked in the v0.2 milestone.

## What Lazzy MCP does not do

- **It does not write anything** to your board or repositories.
- **It does not rank people.** Questions like "who made the fewest PRs?" are out of scope by design.
- **It does not see everything.** Meetings, pairing, investigations and other non-code work are invisible to it; the `/daily` prompt reminds you to add them.
- **It does not send data anywhere** except to the APIs of the providers you configured.

## Roadmap

- **v0.1:** `get_activity`, `/daily`, GitHub, Azure Boards, demo mode ← *you are here*
- **v0.2:** project metrics for leads: `get_pr_metrics` (volume, time to first review, time to merge, always compared with the previous period) and `list_prs` for drill-down; **workspaces**, so a single configuration file can keep work and personal projects apart
- **v0.3:** `get_flow_metrics` (cycle time, throughput, rework), `get_blockers`, `get_review_load`
- **v0.4:** Jira adapter
- **Later:** local SQLite cache for long periods, NuGet and Docker distribution, community adapters

Metric definitions live in [docs/metrics.md](docs/metrics.md) so every number can be checked.

## Contributing

New providers are the most valuable contribution. An adapter is one class that depends only on `Lazzy.Abstractions`, and the contract test suite tells you when it is done. See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE)
