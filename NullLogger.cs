namespace GTIRadioInTray;

/// <summary>
/// Реализация логгера, которая ничего не делает (для случаев, когда логирование не требуется)
/// </summary>
public class NullLogger : ILogger
{
	/// <summary>
	/// Записывает информационное сообщение
	/// </summary>
	public void LogInformation(string message) { }

	/// <summary>
	/// Записывает информационное сообщение с параметрами
	/// </summary>
	public void LogInformation(string message, params object[] args) { }

	/// <summary>
	/// Записывает сообщение об ошибке
	/// </summary>
	public void LogError(string message) { }

	/// <summary>
	/// Записывает сообщение об ошибке с исключением
	/// </summary>
	public void LogError(Exception exception, string message) { }

	/// <summary>
	/// Записывает сообщение об ошибке с параметрами
	/// </summary>
	public void LogError(Exception exception, string message, params object[] args) { }

	/// <summary>
	/// Записывает предупреждение
	/// </summary>
	public void LogWarning(string message) { }

	/// <summary>
	/// Записывает предупреждение с исключением
	/// </summary>
	public void LogWarning(Exception exception, string message) { }

	/// <summary>
	/// Записывает предупреждение с параметрами
	/// </summary>
	public void LogWarning(Exception exception, string message, params object[] args) { }

	/// <summary>
	/// Записывает отладочное сообщение
	/// </summary>
	public void LogDebug(string message) { }

	/// <summary>
	/// Записывает отладочное сообщение с исключением
	/// </summary>
	public void LogDebug(Exception exception, string message) { }

	/// <summary>
	/// Записывает отладочное сообщение с параметрами
	/// </summary>
	public void LogDebug(Exception exception, string message, params object[] args) { }
}


