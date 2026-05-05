using Dapper;
using HighLoad.Streamline.Shared.Contracts;
using MassTransit;
using Npgsql;
using StackExchange.Redis;
using System.Text.Json;

namespace HighLoad.Streamline.Worker.Consumers;

public class DataRecordBatchConsumer : IConsumer<Batch<DataRecordRequest>>
{
    private readonly string _pgConnString;
    private readonly IDatabase _redis;
    private readonly ILogger<DataRecordBatchConsumer> _logger;

    public DataRecordBatchConsumer(IConfiguration config, IConnectionMultiplexer redis, ILogger<DataRecordBatchConsumer> logger)
    {
        _pgConnString = config.GetConnectionString("Postgres")!;
        _redis = redis.GetDatabase();
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<Batch<DataRecordRequest>> context)
    {
        var messages = context.Message.Select(m => m.Message).ToList();

        // 1. Высокопроизводительная вставка в БД (Binary Copy)
        await using var conn = new NpgsqlConnection(_pgConnString);
        await conn.OpenAsync();

        await using (var writer = await conn.BeginBinaryImportAsync(
            "COPY data_records (id, payload, created_at) FROM STDIN (FORMAT BINARY)"))
        {
            foreach (var msg in messages)
            {
                await writer.StartRowAsync();
                await writer.WriteAsync(msg.Id);
                await writer.WriteAsync(msg.Payload);
                await writer.WriteAsync(msg.CreatedAt, NpgsqlTypes.NpgsqlDbType.TimestampTz);
            }
            await writer.CompleteAsync();
        }

        // 2. Пакетное обновление кэша в Redis (Pipelining)
        // Теперь вместо статуса мы сохраняем объект целиком
        var redisBatch = _redis.CreateBatch();
        foreach (var msg in messages)
        {
            string key = $"Streamline_data_{msg.Id}";
            _ = redisBatch.StringSetAsync(key, JsonSerializer.Serialize(msg), TimeSpan.FromHours(1));
        }

        // Отправляем все команды в Redis одним махом
        redisBatch.Execute();

        _logger.LogInformation("Batch processed: {Count} records saved to DB and Warm-cached", messages.Count);
    }
}