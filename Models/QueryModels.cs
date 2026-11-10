namespace client_sql.Models;

public sealed record QueryRequest(
    IReadOnlyList<string>? Select,
    IReadOnlyList<WhereCondition>? Where,
    IReadOnlyList<OrderByItem>? OrderBy,
    int? Limit);

public sealed record WhereCondition(
    string Column,
    string Operator,
    string? Value,
    string Conjunction);

public sealed record OrderByItem(
    string Column,
    string Direction);

public sealed record QueryResponse(
    string Sql,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows,
    int RowCount);
