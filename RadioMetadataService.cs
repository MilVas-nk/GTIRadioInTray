using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace GTIRadioInTray;

/// <summary>
/// Сервис для извлечения метаданных (исполнитель/композиция) из интернет-радио потоков
/// </summary>
public class RadioMetadataService : IRadioMetadataService
{
	private readonly HttpClient _httpClient;
	private readonly ILogger? _logger;
	private CancellationTokenSource? _cancellationTokenSource;
	private Task? _metadataTask;
	private string _currentUrl = string.Empty;

	/// <summary>
	/// Событие, возникающее при обновлении метаданных
	/// </summary>
	public event EventHandler<MetadataEventArgs>? MetadataUpdated;

	/// <summary>
	/// Инициализирует новый экземпляр класса RadioMetadataService
	/// </summary>
	/// <param name="logger">Логгер для записи событий и ошибок (опционально)</param>
	public RadioMetadataService(ILogger? logger = null)
	{
		_logger = logger;
		_httpClient = new HttpClient();
		_httpClient.Timeout = TimeSpan.FromSeconds(10);
	}

	/// <summary>
	/// Начинает мониторинг метаданных для указанного URL потока
	/// </summary>
	public void StartMonitoring(string url)
	{
		if (_currentUrl == url && _metadataTask != null && !_metadataTask.IsCompleted)
		{
			return; // Уже мониторим этот поток
		}

		StopMonitoring();
		_currentUrl = url;

		_cancellationTokenSource = new CancellationTokenSource();
		_metadataTask = Task.Run(() => MonitorMetadataAsync(url, _cancellationTokenSource.Token));
	}

	/// <summary>
	/// Останавливает мониторинг метаданных
	/// </summary>
	public void StopMonitoring()
	{
		_cancellationTokenSource?.Cancel();
		if (_metadataTask != null)
		{
			try
			{
				_metadataTask.Wait(TimeSpan.FromSeconds(2));
			}
			catch { /* игнорируем */ }
		}
		_cancellationTokenSource?.Dispose();
		_cancellationTokenSource = null;
		_metadataTask = null;
	}

