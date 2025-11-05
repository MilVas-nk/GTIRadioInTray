namespace GTIRadioInTray;

/// <summary>
/// Интерфейс сервиса для извлечения метаданных из интернет-радио потоков
/// </summary>
public interface IRadioMetadataService : IDisposable
{
	/// <summary>
	/// Событие, возникающее при обновлении метаданных
	/// </summary>
	event EventHandler<MetadataEventArgs>? MetadataUpdated;

	/// <summary>
	/// Начинает мониторинг метаданных для указанного URL потока
	/// </summary>
	/// <param name="url">URL потока для мониторинга</param>
	void StartMonitoring(string url);

	/// <summary>
	/// Останавливает мониторинг метаданных
	/// </summary>
	void StopMonitoring();
}


