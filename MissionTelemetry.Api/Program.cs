using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MissionTelemetry.Api.Repositories;
using MissionTelemetry.Core.Services;
using MissionTelemetry.Persistence;
using MissionTelemetry.Persistence.Entities;
using Microsoft.Extensions.Options;
using MissionTelemetry.Api.Options;
using System.Runtime.InteropServices;


var builder = WebApplication.CreateBuilder(args);

// ---------- Services ----------
builder.Services.AddOpenApi();          // OpenAPI JSON 
builder.Services.AddControllers();


builder.Services.AddCors(o => o.AddPolicy("AllowSameOrigin",
    p => p
        .AllowAnyHeader()
        .AllowAnyMethod()
        .SetIsOriginAllowed(origin => true) //  dev: alles erlauben
        .AllowCredentials()
));

//  für schnellere Auslieferung von Web-Assets
builder.Services.AddResponseCompression(opts =>
{
    opts.EnableForHttps = true;
    opts.Providers.Add<GzipCompressionProvider>();
    opts.Providers.Add<BrotliCompressionProvider>();
});

//  Repos / Evaluator / Manager / Quellen / Worker 
builder.Services.AddScoped<ITelemetryRepository>(sp =>
{
    var opt = sp.GetRequiredService<IOptions<TelemetryOptions>>().Value;
    if (opt.UseEfRepository)
        return ActivatorUtilities.CreateInstance<EfTelemetryRepository>(sp);   // scoped EF
    else
        return sp.GetRequiredService<InMemoryTelemetryRepository>();           // Singleton Memory
});

builder.Services.AddSingleton<InMemoryTelemetryRepository>();
builder.Services.AddSingleton<IProximityRepository, InMemoryProximityRepository>();
builder.Services.AddSingleton<IAlarmReadModel, AlarmReadModelAdapter>();

builder.Services.AddSingleton<IAlarmEvaluator>(sp =>
{
    var path = Path.Combine(AppContext.BaseDirectory, "mission_dict.json");
    var dict = new JsonDictionaryLoader().LoadFromFile(path);
    return new DataDrivenAlarmEvaluator(dict);
});
builder.Services.AddSingleton<IAlarmManager, AlarmManager>();

builder.Services.AddSingleton<IProximitySource>(_ => new SimulatedProximitySource(1.0));

builder.Services.AddSingleton<ITelemtrySource>(_ => new SimulatedTelemetrySource(1.0));
builder.Services.AddHostedService<MissionTelemetry.Api.Services.SimulationWorker>();

builder.Services.AddDbContext<MissionDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("MissionDb");
    opt.UseSqlite(cs);
});

builder.Services.Configure<TelemetryOptions>(
    builder.Configuration.GetSection("Telemetry"));
    

//builder.Services.AddDbContextFactory<MissionDbContext>(opt =>
//opt.UseInMemoryDatabase("MissionDb"));


var app = builder.Build();

using(var scope = app.Services.CreateScope())
{
    var db = scope .ServiceProvider.GetRequiredService<MissionTelemetry.Persistence.MissionDbContext>();
    db.Database.Migrate();
}

// Middleware-Pipeline 
app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseCors("AllowSameOrigin");          

// Statische Website ausliefern (wwwroot)

app.UseDefaultFiles();
app.UseStaticFiles();


app.MapOpenApi();    // /openapi/v1.json

app.MapGet("/status", () => Results.Text("OK - API up", "text/plain"));                   

// schlanke Doku-UI 
app.MapGet("/docs", () =>
{
    var html = """
    <!doctype html>
    <html>
    <head>
      <meta charset="utf-8"/>
      <title>MissionTelemetry API Docs</title>
      <meta name="viewport" content="width=device-width, initial-scale=1">
      <style>html,body{height:100%;margin:0}</style>
    </head>
    <body>
      <redoc spec-url="/openapi/v1.json"></redoc>
      <script src="https://cdn.redoc.ly/redoc/latest/bundles/redoc.standalone.js"></script>
    </body>
    </html>
    """;
    return Results.Content(html, "text/html");
});

// API-Controller
app.MapControllers();

//  Fallback f�r SPA-Routing (wenn du clientseitige Routen nutzt)
app.MapFallbackToFile("index.html");

app.Run();
