using Domain.Model.Services;
using Elastic.Clients.Elasticsearch;
using Gdd.Application.Services;
using Gdd.Domain.Model;
using Gdd.Domain.Model.Requests;
using Gdd.Domain.Services;
using Gdd.Domain.Services.Tiles;
using Gdd.Repository.Utils;
using Serilog;
using Serilog.Events;
using TileProxyServer.Services;

var builder = WebApplication.CreateBuilder(args);

using var logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {ClientIp}] {Message:lj}{NewLine}{Exception}")
    .Enrich.WithClientIp()
    .CreateLogger();

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddSerilog(logger);

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.Configure<TileServerConfiguration>(builder.Configuration.GetSection("TileServer"));
builder.Services.Configure<SqliteConfiguration>(builder.Configuration.GetSection("Sqlite"));

//ElasticSearch build
var elasticsearchUri = builder.Configuration["ElasticSearch:Url"] ?? string.Empty;
var elasticsearchIndex = builder.Configuration["ElasticSearch:Index"] ?? string.Empty;

var elasticSettings = new ElasticsearchClientSettings(new Uri(elasticsearchUri))
    .DefaultIndex(elasticsearchIndex);

var elasticSearchClient = new ElasticsearchClient(elasticSettings);

await elasticSearchClient.DeleteByQueryAsync<Request>(
    s => s.Query(q => q.QueryString(qs => qs.Query("*")))
);

builder.Services.AddSingleton(elasticSearchClient);
builder.Services.AddSingleton<IBlacklistRepository, BlacklistRepository>();
builder.Services.AddSingleton<IBlacklistManager, BlacklistManager>();

builder.Services.AddSingleton<IRequestsRepository, RequestsRepository>();
builder.Services.AddSingleton<IIntrusionDetectionService, IntrusionDetectionService>();

builder.Services.AddSingleton<ITileRepository, RemoteTileRepository>();
builder.Services.AddSingleton<ITileManager, TileManager>();

builder.Services.AddSingleton<IIpSecurityService, IpSecurityService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
