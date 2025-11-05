using System.Windows.Forms;
using System.Windows.Media;
using System.Drawing;

namespace GTIRadioInTray;

/// <summary>
/// Контекст приложения для tray-иконки с функционалом воспроизведения интернет-радио
/// </summary>
public sealed class TrayApplicationContext : ApplicationContext
{
	private readonly NotifyIcon _notifyIcon;
	private readonly ContextMenuStrip _contextMenu;
	private readonly ToolStripMenuItem _exitMenuItem;
	private readonly ToolStripMenuItem _hiMenuItem;
	private readonly ToolStripMenuItem _midMenuItem;
	private readonly ToolStripMenuItem _lowMenuItem;
	private readonly MediaPlayer _mediaPlayer;
	private readonly IRadioMetadataService _metadataService;
	private readonly ILogger? _logger;

	private const string UrlHi = "https://listen.gtiradio.ru/radiohi";
	private const string UrlMid = "https://listen.gtiradio.ru/radiomid";
	private const string UrlLow = "https://listen.gtiradio.ru/radiolow";

	private string _currentUrl = UrlMid;

	/// <summary>
	/// Инициализирует новый экземпляр класса TrayApplicationContext
	/// </summary>
	/// <param name="metadataService">Сервис для получения метаданных радио</param>
	/// <param name="logger">Логгер для записи событий и ошибок (опционально)</param>
	public TrayApplicationContext(IRadioMetadataService? metadataService = null, ILogger? logger = null)
	{
		_contextMenu = new ContextMenuStrip();
		_exitMenuItem = new ToolStripMenuItem("Выход");
		_exitMenuItem.Click += (_, _) => ExitApplication();

		_hiMenuItem = new ToolStripMenuItem("hi");
		_midMenuItem = new ToolStripMenuItem("middle");
		_lowMenuItem = new ToolStripMenuItem("low");

		_hiMenuItem.Click += (_, _) => SelectStream(UrlHi, _hiMenuItem);
		_midMenuItem.Click += (_, _) => SelectStream(UrlMid, _midMenuItem);
		_lowMenuItem.Click += (_, _) => SelectStream(UrlLow, _lowMenuItem);

		_contextMenu.Items.AddRange(new ToolStripItem[] { _hiMenuItem, _midMenuItem, _lowMenuItem, new ToolStripSeparator(), _exitMenuItem });

		_notifyIcon = new NotifyIcon
		{
			Icon = Properties.Resources.tray,
			Visible = true,
			Text = "GTIRadioInTray by MilVas",
			ContextMenuStrip = _contextMenu
		};

		_notifyIcon.MouseUp += (_, e) =>
		{
			// Меню только по правой кнопке мыши
			if (e.Button == MouseButtons.Right)
			{
				_contextMenu.Show(Cursor.Position);
			}
		};

		_mediaPlayer = new MediaPlayer
		{
			Volume = 1.0
		};

		// Инициализация сервиса метаданных
		_logger = logger ?? new NullLogger();
		_metadataService = metadataService ?? new RadioMetadataService(new ConsoleLogger(nameof(RadioMetadataService)));
		_metadataService.MetadataUpdated += OnMetadataUpdated;

		// Автозапуск потока при старте
		MarkSelected(_midMenuItem);
		_mediaPlayer.Open(new Uri(_currentUrl));
		_mediaPlayer.MediaFailed += (_, args) =>
		{
			if (args.ErrorException != null)
			{
				_logger?.LogError(args.ErrorException, "Ошибка воспроизведения медиапотока");
			}
			else
			{
				_logger?.LogError("Ошибка воспроизведения медиапотока: неизвестная ошибка");
			}
			_notifyIcon.BalloonTipTitle = "GTIRadioInTray";
			_notifyIcon.BalloonTipText = "Ошибка воспроизведения потока.";
			_notifyIcon.ShowBalloonTip(3000);
		};
		_mediaPlayer.MediaOpened += (_, _) =>
		{
			_mediaPlayer.Play();
			// Запускаем мониторинг метаданных после успешного открытия потока
			_metadataService.StartMonitoring(_currentUrl);
		};
	}

	/// <summary>
	/// Переключает воспроизведение на указанный поток
	/// </summary>
	/// <param name="url">URL потока для воспроизведения</param>
	/// <param name="sourceItem">Элемент меню, соответствующий выбранному потоку</param>
	private void SelectStream(string url, ToolStripMenuItem sourceItem)
	{
		if (_currentUrl == url) return;
		
		_currentUrl = url;
		MarkSelected(sourceItem);
		
		try
		{
			_logger?.LogInformation($"Переключение потока на: {url}");
			_mediaPlayer.Stop();
			_metadataService.StopMonitoring(); // Останавливаем старый мониторинг
			_mediaPlayer.Open(new Uri(_currentUrl));
			// Новый мониторинг запустится в событии MediaOpened
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, $"Не удалось переключить поток на: {url}");
			_notifyIcon.BalloonTipTitle = "GTIRadioInTray";
			_notifyIcon.BalloonTipText = "Не удалось переключить поток.";
			_notifyIcon.ShowBalloonTip(3000);
		}
	}

	/// <summary>
	/// Обработчик события обновления метаданных
	/// </summary>
	/// <param name="sender">Источник события</param>
	/// <param name="e">Аргументы события с метаданными</param>
	private void OnMetadataUpdated(object? sender, MetadataEventArgs e)
	{
		// Обновляем tooltip с информацией о текущей композиции
		if (!string.IsNullOrWhiteSpace(e.Artist) || !string.IsNullOrWhiteSpace(e.Title))
		{
			var tooltipText = $"Сейчас играет: {e}";
			// Ограничиваем длину tooltip (максимум 127 символов для NotifyIcon)
			var displayText = tooltipText.Length > 127 
				? tooltipText.Substring(0, 124) + "..." 
				: tooltipText;
			
			// Обновляем свойства NotifyIcon
			// NotifyIcon свойства Text и BalloonTipText безопасны для использования из других потоков
			_notifyIcon.Text = displayText;
			_notifyIcon.BalloonTipTitle = "GTIRadioInTray";
			_notifyIcon.BalloonTipText = $"Сейчас играет: {e}";
		}
	}

	/// <summary>
	/// Отмечает выбранный элемент меню как активный
	/// </summary>
	/// <param name="selected">Выбранный элемент меню</param>
	private void MarkSelected(ToolStripMenuItem selected)
	{
		_hiMenuItem.Checked = selected == _hiMenuItem;
		_midMenuItem.Checked = selected == _midMenuItem;
		_lowMenuItem.Checked = selected == _lowMenuItem;
	}

	/// <summary>
	/// Корректно завершает работу приложения
	/// </summary>
	private void ExitApplication()
	{
		try
		{
			_logger?.LogInformation("Завершение работы приложения");
			_mediaPlayer.Stop();
			_mediaPlayer.Close();
			_metadataService.StopMonitoring();
			_metadataService.Dispose();
		}
		catch (Exception ex)
		{
			_logger?.LogWarning(ex, "Ошибка при освобождении ресурсов при выходе");
		}
		finally
		{
			_notifyIcon.Visible = false;
			_notifyIcon.Dispose();
			_contextMenu.Dispose();
			_exitMenuItem.Dispose();
			ExitThread();
		}
	}
}


