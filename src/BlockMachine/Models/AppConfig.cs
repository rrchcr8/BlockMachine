namespace BlockMachine.Models;

public sealed class AppConfig
{
    public bool IsConfigured { get; set; }
    public bool Enabled { get; set; } = true;
    public bool RunAtStartup { get; set; } = true;

    /// <summary>Hora de inicio del bloqueo (formato HH:mm).</summary>
    public string BlockStartTime { get; set; } = "02:00";

    /// <summary>Hora de fin del bloqueo (formato HH:mm).</summary>
    public string BlockEndTime { get; set; } = "07:00";

    public MessageDisplayMode DisplayMode { get; set; } = MessageDisplayMode.Slideshow;

    public int SlideIntervalSeconds { get; set; } = 30;

    public bool CreateAdminDesktopShortcut { get; set; } = true;

    public List<BlockMessage> Messages { get; set; } = [];

    // Campos legacy: se migran automáticamente a Messages al cargar.
    public string Title { get; set; } = "Es hora de descansar";

    public string Message { get; set; } =
        "Son las {hora}.\n\nPor favor, vuelve a la cama y descansa.\nMañana podrás usar la computadora.\n\nTe queremos mucho.";

    public string AdminPasswordHash { get; set; } = string.Empty;
}
