using Xunit;
using client_sql.Services;
using Models = client_sql.Models;

namespace client_sql.Tests;

public class SqlQueryBuilderTests
{
    private static readonly string[] Columns = ["id", "nome", "categoria", "preco", "estoque", "disponivel"];

    private static SqlQueryBuilder Builder() => new(Columns);

    [Fact]
    public void SelectEmptyReturnsStar()
    {
        var b = Builder();
        var (sql, parameters) = b.Build(new Models.QueryRequest(null, null, null, null));
        Assert.Equal("SELECT * FROM produtos LIMIT 100", sql);
        Assert.Empty(parameters);
    }

    [Fact]
    public void SelectValidQuotesColumns()
    {
        var b = Builder();
        var (sql, parameters) = b.Build(new Models.QueryRequest(["nome", "preco"], null, null, null));
        Assert.Equal("SELECT \"nome\", \"preco\" FROM produtos LIMIT 100", sql);
        Assert.Empty(parameters);
    }

    [Fact]
    public void InvalidColumnThrows()
    {
        var b = Builder();
        var ex = Assert.Throws<ValidationException>(() =>
            b.Build(new Models.QueryRequest(["nome\"; DROP TABLE produtos;--"], null, null, null)));
        Assert.Contains("Coluna inválida", ex.Message);
    }

    [Fact]
    public void InvalidOperatorThrows()
    {
        var b = Builder();
        var ex = Assert.Throws<ValidationException>(() =>
            b.Build(new Models.QueryRequest(null, [new Models.WhereCondition("nome", "===", "x", "AND")], null, null)));
        Assert.Contains("Operador inválido", ex.Message);
    }

    [Fact]
    public void IsNullWithoutValueHasNoParameter()
    {
        var b = Builder();
        var (sql, parameters) = b.Build(new Models.QueryRequest(null,
            [new Models.WhereCondition("estoque", "IS NULL", null, "AND")], null, null));
        Assert.Equal("SELECT * FROM produtos WHERE \"estoque\" IS NULL LIMIT 100", sql);
        Assert.Empty(parameters);
    }

    [Fact]
    public void NormalOperatorWithNullValueThrows()
    {
        var b = Builder();
        var ex = Assert.Throws<ValidationException>(() =>
            b.Build(new Models.QueryRequest(null,
                [new Models.WhereCondition("nome", "=", null, "AND")], null, null)));
        Assert.Contains("requer um valor", ex.Message);
    }

    [Fact]
    public void TwoConditionsUseConjunctionAndParameters()
    {
        var b = Builder();
        var (sql, parameters) = b.Build(new Models.QueryRequest(null,
            [
                new Models.WhereCondition("categoria", "=", "Livros", "AND"),
                new Models.WhereCondition("preco", ">", "100", "OR")
            ], null, null));
        Assert.Equal("SELECT * FROM produtos WHERE \"categoria\" = @p0 OR \"preco\" > @p1 LIMIT 100", sql);
        Assert.Equal(["@p0", "@p1"], parameters.Select(p => p.Name).ToList());
        Assert.Equal("Livros", parameters[0].Value);
        Assert.Equal("100", parameters[1].Value);
    }

    [Fact]
    public void OrderByValid()
    {
        var b = Builder();
        var (sql, _) = b.Build(new Models.QueryRequest(null, null,
            [new Models.OrderByItem("preco", "DESC")], null));
        Assert.Contains("ORDER BY \"preco\" DESC", sql);
    }

    [Fact]
    public void InvalidDirectionThrows()
    {
        var b = Builder();
        var ex = Assert.Throws<ValidationException>(() =>
            b.Build(new Models.QueryRequest(null, null,
                [new Models.OrderByItem("preco", "INVALID")], null)));
        Assert.Contains("Direção inválida", ex.Message);
    }

    [Fact]
    public void LimitNullDefaultsTo100()
    {
        var b = Builder();
        var (sql, _) = b.Build(new Models.QueryRequest(null, null, null, null));
        Assert.EndsWith("LIMIT 100", sql);
    }

    [Fact]
    public void LimitZeroThrows()
    {
        var b = Builder();
        var ex = Assert.Throws<ValidationException>(() =>
            b.Build(new Models.QueryRequest(null, null, null, 0)));
        Assert.Contains("entre 1 e 1000", ex.Message);
    }

    [Fact]
    public void Limit1001Throws()
    {
        var b = Builder();
        var ex = Assert.Throws<ValidationException>(() =>
            b.Build(new Models.QueryRequest(null, null, null, 1001)));
        Assert.Contains("entre 1 e 1000", ex.Message);
    }

    [Fact]
    public void MaliciousValueGoesAsParameter()
    {
        var b = Builder();
        var (sql, parameters) = b.Build(new Models.QueryRequest(null,
            [new Models.WhereCondition("nome", "=", "' OR 1=1 --", "AND")], null, null));
        Assert.DoesNotContain("OR 1=1", sql);
        Assert.Single(parameters);
        Assert.Equal("' OR 1=1 --", parameters[0].Value);
    }
}
