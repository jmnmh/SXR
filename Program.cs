using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using System.Text.Json;

var builder = WebApplication.CreateBuilder();
var app = builder.Build();

// Serve static frontend (if hosted together)
app.UseStaticFiles(new StaticFileOptions {
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")),
    RequestPath = ""
});

// Simple endpoint to return list of tools (from JSON file)
app.MapGet("/api/tools", async () =>
{
    var json = await File.ReadAllTextAsync("data/files.json");
    return Results.Content(json, "application/json");
});

// Protected download redirect (increments counter)
app.MapGet("/api/download/{toolId}", async (string toolId, HttpContext ctx) =>
{
    var json = await File.ReadAllTextAsync("data/files.json");
    var list = JsonSerializer.Deserialize<List<Tool>>(json);
    var tool = list.FirstOrDefault(t => t.Id == toolId);
    if (tool == null) return Results.NotFound();
    // increment counter and save (simple)
    tool.Downloads++;
    await File.WriteAllTextAsync("data/files.json", JsonSerializer.Serialize(list));
    // log
    var logLine = $"{DateTime.UtcNow:o}\t{ctx.Connection.RemoteIpAddress}\t{toolId}\n";
    await File.AppendAllTextAsync("logs/downloads.log", logLine);
    // Return redirect to actual file (could be S3 or local)
    return Results.Redirect($"/files/{tool.FileName}");
});

app.Run();

record Tool(string Id, string Name, string Version, string Description, string FileName, int Downloads);
