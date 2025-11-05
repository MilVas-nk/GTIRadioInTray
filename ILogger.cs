namespace GTIRadioInTray;

/// <summary>
/// Простой интерфейс для логирования без зависимостей от внешних пакетов
/// </summary>
public interface ILogger
{
	/// <summary>
	/// Записывает информационное сообщение
	/// </summary>
	void LogInformation(string message);

	/// <summary>
	/// Записывает информационное сообщение с параметрами
	/// </summary>
	void LogInformation(string message, params object[] args);

	/// <summary>
	/// Записывает сообщение об ошибке
	/// </summary>
	void LogError(string message);

	/// <summary>
	/// Записывает сообщение об ошибке с исключением
	/// </summary>
	void LogError(Exception exception, string message);

	/// <summary>
	/// Записывает сообщение об ошибке с параметрами
	/// </summary>
	void LogError(Exception exception, string message, params object[] args);

	/// <summary>
	/// Записывает предупреждение
	/// </summary>
	void LogWarning(string message);

	/// <summary>
	/// Записывает предупреждение с исключением
	/// </summary>
	void LogWarning(Exception exception, string message);

	/// <summary>
	/// Записывает предупреждение с параметрами
	/// </summary>
	void LogWarning(Exception exception, string message, params object[] args);

	/// <summary>
	/// Записывает отладочное сообщение
	/// </summary>
	void LogDebug(string message);

	/// <summary>
	/// Записывает отладочное сообщение с исключением
	/// </summary>
	void LogDebug(Exception exception, string message);

	/// <summary>
	/// Записывает отладочное сообщение с параметрами
	/// </summary>
	void LogDebug(Exception exception, string message, params object[] args);
}


