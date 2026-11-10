using Microsoft.Data.Sqlite;

namespace client_sql.Services;

public sealed class AppDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

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
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO produtos (nome, categoria, preco, estoque, disponivel) VALUES
                ('Notebook Dell Inspiron 15', 'Informática', 3499.90, 12, 1),
                ('Mouse Logitech G203', 'Informática', 149.90, 45, 1),
                ('Teclado Mecânico Redragon', 'Informática', 299.90, 8, 1),
                ('Monitor 24" Full HD', 'Informática', 899.00, 0, 1),
                ('Cadeira Ergonômica Office', 'Móveis', 1299.00, 6, 1),
                ('Mesa de Escritório 1.20m', 'Móveis', 549.00, 3, 0),
                ('Estante de Livros Madeira', 'Móveis', 429.00, 10, 1),
                ('Dom Casmurro - Machado de Assis', 'Livros', 39.90, 20, 1),
                ('Clean Code - Robert C. Martin', 'Livros', 129.00, 5, 1),
                ('Harry Potter e a Pedra Filosofal', 'Livros', 54.90, 15, 0),
                ('Controle DualSense PS5', 'Games', 449.90, 7, 1),
                ('Jogo FIFA 24 - PS5', 'Games', 249.00, 0, 1),
                ('Caneta Esferográfica Azul', 'Papelaria', 2.50, 100, 1),
                ('Caderno Espiral 100 folhas', 'Papelaria', 15.90, 30, 1),
                ('Grampeador de Mesa', 'Papelaria', 24.90, 18, 1);
                """;
            cmd.ExecuteNonQuery();
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

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> ExecuteQuery(string sql, IReadOnlyList<(string Name, object? Value)> parameters)
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

    public void Dispose() => _connection.Dispose();

    public sealed record ColumnInfo(string Name, string Type);
}
