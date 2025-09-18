using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Ad Platforms API", Version = "v1" });
});

var app = builder.Build();

// In-memory хранилище площадок
var platforms = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

// Метод загрузки площадок (устойчивый)
app.MapPost("/upload", (string content) =>
{
    try
    {
        var newPlatforms = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var parts = line.Split(':', 2);
            if (parts.Length != 2) continue; // пропускаем некорректные строки

            var name = parts[0].Trim();
            if (string.IsNullOrEmpty(name)) continue;

            var locations = parts[1]
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (locations.Count > 0)
                newPlatforms[name] = locations;
        }

        platforms.Clear();
        foreach (var kvp in newPlatforms)
            platforms[kvp.Key] = kvp.Value;

        return Results.Ok("Data uploaded successfully");
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error parsing data: {ex.Message}");
    }
})
.Accepts<string>("text/plain")
.WithName("UploadPlatforms");

// Метод поиска площадок
app.MapPost("/search", (SearchRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Location))
        return Results.BadRequest("Location is required");

    var location = request.Location.Trim();
    var result = new List<string>();

    foreach (var kvp in platforms)
    {
        if (kvp.Value.Any(loc => location.StartsWith(loc, StringComparison.OrdinalIgnoreCase)))
            result.Add(kvp.Key);
    }

    return Results.Ok(result);
})
.Accepts<SearchRequest>("application/json")
.WithName("SearchPlatforms");

// GET для проверки содержимого (отладка)
app.MapGet("/platforms", () => platforms)
   .WithName("GetPlatforms");

// Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();
