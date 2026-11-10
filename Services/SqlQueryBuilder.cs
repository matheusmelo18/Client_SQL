using System.Text;

namespace client_sql.Services;

public sealed class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

public sealed class SqlQueryBuilder
{
    private static readonly string[] AllowedOperators =
    {
        "=", "!=", "<", "<=", ">", ">=", "LIKE", "NOT LIKE", "IS NULL", "IS NOT NULL"
    };

    private static readonly string[] AllowedConjunctions = { "AND", "OR" };
    private static readonly string[] AllowedDirections = { "ASC", "DESC" };

    private readonly IReadOnlyList<string> _allowedColumns;

    public SqlQueryBuilder(IReadOnlyList<string> allowedColumns)
    {
        _allowedColumns = allowedColumns;
    }

    public (string Sql, IReadOnlyList<(string Name, object? Value)> Parameters) Build(Models.QueryRequest request)
    {
        var select = BuildSelect(request.Select);
        var where = BuildWhere(request.Where, out var whereParameters);
        var orderBy = BuildOrderBy(request.OrderBy);
        var limit = BuildLimit(request.Limit);

        var sql = new StringBuilder("SELECT ");
        sql.Append(select);
        sql.Append(" FROM produtos");
        if (where.Length > 0)
        {
            sql.Append(" WHERE ");
            sql.Append(where);
        }
        if (orderBy.Length > 0)
        {
            sql.Append(" ORDER BY ");
            sql.Append(orderBy);
        }
        sql.Append(" LIMIT ");
        sql.Append(limit);

        return (sql.ToString(), whereParameters);
    }

    private string BuildSelect(IReadOnlyList<string>? columns)
    {
        if (columns is null || columns.Count == 0)
            return "*";

        var sb = new StringBuilder();
        for (var i = 0; i < columns.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            ValidateColumn(columns[i]);
            sb.Append(QuoteIdentifier(columns[i]));
        }
        return sb.ToString();
    }

    private string BuildWhere(IReadOnlyList<Models.WhereCondition>? conditions, out IReadOnlyList<(string Name, object? Value)> parameters)
    {
        var sb = new StringBuilder();
        var paramList = new List<(string Name, object? Value)>();
        parameters = paramList;
        if (conditions is null || conditions.Count == 0) return sb.ToString();

        var paramIndex = 0;
        for (var i = 0; i < conditions.Count; i++)
        {
            var c = conditions[i];
            ValidateColumn(c.Column);
            ValidateOperator(c.Operator);

            if (i > 0)
            {
                ValidateConjunction(c.Conjunction);
                sb.Append(' ');
                sb.Append(c.Conjunction.ToUpperInvariant());
                sb.Append(' ');
            }

            var isNullOp = c.Operator.Equals("IS NULL", StringComparison.OrdinalIgnoreCase) ||
                           c.Operator.Equals("IS NOT NULL", StringComparison.OrdinalIgnoreCase);

            sb.Append(QuoteIdentifier(c.Column));
            sb.Append(' ');
            sb.Append(c.Operator.ToUpperInvariant());

            if (!isNullOp)
            {
                if (c.Value is null)
                    throw new ValidationException($"O operador '{c.Operator}' requer um valor para a coluna '{c.Column}'.");
                var paramName = $"@p{paramIndex++}";
                sb.Append(' ');
                sb.Append(paramName);
                paramList.Add((paramName, c.Value));
            }
        }
        return sb.ToString();
    }

    private string BuildOrderBy(IReadOnlyList<Models.OrderByItem>? items)
    {
        if (items is null || items.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            var item = items[i];
            ValidateColumn(item.Column);
            ValidateDirection(item.Direction);
            sb.Append(QuoteIdentifier(item.Column));
            sb.Append(' ');
            sb.Append(item.Direction.ToUpperInvariant());
        }
        return sb.ToString();
    }

    private static int BuildLimit(int? limit)
    {
        var value = limit ?? 100;
        if (value < 1 || value > 1000)
            throw new ValidationException("O limite de linhas deve estar entre 1 e 1000.");
        return value;
    }

    private void ValidateColumn(string column)
    {
        if (string.IsNullOrWhiteSpace(column) || !_allowedColumns.Contains(column))
            throw new ValidationException($"Coluna inválida ou não permitida: '{column}'.");
    }

    private void ValidateOperator(string @operator)
    {
        if (string.IsNullOrWhiteSpace(@operator) ||
            !AllowedOperators.Contains(@operator, StringComparer.OrdinalIgnoreCase))
            throw new ValidationException($"Operador inválido: '{@operator}'.");
    }

    private void ValidateConjunction(string conjunction)
    {
        if (string.IsNullOrWhiteSpace(conjunction) ||
            !AllowedConjunctions.Contains(conjunction, StringComparer.OrdinalIgnoreCase))
            throw new ValidationException($"Conjunção inválida: '{conjunction}'.");
    }

    private void ValidateDirection(string direction)
    {
        if (string.IsNullOrWhiteSpace(direction) ||
            !AllowedDirections.Contains(direction, StringComparer.OrdinalIgnoreCase))
            throw new ValidationException($"Direção inválida: '{direction}'.");
    }

    private static string QuoteIdentifier(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"")}\"";
    }
}
