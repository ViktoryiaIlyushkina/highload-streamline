namespace HighLoad.Streamline.Shared.Contracts;

/// <summary>
/// Жизненный цикл обработки записи в системе.
/// </summary>
public enum ProcessingStatus
{
    /// <summary> Запрос принят API и ожидает в очереди </summary>
    Pending,

    /// <summary> Воркер взял задачу в работу </summary>
    Processing,

    /// <summary> Данные успешно записаны в БД </summary>
    Completed,

    /// <summary> Произошла ошибка при обработке </summary>
    Failed
}
