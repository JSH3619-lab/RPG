using RamosPartGenerator.Api.Contracts;
using RamosPartGenerator.Api.Services;
using RamosPartGenerator.Core.Models;
using RamosPartGenerator.Core.Services;
using RamosPartGenerator.Excel;

var builder = WebApplication.CreateBuilder(args);

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

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/status", () => Results.Ok(new
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

app.MapPost("/api/incoming-comp/parse", (PartParseRequest request, IncomingCompService service) =>
{
    try
    {
        return Results.Ok(service.ParseCompPart(request.Revision, request.PartCode));
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapPost("/api/module/parse-comp", (PartParseRequest request, ModuleService service) =>
{
    try
    {
        return Results.Ok(service.ParseCompPart(request.Revision, request.PartCode));
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapPost("/api/module/parse-full", (PartParseRequest request, ModuleService service) =>
{
    try
    {
        return Results.Ok(service.ParseModuleFullPart(request.Revision, request.PartCode));
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapPost("/api/export/registration", (ExportRegistrationRequest request, RegistrationExcelExporter exporter) =>
{
    if (request.Rows is null || request.Rows.Count == 0)
    {
        return Results.BadRequest(new { message = "Export rows are required." });
    }

    var rows = request.Rows
        .Select(row => new GeneratedPartRow(
            row.Kind,
            row.PartCode,
            row.Name,
            row.GeneralInfo,
            row.Specification,
            row.Note))
        .ToArray();

    var content = exporter.Export(rows);
    return Results.File(
        content,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        exporter.DefaultFileName);
});

app.MapFallbackToFile("index.html");

app.Run();