	private async Task MonitorMetadataAsync(string url, CancellationToken cancellationToken)
	{
		HttpResponseMessage? response = null;
		Stream? stream = null;

		try
		{
			var request = new HttpRequestMessage(HttpMethod.Get, url);
			request.Headers.Add("Icy-MetaData", "1"); // Запрашиваем метаданные ICY протокола

			response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
			response.EnsureSuccessStatusCode();

			// Проверяем, поддерживает ли сервер ICY метаданные
			if (!response.Headers.TryGetValues("icy-metaint", out var metaintValues))
			{
				_logger?.LogDebug("Поток не содержит ICY метаданных");
				// Сервер не поддерживает ICY метаданные, пробуем альтернативные методы
				await TryAlternativeMetadataMethod(url, cancellationToken);
				return;
			}

			// Получаем интервал метаданных (каждые N байт)
			int metaint = int.Parse(string.Join("", metaintValues));
			_logger?.LogDebug($"Метаданные найдены! Интервал: {metaint} байт");

			stream = await response.Content.ReadAsStreamAsync(cancellationToken);
			string? lastMetadata = null;

			while (!cancellationToken.IsCancellationRequested)
			{
				// Читаем метаданные из потока (рабочий метод из radioInfo/Program.cs)
				var metadata = await ReadMetadataFromStreamAsync(stream, metaint, cancellationToken);
				
				if (metadata == null)
				{
					await Task.Delay(1000, cancellationToken); // Пауза перед следующей попыткой
					continue;
				}

				if (!string.IsNullOrEmpty(metadata.RawMetadata) && metadata.RawMetadata != lastMetadata)
				{
					lastMetadata = metadata.RawMetadata;
					if (!string.IsNullOrEmpty(metadata.Title))
					{
						string artist = metadata.Artist ?? string.Empty;
						string title = !string.IsNullOrEmpty(metadata.Song) ? metadata.Song : metadata.Title;
						MetadataUpdated?.Invoke(this, new MetadataEventArgs(artist, title));
					}
				}

				// Небольшая задержка, чтобы не перегружать процессор
				await Task.Delay(100, cancellationToken);
			}
		}
		catch (OperationCanceledException)
		{
			// Ожидаемое исключение при отмене
			_logger?.LogDebug($"Мониторинг метаданных был отменен для URL: {url}");
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, $"Ошибка при мониторинге метаданных для URL: {url}");
		}
		finally
		{
			stream?.Dispose();
			response?.Dispose();
		}
	}

	/// <summary>
	/// Альтернативный метод для получения метаданных (например, через HTTP заголовки или JSON API)
	/// </summary>
	private async Task TryAlternativeMetadataMethod(string url, CancellationToken cancellationToken)
	{
		try
		{
			// Некоторые радиостанции предоставляют метаданные через отдельный endpoint
			// Например: https://listen.gtiradio.ru/radiohi/current.json или /status-json.xsl
			var baseUrl = url.TrimEnd('/');
			var possibleEndpoints = new[]
			{
				$"{baseUrl}/status-json.xsl",
				$"{baseUrl}/current.json",
				$"{baseUrl}/metadata",
				$"{baseUrl}/nowplaying"
			};

			foreach (var endpoint in possibleEndpoints)
			{
				try
				{
					using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
					if (response.IsSuccessStatusCode)
					{
						var content = await response.Content.ReadAsStringAsync(cancellationToken);
						var metadata = ParseJsonMetadata(content);
						if (metadata.HasValue)
						{
							MetadataUpdated?.Invoke(this, new MetadataEventArgs(metadata.Value.Artist, metadata.Value.Title));
							var currentMetadata = metadata.Value;
							
							// Периодически проверяем обновления
							while (!cancellationToken.IsCancellationRequested)
							{
								await Task.Delay(5000, cancellationToken); // Проверяем каждые 5 секунд
								
								using var updateResponse = await _httpClient.GetAsync(endpoint, cancellationToken);
								if (updateResponse.IsSuccessStatusCode)
								{
									var updateContent = await updateResponse.Content.ReadAsStringAsync(cancellationToken);
									var newMetadata = ParseJsonMetadata(updateContent);
									if (newMetadata.HasValue && (currentMetadata.Artist != newMetadata.Value.Artist || currentMetadata.Title != newMetadata.Value.Title))
									{
										currentMetadata = newMetadata.Value;
										MetadataUpdated?.Invoke(this, new MetadataEventArgs(currentMetadata.Artist, currentMetadata.Title));
									}
								}
							}
							return;
						}
					}
				}
				catch (Exception ex)
				{
					_logger?.LogDebug(ex, $"Не удалось получить метаданные из endpoint: {endpoint}");
				}
			}
		}
		catch (OperationCanceledException)
		{
			_logger?.LogDebug($"Альтернативный метод получения метаданных был отменен для URL: {url}");
		}
		catch (Exception ex)
		{
			_logger?.LogWarning(ex, $"Ошибка при альтернативном методе получения метаданных для URL: {url}");
		}
	}

	/// <summary>
	/// Читает метаданные из потока (рабочая реализация из radioInfo/Program.cs)
	/// </summary>
	private async Task<IcyStreamMetadata?> ReadMetadataFromStreamAsync(Stream stream, int metaint, CancellationToken cancellationToken)
	{
		// Пропускаем аудио данные
		byte[] buffer = new byte[metaint];
		int bytesRead = await ReadFullyAsync(stream, buffer, 0, metaint, cancellationToken);

		if (bytesRead < metaint)
		{
			_logger?.LogDebug("Не удалось прочитать достаточно данных из потока");
			return null;
		}

		// Читаем размер метаданных
		byte[] metaLengthByte = new byte[1];
		if (await stream.ReadAsync(metaLengthByte, 0, 1, cancellationToken) != 1)
		{
			_logger?.LogDebug("Не удалось прочитать размер метаданных");
			return null;
		}

		int metadataLength = metaLengthByte[0] * 16;

		if (metadataLength == 0)
		{
			return null; // Нет метаданных в этом блоке
		}

		// Читаем метаданные
		byte[] metadataBytes = new byte[metadataLength];
		int metadataBytesRead = await ReadFullyAsync(stream, metadataBytes, 0, metadataLength, cancellationToken);
		
		if (metadataBytesRead < metadataLength)
		{
			_logger?.LogDebug("Не удалось прочитать все метаданные");
			return null;
		}

		string metadata = Encoding.UTF8.GetString(metadataBytes).TrimEnd('\0');
		return ParseMetadata(metadata);
	}

	/// <summary>
	/// Гарантированно читает указанное количество байт из потока
	/// </summary>
	private async Task<int> ReadFullyAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		int totalRead = 0;
		while (totalRead < count && !cancellationToken.IsCancellationRequested)
		{
			int read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead, cancellationToken);
			if (read == 0)
				break;
			totalRead += read;
		}
		return totalRead;
	}

	/// <summary>
	/// Парсит метаданные в формате StreamTitle='Artist - Title' (рабочая реализация из radioInfo/Program.cs)
	/// </summary>
	private IcyStreamMetadata ParseMetadata(string metadata)
	{
		var result = new IcyStreamMetadata { RawMetadata = metadata };

		var match = Regex.Match(metadata, @"StreamTitle='([^']+)'");
		if (!match.Success)
		{
			_logger?.LogDebug("Не удалось извлечь заголовок из метаданных");
			return result;
		}

		result.Title = match.Groups[1].Value;
		_logger?.LogDebug($"Сейчас играет: {result.Title}");

		// Разделяем на исполнителя и песню
		if (result.Title.Contains(" - "))
		{
			var parts = result.Title.Split(new[] { " - " }, 2, StringSplitOptions.None);
			result.Artist = parts[0].Trim();
			result.Song = parts[1].Trim();
			_logger?.LogDebug($"Исполнитель: {result.Artist}");
			_logger?.LogDebug($"Песня: {result.Song}");
		}

		return result;
	}

	/// <summary>
	/// Вспомогательный класс для хранения метаданных ICY потока
	/// </summary>
	private class IcyStreamMetadata
	{
		public string Title { get; set; } = string.Empty;
		public string? Artist { get; set; }
		public string? Song { get; set; }
		public string RawMetadata { get; set; } = string.Empty;
	}

	/// <summary>
	/// Парсит JSON метаданные (для альтернативных методов)
	/// </summary>
	private (string Artist, string Title)? ParseJsonMetadata(string json)
	{
		try
		{
			// Пробуем найти StreamTitle или похожие поля в JSON
			var streamTitleMatch = Regex.Match(
				json,
				@"""streamtitle""\s*:\s*[""']([^""']+)[""']",
				RegexOptions.IgnoreCase);

			if (!streamTitleMatch.Success)
			{
				// Пробуем другой формат
				streamTitleMatch = Regex.Match(
					json,
					@"""title""\s*:\s*[""']([^""']+)[""']",
					RegexOptions.IgnoreCase);
			}

			if (streamTitleMatch.Success)
			{
				var title = streamTitleMatch.Groups[1].Value.Trim();
				var parts = title.Split(new[] { " - ", " – ", " — ", "-" }, 2, StringSplitOptions.RemoveEmptyEntries);
				
				if (parts.Length == 2)
				{
					return (parts[0].Trim(), parts[1].Trim());
				}
				
				return (string.Empty, title);
			}
		}
		catch (Exception ex)
		{
			_logger?.LogWarning(ex, "Ошибка при парсинге JSON метаданных");
		}

		return null;
	}

	/// <summary>
	/// Освобождает ресурсы, используемые объектом RadioMetadataService
	/// </summary>
	public void Dispose()
	{
		StopMonitoring();
		_httpClient?.Dispose();
	}
}

/// <summary>
/// Аргументы события обновления метаданных
/// </summary>
public class MetadataEventArgs : EventArgs
{
	/// <summary>
	/// Получает имя исполнителя
	/// </summary>
	public string Artist { get; }

	/// <summary>
	/// Получает название композиции
	/// </summary>
	public string Title { get; }

	/// <summary>
	/// Инициализирует новый экземпляр класса MetadataEventArgs
	/// </summary>
	/// <param name="artist">Имя исполнителя</param>
	/// <param name="title">Название композиции</param>
	public MetadataEventArgs(string artist, string title)
	{
		Artist = artist;
		Title = title;
	}

	/// <summary>
	/// Возвращает строковое представление метаданных
	/// </summary>
	/// <returns>Строка в формате "Artist - Title" или только "Title", если исполнитель не указан</returns>
	public override string ToString()
	{
		if (string.IsNullOrWhiteSpace(Artist))
			return Title;
		return $"{Artist} - {Title}";
	}
}

