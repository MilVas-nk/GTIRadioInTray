using System.Diagnostics;

namespace GTIRadioInTray;

/// <summary>
/// Простая реализация логгера, выводящая сообщения в Debug output
/// </summary>
public class ConsoleLogger : ILogger
{
	private readonly string _categoryName;

	/// <summary>
	/// Инициализирует новый экземпляр ConsoleLogger
	/// </summary>
	/// <param name="categoryName">Имя категории для логирования</param>
	public ConsoleLogger(string categoryName)
	{
		_categoryName = categoryName ?? string.Empty;
	}

	/// <summary>
	/// Записывает информационное сообщение
	/// </summary>
	public void LogInformation(string message)
	{
		Debug.WriteLine($"[INFO] [{_categoryName}] {message}");
	}

	/// <summary>
	/// Записывает информационное сообщение с параметрами
	/// </summary>
	public void LogInformation(string message, params object[] args)
	{
		try
		{
			var formatted = string.Format(message, args);
			LogInformation(formatted);
		}
		catch
		{
			LogInformation(message);
		}
	}

	/// <summary>
	/// Записывает сообщение об ошибке
	/// </summary>
	public void LogError(string message)
	{
		Debug.WriteLine($"[ERROR] [{_categoryName}] {message}");
	}

	/// <summary>
	/// Записывает сообщение об ошибке с исключением
	/// </summary>
	public void LogError(Exception exception, string message)
	{
		Debug.WriteLine($"[ERROR] [{_categoryName}] {message}");
		if (exception != null)
		{
			Debug.WriteLine($"[ERROR] [{_categoryName}] Exception: {exception}");
		}
	}

	/// <summary>
	/// Записывает сообщение об ошибке с параметрами
	/// </summary>
	public void LogError(Exception exception, string message, params object[] args)
	{
		try
		{
			var formatted = string.Format(message, args);
			LogError(exception, formatted);
		}
		catch
		{
			LogError(exception, message);
		}
	}

	/// <summary>
	/// Записывает предупреждение
	/// </summary>
	public void LogWarning(string message)
	{
		Debug.WriteLine($"[WARN] [{_categoryName}] {message}");
	}

	/// <summary>
	/// Записывает предупреждение с исключением
	/// </summary>
	public void LogWarning(Exception exception, string message)
	{
		Debug.WriteLine($"[WARN] [{_categoryName}] {message}");
		if (exception != null)
		{
			Debug.WriteLine($"[WARN] [{_categoryName}] Exception: {exception}");
		}
	}

	/// <summary>
	/// Записывает предупреждение с параметрами
	/// </summary>
	public void LogWarning(Exception exception, string message, params object[] args)
	{
		try
		{
			var formatted = string.Format(message, args);
			LogWarning(exception, formatted);
		}
		catch
		{
			LogWarning(exception, message);
		}
	}

	/// <summary>
	/// Записывает отладочное сообщение
	/// </summary>
	public void LogDebug(string message)
	{
		Debug.WriteLine($"[DEBUG] [{_categoryName}] {message}");
	}

	/// <summary>
	/// Записывает отладочное сообщение с исключением
	/// </summary>
	public void LogDebug(Exception exception, string message)
	{
		Debug.WriteLine($"[DEBUG] [{_categoryName}] {message}");
		if (exception != null)
		{
			Debug.WriteLine($"[DEBUG] [{_categoryName}] Exception: {exception}");
		}
	}

	/// <summary>
	/// Записывает отладочное сообщение с параметрами
	/// </summary>
	public void LogDebug(Exception exception, string message, params object[] args)
	{
		try
		{
			var formatted = string.Format(message, args);
			LogDebug(exception, formatted);
		}
		catch
		{
			LogDebug(exception, message);
		}
	}
}


