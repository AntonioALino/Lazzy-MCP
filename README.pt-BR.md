# Lazzy MCP

**Português** · [English](README.md)

**Seja preguiçoso com a daily, não com o seu time.**
O Lazzy MCP monta a sua daily e as métricas do seu projeto direto dos seus boards e repositórios, dentro de qualquer cliente MCP.
Somente leitura. Local. Neutro em relação a fornecedores.

> **Status: inicial (v0.1).** A daily funciona com GitHub e Azure Boards. As métricas de projeto vêm a seguir.
> Espere mudanças incompatíveis até a versão 1.0.

---

## Sumário

- [Por quê](#por-quê)
- [Princípios](#princípios)
- [O que ele faz hoje](#o-que-ele-faz-hoje)
- [Início rápido](#início-rápido)
- [Configuração](#configuração)
  - [Separando projetos de trabalho e pessoais](#separando-projetos-de-trabalho-e-pessoais)
- [O que o Lazzy MCP não faz](#o-que-o-lazzy-mcp-não-faz)
- [Roadmap](#roadmap)
- [Como contribuir](#como-contribuir)
- [Licença](#licença)

---

## Por quê

Toda manhã alguém abre o board, passa pelos commits e pull requests de ontem e tenta lembrar o que fez.
Todo mês um gestor exporta dados para uma planilha para responder "quantos PRs mergeamos e quanto tempo os reviews levaram?".

O Lazzy MCP é um servidor [MCP](https://modelcontextprotocol.io) que lê o seu provedor git e o seu board e responde essas perguntas no assistente de IA que você já usa: Claude, VS Code, Cursor ou qualquer outro cliente MCP.

```text
Você:   /daily
Claude: Desde sexta
        • O PR #87 "Adicionar Pix como forma de pagamento" foi mergeado; a task 1201 foi para Ready to Test.
        • Comecei a task 1207 "Mostrar prazo de entrega no checkout" e abri o PR #89.
        • Revisei o PR #88 "Atualizar para .NET 10".
        Hoje: continuar o PR #89 (ainda aberto). Tem algo te bloqueando?
```

## Princípios

São decisões de design deliberadas, não limitações temporárias:

- **Somente leitura.** O Lazzy nunca move um card, comenta, faz push ou merge. Ele só pede escopos de leitura.
- **Local, sem telemetria.** Ele roda na sua máquina. Seu código, seu board e seus tokens nunca passam por um servidor nosso.
- **Seu token, suas permissões.** Cada pessoa usa o próprio token, então o Lazzy só enxerga o que aquela pessoa já enxerga no provedor. O controle de acesso é herdado, não reinventado.
- **Times, não rankings.** As métricas são agregadas por projeto. O Lazzy não ranqueia pessoas por quantidade de PRs ou commits: esses números punem quem revisa, faz mentoria e investiga.
- **Neutro.** Qualquer provedor git, qualquer board, qualquer cliente MCP. Provedores são adapters pequenos que qualquer pessoa pode adicionar.
- **Os números vêm do código, não do modelo.** As tools calculam toda contagem e toda duração; o modelo de linguagem só explica. LLMs erram contas, e um gestor que pega um número errado deixa de confiar na ferramenta.

## O que ele faz hoje

| Tipo | Nome | O que faz |
|---|---|---|
| Prompt | `daily` | Prepara a sua daily: o que você fez, o que está fazendo e bloqueios. Aparece como `/daily` nos clientes que suportam prompts MCP. |
| Tool | `get_activity` | Seus commits, PRs abertos/mergeados/revisados e mudanças de estado de work items em um período (`yesterday`, `today`, `7d`, `2026-10-01`). |
| Tool | `list_projects` | Os projetos configurados, com seus apelidos e onde eles vivem. |

"Ontem" significa **o dia útil anterior** no seu fuso horário: numa segunda-feira ele volta até a sexta, e os feriados configurados são pulados.

### Provedores suportados

| Provedor | Papel | Status |
|---|---|---|
| GitHub | Controle de versão | ✅ v0.1 |
| Azure Boards | Gestão de tarefas | ✅ v0.1 |
| Demo (dados fictícios) | Ambos | ✅ v0.1 |
| Jira | Gestão de tarefas | Planejado (v0.4) |
| Azure Repos, GitLab | Controle de versão | Contribuições bem-vindas |

## Início rápido

Requer o [SDK do .NET 10](https://dotnet.microsoft.com/download).

### 1. Teste com dados fictícios (sem token)

```bash
git clone https://github.com/AntonioALino/lazzy-mcp
cd lazzy-mcp
dotnet build
```

Depois registre o servidor no seu cliente MCP em modo demo.

**Claude Code**

```bash
claude mcp add lazzy -- dotnet run --project /caminho/para/lazzy-mcp/src/Lazzy.Server -- --demo
```

**Claude Desktop** (`claude_desktop_config.json`)

```json
{
  "mcpServers": {
    "lazzy": {
      "command": "dotnet",
      "args": ["run", "--project", "/caminho/para/lazzy-mcp/src/Lazzy.Server", "--", "--demo"]
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
      "args": ["run", "--project", "/caminho/para/lazzy-mcp/src/Lazzy.Server", "--", "--demo"]
    }
  }
}
```

Pergunte *"o que eu fiz desde ontem?"* ou rode `/daily`.

### 2. Use com seus projetos reais

1. Copie o [`lazzy.example.yaml`](lazzy.example.yaml) para `lazzy.yaml` e descreva seus projetos (veja [Configuração](#configuração)).
2. Crie tokens somente leitura e exponha-os como variáveis de ambiente:

   | Variável | Como criar |
   |---|---|
   | `GITHUB_TOKEN` | [docs/providers/github.md](docs/providers/github.md) |
   | `AZURE_DEVOPS_PAT` | [docs/providers/azure-devops.md](docs/providers/azure-devops.md) |

3. Registre o servidor sem o `--demo`:

   ```bash
   claude mcp add lazzy \
     -e GITHUB_TOKEN=... -e AZURE_DEVOPS_PAT=... -e LAZZY_CONFIG=/caminho/para/lazzy.yaml \
     -- dotnet run --project /caminho/para/lazzy-mcp/src/Lazzy.Server
   ```

### Instalando como .NET tool

Enquanto o pacote não é publicado no NuGet, você pode instalar a partir do código-fonte:

```bash
dotnet pack src/Lazzy.Server -o ./nupkg
dotnet tool install -g Lazzy.Mcp --add-source ./nupkg
```

A partir daí, o comando no cliente passa a ser simplesmente `lazzy-mcp` (adicione `--demo` para testar).

## Configuração

```yaml
timeZone: America/Sao_Paulo        # id IANA; define o que é "ontem"
holidays: [2026-11-02, 2026-11-20] # dias não úteis, yyyy-MM-dd

identity:                          # quem é "eu" em cada provedor
  github: seu-login-no-github
  ado: voce@empresa.com            # unique name no Azure DevOps, normalmente o e-mail de login

projects:
  - name: Checkout
    aliases: [checkout, pagamentos] # nomes que as pessoas realmente digitam
    targets:                        # onde o projeto vive: provedor:local
      - github:minha-org/checkout-api # owner/repo
      - github:minha-org/checkout-web
      - ado:minha-org/Checkout        # organização/projeto
```

Esse arquivo **não contém segredos** e pode ser commitado e compartilhado com o time. Os tokens são sempre lidos de variáveis de ambiente.

O Lazzy procura a configuração nesta ordem:

1. o argumento `--config <caminho>`;
2. a variável de ambiente `LAZZY_CONFIG`;
3. `./lazzy.yaml` no diretório atual;
4. `~/.config/lazzy/lazzy.yaml`.

O `--demo` (ou `LAZZY_DEMO=1`) ignora tudo isso e usa um projeto fictício embutido.

### Separando projetos de trabalho e pessoais

O Lazzy nunca busca "tudo o que você fez". Toda consulta é presa a um target listado na sua configuração: as chamadas ao GitHub são feitas no endpoint do próprio repositório ou filtradas com `repo:owner/repo`, e as consultas ao Azure Boards ficam restritas ao projeto. Um repositório que não está na configuração não existe para o Lazzy, mesmo que você use a mesma conta do GitHub no trabalho e nos projetos pessoais.

A mistura só pode acontecer em uma situação: projetos de trabalho e pessoais no **mesmo** arquivo de configuração, e o `/daily` executado **sem informar o projeto**, o que significa "todos os projetos configurados".

**Recomendado até a chegada dos workspaces na v0.2: um arquivo de configuração por contexto**, registrados como dois servidores separados.

```bash
claude mcp add lazzy-trabalho \
  -e GITHUB_TOKEN=<token do trabalho> -e AZURE_DEVOPS_PAT=<PAT do trabalho> -e LAZZY_CONFIG=/caminho/para/lazzy.trabalho.yaml \
  -- dotnet run --project /caminho/para/lazzy-mcp/src/Lazzy.Server

claude mcp add lazzy-pessoal \
  -e GITHUB_TOKEN=<token pessoal> -e LAZZY_CONFIG=/caminho/para/lazzy.pessoal.yaml \
  -- dotnet run --project /caminho/para/lazzy-mcp/src/Lazzy.Server
```

Cada servidor conhece apenas os próprios projetos e os próprios tokens, então os dois contextos nunca se encontram. Os clientes MCP separam tools e prompts pelo nome do servidor, então cada contexto ganha o seu próprio comando de daily (no Claude Code, por exemplo, `/mcp__lazzy-trabalho__daily`).

Duas proteções extras:

- **Informe o projeto** quando um mesmo arquivo tiver vários: "faça a daily do Checkout".
- **Use tokens fine-grained restritos aos repositórios daquele contexto.** Mesmo que um repositório seja colocado no arquivo errado por engano, o Lazzy não consegue lê-lo e só mostra um aviso de "sem acesso".

> **Chegando na v0.2: workspaces.** Um único arquivo vai poder conter os dois contextos, com o `/daily` usando um deles como padrão. O acompanhamento está no milestone da v0.2.

## O que o Lazzy MCP não faz

- **Não escreve nada** no seu board ou nos seus repositórios.
- **Não ranqueia pessoas.** Perguntas como "quem fez menos PRs?" estão fora do escopo por design.
- **Não vê tudo.** Reuniões, pair programming, investigações e outros trabalhos fora do código são invisíveis para ele; o prompt `/daily` lembra você de adicioná-los.
- **Não envia dados para lugar nenhum**, exceto para as APIs dos provedores que você configurou.

## Roadmap

- **v0.1:** `get_activity`, `/daily`, GitHub, Azure Boards, modo demo ← *você está aqui*
- **v0.2:** métricas de projeto para gestores: `get_pr_metrics` (volume, tempo até o primeiro review, tempo até o merge, sempre comparados com o período anterior) e `list_prs` para detalhamento; **workspaces**, para que um único arquivo de configuração mantenha projetos de trabalho e pessoais separados
- **v0.3:** `get_flow_metrics` (cycle time, throughput, retrabalho), `get_blockers`, `get_review_load`
- **v0.4:** adapter do Jira
- **Depois:** cache local em SQLite para períodos longos, distribuição via NuGet e Docker, adapters da comunidade

As definições das métricas ficam em [docs/metrics.md](docs/metrics.md), para que todo número possa ser conferido.

## Como contribuir

Novos provedores são a contribuição mais valiosa. Um adapter é uma classe que depende apenas de `Lazzy.Abstractions`, e a suíte de testes de contrato diz quando ele está pronto. Veja o [CONTRIBUTING.md](CONTRIBUTING.md).

## Licença

[MIT](LICENSE)
