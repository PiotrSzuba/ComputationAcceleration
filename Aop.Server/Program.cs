using Aop.Server;
using Aop.RabbitMQ.TSP;
using Microsoft.Extensions.FileProviders;
using System.Text.Json;

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


// Enable displaying browser links.
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

app.MapGet("/Send/{file}", (TspServer server, string file) =>
{
    //var tspFileReader = new TspFileReader(file);
    //string message = JsonSerializer.Serialize(new TspInput{ Matrix = tspFileReader.ImMatrix });
    //master.SendMessage(message);
});


app.Run();
