using Xunit;
using System.Net;
using System.Text.Json;
using client_sql;
using client_sql.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace client_sql.Tests;

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private static async Task<T> ReadJson<T>(HttpResponseMessage resp)
    {
        var body = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    [Fact]
    public async Task GetSchemaReturnsProdutosWithSixColumns()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/schema");
        resp.EnsureSuccessStatusCode();

        var body = await ReadJson<JsonElement>(resp);
        Assert.Equal("produtos", body.GetProperty("table").GetString());
        Assert.Equal(6, body.GetProperty("columns").GetArrayLength());
    }

    [Fact]
    public async Task PostQueryEmptyBodyReturnsAllRows()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsync("/api/query", new StringContent("{}", null, "application/json"));
        resp.EnsureSuccessStatusCode();

        var body = await ReadJson<QueryResponse>(resp);
        Assert.Equal(15, body.Rows.Count);
        Assert.Contains("LIMIT 100", body.Sql);
    }

    [Fact]
    public async Task WhereCategoriaLivrosReturnsThreeRows()
    {
        var client = _factory.CreateClient();
        var json = """
            {
              "where": [
                { "column": "categoria", "operator": "=", "value": "Livros", "conjunction": "AND" }
              ]
            }
            """;
        var resp = await client.PostAsync("/api/query", new StringContent(json, null, "application/json"));
        resp.EnsureSuccessStatusCode();

        var body = await ReadJson<QueryResponse>(resp);
        Assert.Equal(3, body.Rows.Count);
        Assert.All(body.Rows, r => Assert.Equal("Livros", r["categoria"]?.ToString()));
    }

    [Fact]
    public async Task OrderByPrecoDescFirstIsMax()
    {
        var client = _factory.CreateClient();
        var json = """
            {
              "orderBy": [
                { "column": "preco", "direction": "DESC" }
              ]
            }
            """;
        var resp = await client.PostAsync("/api/query", new StringContent(json, null, "application/json"));
        resp.EnsureSuccessStatusCode();

        var body = await ReadJson<QueryResponse>(resp);
        Assert.Equal(3499.90, ((JsonElement)body.Rows[0]["preco"]).GetDouble());
    }
    [Fact]
    public async Task LimitTwoReturnsTwoRows()
    {
        var client = _factory.CreateClient();
        var json = """{ "limit": 2 }""";
        var resp = await client.PostAsync("/api/query", new StringContent(json, null, "application/json"));
        resp.EnsureSuccessStatusCode();

        var body = await ReadJson<QueryResponse>(resp);
        Assert.Equal(2, body.Rows.Count);
        Assert.Equal(2, body.RowCount);
    }

    [Fact]
    public async Task SelectTwoColumnsReturnsOnlyThoseKeys()
    {
        var client = _factory.CreateClient();
        var json = """{ "select": ["nome", "preco"] }""";
        var resp = await client.PostAsync("/api/query", new StringContent(json, null, "application/json"));
        resp.EnsureSuccessStatusCode();

        var body = await ReadJson<QueryResponse>(resp);
        Assert.Equal(15, body.Rows.Count);
        Assert.All(body.Rows, r => Assert.Equal(new HashSet<string> { "nome", "preco" }, new HashSet<string>(r.Keys)));
    }

    [Fact]
    public async Task InvalidColumnReturns400()
    {
        var client = _factory.CreateClient();
        var json = """{ "select": ["nome\"; DROP TABLE produtos;--"] }""";
        var resp = await client.PostAsync("/api/query", new StringContent(json, null, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);

        var body = await ReadJson<JsonElement>(resp);
        Assert.True(body.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task InvalidOperatorReturns400()
    {
        var client = _factory.CreateClient();
        var json = """{ "where": [{ "column": "nome", "operator": "===", "value": "x", "conjunction": "AND" }] }""";
        var resp = await client.PostAsync("/api/query", new StringContent(json, null, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetRootServesIndexHtml()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/");
        resp.EnsureSuccessStatusCode();

        var html = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Gerador de Queries SQL", html);
    }
}
