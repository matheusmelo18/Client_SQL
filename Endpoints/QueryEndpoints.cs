using Models = client_sql.Models;
using Services = client_sql.Services;

namespace client_sql.Endpoints;

public static class QueryEndpoints
{
    public static void MapApiEndpoints(this WebApplication app)
    {
        app.MapGet("/api/schema", (Services.AppDatabase db) => new
        {
            table = "produtos",
            columns = db.GetSchema()
        });

        app.MapPost("/api/query", async (Services.AppDatabase db, Services.SqlQueryBuilder queryBuilder, Models.QueryRequest request) =>
        {
            if (request is null)
                return Results.BadRequest(new { error = "Corpo da requisição inválido." });

            try
            {
                var (sql, parameters) = queryBuilder.Build(request);
                var rows = db.ExecuteQuery(sql, parameters);
                return Results.Ok(new Models.QueryResponse(sql, rows, rows.Count));
            }
            catch (Services.ValidationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });
    }
}
