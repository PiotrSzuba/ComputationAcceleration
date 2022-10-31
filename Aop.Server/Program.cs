using Aop.RabbitMQ.TSP;
using Aop.Server;
using Microsoft.Extensions.FileProviders;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<TspServer>();
builder.Services.AddDirectoryBrowser();

var fileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "Instances"));
var requestPath = "/instances/list";

var app = builder.Build();
app.Urls.Add("https://0.0.0.0:7251");

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

app.MapGet("/", (IEnumerable<EndpointDataSource> endpointSources) =>
    string.Join("\n", endpointSources.SelectMany(source => source.Endpoints)));

app.MapGet("/allFiles", () =>
{
    string path = Path.Combine(builder.Environment.ContentRootPath, "Instances");
    var allFiles = Directory.GetFiles(path)
        .Select(p => p.Replace(path, ""))
        .Select(p => p.Replace(@"\", ""))
        .ToList();

    allFiles.Sort();

    return Results.Ok(allFiles);
});

app.MapGet("/run/native/{algoritm}/{fileName}", (TspAlgoritms algoritm, string fileName) =>
{
    var tspFileReader = new TspFileReader(fileName);
    var tspInput = new TspInput
    {
        Matrix = tspFileReader.ImMatrix,
    };

    var sw = new Stopwatch();
    sw.Start();
    TspOutput? output = null;
    switch (algoritm)
    {
        case TspAlgoritms.Bruteforce:
            output = Bruteforce.Run(tspInput);
            break;
        case TspAlgoritms.Genetic:
            var genetic = new Genetic(tspInput);
            output = genetic.Run();
            break;
        default:
            output = TspOutput.Error;
            break;
    }
    sw.Stop();

    return Results.Ok(new
    {
        File = fileName,
        Time = sw.ElapsedMilliseconds,
        Prd = ((float)(output.Cost - tspFileReader.OptimalValue) / tspFileReader.OptimalValue) * 100,
        Cost = output.Cost,
        OptimalValue = tspFileReader.OptimalValue,
    });
});

app.MapGet("/run/{algoritm}/{fileName}", (TspServer tspServer, TspAlgoritms algoritm, string fileName) =>
{
    if (tspServer.IsRunning) return Results.Problem("Tsp still running");

    var tspFileReader = new TspFileReader(fileName);

    tspServer.Run(algoritm, tspFileReader);

    return Results.Ok("Tsp started !");
});

app.Run();
