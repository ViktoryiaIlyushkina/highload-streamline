using HighLoad.Streamline.Shared; // Убедись, что тут твой DbConnectionFactory
using HighLoad.Streamline.Shared.Contracts;
using HighLoad.Streamline.Worker.Consumers;
using MassTransit;
using Serilog;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

// --- 1. Настройка Serilog ---
// Сначала настраиваем статический логгер
Log.Logger = new LoggerConfiguration()
    .WriteTo.Async(a => a.Console())
    .WriteTo.Async(a => a.Seq("http://seq:80"))
    .WriteTo.Async(a => a.Http("http://logstash:5044", queueLimitBytes: null))
    .CreateLogger();

// Правильный способ регистрации для HostApplicationBuilder:
builder.Logging.ClearProviders();
builder.Services.AddSerilog();

// --- 2. Инфраструктура ---
// Регистрация БД (обязательно, если консьюмер пишет в Postgres)
builder.Services.AddSingleton(new DbConnectionFactory(builder.Configuration.GetConnectionString("Postgres")!));

// Регистрация Redis
var redisConn = builder.Configuration.GetConnectionString("Redis") ?? "redis:6379,password=streamline_pass,abortConnect=false";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConn));

// --- 3. Настройка MassTransit ---
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<DataRecordBatchConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMq"));

        cfg.ReceiveEndpoint("data-records-queue", e =>
        {
            e.PrefetchCount = 1000;

            // Настройка батча
            e.Batch<DataRecordRequest>(b =>
            {
                b.MessageLimit = 100;
                b.TimeLimit = TimeSpan.FromSeconds(2);
            });

            // ВАЖНО: В режиме батчинга ConcurrentMessageLimit должен быть 
            // согласован с PrefetchCount. 500 — это много для батчей по 100 при префетче 1000.
            // Попробуй начать с меньшего числа, чтобы проверить стабильность.
            e.ConcurrentMessageLimit = 10;

            e.ConfigureConsumer<DataRecordBatchConsumer>(context);
        });
    });
});

var host = builder.Build();
host.Run();