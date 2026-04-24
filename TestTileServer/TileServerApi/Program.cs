using Scalar.AspNetCore;
using TileServerApi.Model;
using TileServerApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var tilesDirectory = builder.Configuration["Tiles:LocalStoragePath"] ?? string.Empty;

builder.Services.AddSingleton<ITileRepository>(_ => new LocalTileRepository(tilesDirectory));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
