using Services = client_sql.Services;
using client_sql.Endpoints;

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

app.MapApiEndpoints();

app.Run();

public partial class Program { }
