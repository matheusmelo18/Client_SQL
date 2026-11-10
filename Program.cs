using Services = client_sql.Services;
using Models = client_sql.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<Services.AppDatabase>();
builder.Services.AddSingleton<Services.SqlQueryBuilder>(sp =>
{
    var db = sp.GetRequiredService<Services.AppDatabase>();
    var columns = db.GetSchema().Select(c => c.Name).ToList();
    return new Services.SqlQueryBuilder(columns);
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

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

app.Run();
