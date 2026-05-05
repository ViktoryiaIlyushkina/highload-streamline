namespace HighLoad.Streamline.Shared.Contracts;

/// <summary>
/// Объект для информирования клиента о текущем состоянии его запроса.
/// </summary>
public record StatusResponse(
    Guid Id,
    ProcessingStatus Status,
    DateTime UpdatedAt);
