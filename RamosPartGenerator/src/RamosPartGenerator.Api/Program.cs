using RamosPartGenerator.Api.Contracts;
using RamosPartGenerator.Api.Services;
using RamosPartGenerator.Core.Models;
using RamosPartGenerator.Core.Services;
using RamosPartGenerator.Excel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddSingleton(provider =>
{
    var specDirectory = Path.Combine(AppContext.BaseDirectory, "specs");
    var specProvider = new SpecProvider(specDirectory);
    specProvider.Load();
    return specProvider;
});
builder.Services.AddSingleton<ProductTextService>();
builder.Services.AddSingleton<IncomingCompService>();
builder.Services.AddSingleton<ModuleService>();
builder.Services.AddSingleton<RegistrationExcelExporter>();
builder.Services.AddSingleton<LookupCatalog>();

var app = builder.Build();

app.UseCors();

app.MapGet("/", () => Results.Ok(new
{
    name = "RamosPartGenerator.Api",
    status = "ok",
    endpoints = new[]
    {
        "/api/meta/revisions",
        "/api/lookups/incoming/{revision}",
        "/api/lookups/module/{revision}",
        "/api/incoming-comp/preview",
        "/api/incoming-comp/parse",
        "/api/module/preview",
        "/api/module/parse-comp",
        "/api/module/parse-full",
        "/api/export/registration"
    }
}));

app.MapGet("/api/meta/revisions", (LookupCatalog lookups) =>
{
    return Results.Ok(lookups.GetRevisions());
});

app.MapGet("/api/lookups/incoming/{revision}", (string revision, LookupCatalog lookups) =>
{
    try
    {
        return Results.Ok(lookups.BuildIncoming(revision));
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapGet("/api/lookups/module/{revision}", (string revision, LookupCatalog lookups) =>
{
    try
    {
        return Results.Ok(lookups.BuildModule(revision));
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapPost("/api/incoming-comp/preview", (IncomingCompRequest request, IncomingCompService service) =>
{
    try
    {
        return Results.Ok(service.GeneratePreview(request));
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapPost("/api/module/preview", (ModuleRequest request, ModuleService service) =>
{
    try
    {
        return Results.Ok(service.GeneratePreview(request));
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapPost("/api/incoming-comp/parse", () =>
    Results.StatusCode(StatusCodes.Status501NotImplemented));

app.MapPost("/api/module/parse-comp", () =>
    Results.StatusCode(StatusCodes.Status501NotImplemented));

app.MapPost("/api/module/parse-full", () =>
    Results.StatusCode(StatusCodes.Status501NotImplemented));

app.MapPost("/api/export/registration", (ExportRegistrationRequest request, RegistrationExcelExporter exporter) =>
{
    if (request.Rows is null || request.Rows.Count == 0)
    {
        return Results.BadRequest(new { message = "내보낼 행이 없습니다." });
    }

    return Results.StatusCode(StatusCodes.Status501NotImplemented);
});

app.Run();
