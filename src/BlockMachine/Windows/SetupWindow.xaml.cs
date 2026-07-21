using System.Text;
using System.Windows;
using BlockMachine.Models;
using BlockMachine.Services;

namespace BlockMachine.Windows;

public partial class SetupWindow : Window
{
    private readonly ConfigService _configService;
    private readonly Action<AppConfig> _onCompleted;

    public SetupWindow(ConfigService configService, Action<AppConfig> onCompleted)
    {
        InitializeComponent();
        _configService = configService;
        _onCompleted = onCompleted;
        LoadMessagesPreview();
    }

    private void LoadMessagesPreview()
    {
        var preview = new StringBuilder();

        foreach (var message in MessageService.CreateDefaultMessages())
        {
            preview.AppendLine($"• {message.Title}");
        }

        MessagesPreviewText.Text = preview.ToString().TrimEnd();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = string.Empty;

        var start = StartTimeBox.Text.Trim();
        var end = EndTimeBox.Text.Trim();

        if (!ScheduleService.IsValidTimeRange(start, end))
        {
            StatusText.Text = "Horario inválido. Usa formato HH:mm (ejemplo: 02:00 y 07:00).";
            return;
        }

        var password = PasswordBox.Password;
        var confirm = ConfirmPasswordBox.Password;

        if (password.Length < 4)
        {
            StatusText.Text = "La contraseña debe tener al menos 4 caracteres.";
            return;
        }

        if (password != confirm)
        {
            StatusText.Text = "Las contraseñas no coinciden.";
            return;
        }

        var config = new AppConfig
        {
            IsConfigured = true,
            Enabled = true,
            RunAtStartup = StartupCheckBox.IsChecked == true,
            CreateAdminDesktopShortcut = DesktopShortcutCheckBox.IsChecked == true,
            BlockStartTime = start,
            BlockEndTime = end,
            DisplayMode = MessageDisplayMode.Slideshow,
            SlideIntervalSeconds = 30,
            Messages = MessageService.CreateDefaultMessages(),
            AdminPasswordHash = PasswordService.HashPassword(password)
        };

        MessageService.SyncLegacyFields(config);
        _configService.Save(config);

        var executablePath = ShortcutService.ResolveExecutablePath();
        StartupService.SetEnabled(config.RunAtStartup, executablePath);

        _onCompleted(config);
        Close();
    }
}
