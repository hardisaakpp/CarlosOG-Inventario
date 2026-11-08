using System.Text.Json.Serialization;
using Inventario.Transactions.Clients;
using Inventario.Transactions.Data;
using Inventario.Transactions.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddDbContext<TransactionDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("inventario")));

// Cliente REST síncrono hacia ProductService.
builder.Services.AddHttpClient<IProductClient, ProductClient>(client =>
{
    var baseUrl = builder.Configuration["Services:ProductService:BaseUrl"] ?? "http://localhost:5079";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddScoped<ITransactionsService, TransactionsService>();

// Permite que el frontend Angular (otro puerto) consuma la API.
var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "TransactionService v1"));
}

app.UseCors();
app.MapControllers();

app.Run();
