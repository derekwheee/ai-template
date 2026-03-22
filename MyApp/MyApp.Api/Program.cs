using MyApp.Api.Extensions;
using MyApp.Api.Routes;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddApplicationServices();

var app = builder.Build();

app.MapDefaultEndpoints();
app.ApplyMigrations();
app.UseApplicationMiddleware();
app.MapAuthEndpoints();

app.Run();

public partial class Program { }
