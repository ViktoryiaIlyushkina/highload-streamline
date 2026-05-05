using Dapper;
using HighLoad.Streamline.Shared;
using HighLoad.Streamline.Shared.Contracts;
using MassTransit;
using OpenTelemetry.Metrics;
using Serilog;
using StackExchange.Redis;
using System.Text.Json;
using Microsoft.OpenApi.Models; // Добавлено

var builder = WebApplication.CreateBuilder(args);

// --- 1. РЕШЕНИЕ ПРОБЛЕМЫ ЗАДЕРЖЕК ---
ThreadPool.SetMinThreads(500, 500);

// --- 2. НАСТРОЙКА SWAGGER ---
// Добавляем генератор Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- 3. НАСТРОЙКА МЕТРИК ---
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddProcessInstrumentation()
        .AddPrometheusExporter());

// --- 4. НАСТРОЙКА LOGGING ---
Log.Logger = new LoggerConfiguration()
    .WriteTo.Async(a => a.Console())
    .WriteTo.Async(a => a.Seq("http://seq:80"))
    .WriteTo.Async(a => a.Http(requestUri: "http://logstash:5044", queueLimitBytes: null))
    .CreateLogger();
builder.Services.AddSerilog();

// --- 5. ИНФРАСТРУКТУРА ---
var redisConn = builder.Configuration.GetConnectionString("Redis") ?? "redis:6379,password=streamline_pass,abortConnect=false";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConn));
builder.Services.AddSingleton(new DbConnectionFactory(builder.Configuration.GetConnectionString("Postgres")!));

builder.Services.AddMassTransit(x => {
    x.UsingRabbitMq((context, cfg) => cfg.Host(builder.Configuration.GetConnectionString("RabbitMq")));
});

var app = builder.Build();

// --- 6. ВКЛЮЧЕНИЕ SWAGGER В PIPELINE ---
// В HighLoad среде обычно включают Swagger только в Development, 
// но если нужно для тестов везде — оставляем без if (app.Environment.IsDevelopment())

app.UseSwagger();
app.UseSwaggerUI();

app.UseSerilogRequestLogging();
app.UseOpenTelemetryPrometheusScrapingEndpoint();

// --- 7. ЭНДПОИНТЫ ---

app.MapPost("/data", async (DataRecordRequest request, IPublishEndpoint publish) => {
    await publish.Publish(request);
    return Results.Accepted($"/data/{request.Id}");
})
.WithName("PostData")
.WithOpenApi(operation => {
    operation.Summary = "Отправить данные в очередь";
    return operation;
});

app.MapGet("/data/{id}", async (Guid id, IConnectionMultiplexer redis, DbConnectionFactory dbFactory) => {
    var db = redis.GetDatabase();
    string key = $"Streamline_data_{id}";

    var cached = await db.StringGetAsync(key);
    if (cached.HasValue)
        return Results.Ok(JsonSerializer.Deserialize<DataRecordRequest>(cached!));

    using var conn = dbFactory.CreateConnection();
    var data = await conn.QueryFirstOrDefaultAsync<DataRecordRequest>(
        "SELECT id, payload, created_at as CreatedAt FROM data_records WHERE id = @id",
        new { id });

    if (data == null) return Results.NotFound();

    _ = db.StringSetAsync(key, JsonSerializer.Serialize(data), TimeSpan.FromHours(1));

    return Results.Ok(data);
})
.WithName("GetDataById")
.Produces<DataRecordRequest>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.WithOpenApi(operation => {
    operation.Summary = "Получить данные по ID (Cache-Aside)";
    return operation;
});

app.Run();