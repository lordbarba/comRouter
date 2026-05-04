using CommRouter.Core;
using CommRouter.Core.Settings;
using CommRouter.WebServer.Hubs;
using CommRouter.WebServer.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── Services ────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddSingleton<RouterService>();
builder.Services.AddSingleton<PluginLoader>();
builder.Services.AddSingleton<JsonSettingsSerializer>();
builder.Services.AddSingleton<XmlMigrationReader>();
builder.Services.AddSingleton<AppSettings>();
builder.Services.AddHostedService<RouterHostedService>();

var app = builder.Build();

// ─── Pipeline ────────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.MapControllers();
app.MapHub<RouterHub>("/hubs/router");
app.MapFallbackToFile("index.html");

app.Run();
