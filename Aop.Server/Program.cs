using Aop.Server;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<TspServer>();
builder.Services.AddDirectoryBrowser();

var fileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "Instances"));
var requestPath = "/instances/list";

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

}

app.UseHttpsRedirection();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = fileProvider,
    RequestPath = requestPath
});

app.UseDirectoryBrowser(new DirectoryBrowserOptions
{
    FileProvider = fileProvider,
    RequestPath = requestPath
});

app.MapGet("/allFiles", () =>
{
    string path = Path.Combine(builder.Environment.ContentRootPath, "Instances");
    var allFiles = Directory.GetFiles(path)
        .Select(p => p.Replace(path, ""))
        .Select(p => p.Replace(@"\", ""))
        .ToList();

    return Results.Ok(allFiles);
});

app.MapGet("/run", (TspServer tspServer) =>
{
    if (tspServer.isRunning) return Results.Problem("Tsp still running");

    tspServer.Run();

    return Results.Ok("Tsp started !");
});

app.Run();
