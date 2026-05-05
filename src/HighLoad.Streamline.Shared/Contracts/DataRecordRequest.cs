namespace HighLoad.Streamline.Shared.Contracts;

/// <summary>
/// Основной контракт данных для записи и передачи в очередь.
/// </summary>
public record DataRecordRequest(
    Guid Id,
    string Payload,
    DateTime CreatedAt);
