using Microsoft.Data.Sqlite;

namespace client_sql.Services;

public sealed class AppDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly object _gate = new();
    public AppDatabase(Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
    {
        var dbPath = System.IO.Path.Combine(env.ContentRootPath, "app.db");
        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();
        EnsureSchema();
        SeedIfEmpty();
    }

    private void EnsureSchema()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS produtos (
                id INTEGER PRIMARY KEY,
                nome TEXT NOT NULL,
                categoria TEXT NOT NULL,
                preco REAL NOT NULL,
                estoque INTEGER NOT NULL,
                disponivel INTEGER NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    private void SeedIfEmpty()
    {
        using var countCmd = _connection.CreateCommand();
        countCmd.CommandText = "SELECT COUNT(*) FROM produtos;";
        var count = Convert.ToInt64(countCmd.ExecuteScalar());
        if (count > 0) return;

        using var tx = _connection.BeginTransaction();
        try
        {
            using var cmd = _connection.CreateCommand();
            foreach (var p in SeedData.Produtos)
            {
                cmd.CommandText = "INSERT INTO produtos (nome, categoria, preco, estoque, disponivel) VALUES (@nome, @categoria, @preco, @estoque, @disponivel);";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@nome", p.Nome);
                cmd.Parameters.AddWithValue("@categoria", p.Categoria);
                cmd.Parameters.AddWithValue("@preco", p.Preco);
                cmd.Parameters.AddWithValue("@estoque", p.Estoque);
                cmd.Parameters.AddWithValue("@disponivel", p.Disponivel);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public IReadOnlyList<ColumnInfo> GetSchema()
    {
        lock (_gate)
        {
            var columns = new List<ColumnInfo>();
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "PRAGMA table_info(produtos);";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                columns.Add(new ColumnInfo(reader.GetString(1), reader.GetString(2)));
            }
            return columns;
        }
    }

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> ExecuteQuery(string sql, IReadOnlyList<(string Name, object? Value)> parameters)
    {
        lock (_gate)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            for (var i = 0; i < parameters.Count; i++)
            {
                cmd.Parameters.AddWithValue(parameters[i].Name, parameters[i].Value ?? DBNull.Value);
            }

            var rows = new List<Dictionary<string, object?>>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                rows.Add(row);
            }
            return rows;
        }
    }

    public void Dispose() => _connection.Dispose();

    public sealed record ColumnInfo(string Name, string Type);
}
