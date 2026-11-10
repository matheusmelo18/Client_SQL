# client_sql

Gerador e executor de queries SELECT sobre a tabela `produtos` em SQLite (`app.db`). Aplicação .NET 10 (Minimal API) com frontend estático em `wwwroot/`. Contrato externo (endpoints, JSON, tabela, UI em pt-BR) é estável.

## Como rodar

Na raiz do projeto:

```
dotnet run
```

A aplicação sobe em `http://localhost:5296` (conforme `Properties/launchSettings.json`). Abra o navegador nesse endereço para usar a interface, ou chame os endpoints diretamente com `curl`.

## Arquitetura

- `Program.cs` — bootstrap: cria o `WebApplication`, registra `AppDatabase` e `SqlQueryBuilder` no DI, serve arquivos estáticos e mapeia os endpoints.
- `Endpoints/QueryEndpoints.cs` — `MapGet("/api/schema")` e `MapPost("/api/query")` (mesmos handlers e mensagens de erro do `Program.cs` original).
- `Services/SqlQueryBuilder.cs` — gera SQL parametrizado a partir do `QueryRequest` (allowlist de colunas, operadores, conjunções e direções; valores via `@pN`; identificadores com `"` escapado; LIMIT padrão 100, máximo 1000).
- `Services/AppDatabase.cs` — executa a query no SQLite (`app.db`), com `lock (_gate)` em `GetSchema()` e `ExecuteQuery()` porque o serviço é singleton e `SqliteConnection` não é thread-safe.
- `Services/SeedData.cs` — os 15 produtos usados na seed idempotente (só insere se a tabela estiver vazia).
- `Models/QueryModels.cs` — contratos `QueryRequest`, `WhereCondition`, `OrderByItem`, `QueryResponse`.
- `wwwroot/` — frontend estático (`index.html`, `style.css`, `app.js`).

## Contrato da API

### GET /api/schema

Resposta:

```json
{
  "table": "produtos",
  "columns": [
    { "name": "id", "type": "INTEGER" },
    { "name": "nome", "type": "TEXT" },
    { "name": "categoria", "type": "TEXT" },
    { "name": "preco", "type": "REAL" },
    { "name": "estoque", "type": "INTEGER" },
    { "name": "disponivel", "type": "INTEGER" }
  ]
}
```

Exemplo:

```bash
curl -s http://localhost:5296/api/schema
```

### POST /api/query

Corpo (`QueryRequest`):

```json
{
  "select": ["nome", "preco"],
  "where": [
    { "column": "categoria", "operator": "=", "value": "Livros", "conjunction": "AND" }
  ],
  "orderBy": [
    { "column": "preco", "direction": "DESC" }
  ],
  "limit": 2
}
```

Resposta (`QueryResponse`):

```json
{
  "sql": "SELECT \"nome\", \"preco\" FROM produtos WHERE \"categoria\" = @p0 ORDER BY \"preco\" DESC LIMIT 2",
  "rows": [ { "nome": "...", "preco": 3499.9 } ],
  "rowCount": 1
}
```

Exemplo:

```bash
curl -s -X POST http://localhost:5296/api/query \
  -H 'Content-Type: application/json' \
  -d '{"where":[{"column":"categoria","operator":"=","value":"Livros","conjunction":"AND"}],"orderBy":[{"column":"preco","direction":"DESC"}],"limit":2}'
```

Campos opcionais: `select` (padrão `*`), `where`, `orderBy`, `limit` (padrão 100, entre 1 e 1000).

## Regras de segurança

- Colunas, operadores, conjunções e direções passam por allowlist; qualquer valor fora dela gera `400` com mensagem em pt-BR.
- Valores de filtro vão sempre como parâmetros parametrizados (`@pN`), nunca embutidos no SQL — injeção via valor é inofensiva.
- Identificadores de coluna são entre aspas duplas com `"` escapado.
- `LIMIT` limitado a 1–1000.

## Decisão do lock no AppDatabase

`AppDatabase` é registrado como singleton e compartilha uma única `SqliteConnection` entre requisições concorrentes. `SqliteConnection` não é thread-safe, então `GetSchema()` e `ExecuteQuery()` envolvem o corpo em `lock (_gate)`. SQLite já serializa gravações internamente, então o lock não altera semântica, apenas torna o acesso seguro.

## Testes

```
dotnet test
```

Executa `SqlQueryBuilderTests` (unit, sem banco) e `ApiTests` (integração HTTP real com `WebApplicationFactory<Program>`).

## Estrutura de pastas

```
client_sql/
  Program.cs
  Endpoints/QueryEndpoints.cs
  Services/AppDatabase.cs
  Services/SqlQueryBuilder.cs
  Services/SeedData.cs
  Models/QueryModels.cs
  wwwroot/index.html
  wwwroot/style.css
  wwwroot/app.js
  client_sql.Tests/
    SqlQueryBuilderTests.cs
    ApiTests.cs
  client_sql.sln
  client_sql.csproj
  README.md
```
