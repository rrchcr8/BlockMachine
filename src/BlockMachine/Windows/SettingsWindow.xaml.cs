using System.Windows;
using System.Windows.Controls;
using BlockMachine.Models;
using BlockMachine.Services;

namespace BlockMachine.Windows;

public partial class SettingsWindow : Window
{
    private readonly ConfigService _configService;
    private readonly AppConfig _config;
    private readonly Action<AppConfig>? _onSaved;
    private readonly Action? _onTestBlock;
    private readonly List<BlockMessage> _messages = [];

    public SettingsWindow(
        ConfigService configService,
        AppConfig config,
        Action<AppConfig>? onSaved = null,
        Action? onTestBlock = null)
    {
        InitializeComponent();

        _configService = configService;
        _config = config;
        _onSaved = onSaved;
        _onTestBlock = onTestBlock;

        LoadFields();
    }

    private void LoadFields()
    {
        MessageService.Normalize(_config);

        StartTimeBox.Text = _config.BlockStartTime;
        EndTimeBox.Text = _config.BlockEndTime;
        EnabledCheckBox.IsChecked = _config.Enabled;
        StartupCheckBox.IsChecked = _config.RunAtStartup;
        DesktopShortcutCheckBox.IsChecked = _config.CreateAdminDesktopShortcut;

        SingleModeRadio.IsChecked = _config.DisplayMode == MessageDisplayMode.Single;
        SlideshowModeRadio.IsChecked = _config.DisplayMode == MessageDisplayMode.Slideshow;
        SlideIntervalBox.Text = _config.SlideIntervalSeconds.ToString();
        UpdateSlideIntervalPanelState();

        _messages.Clear();
        foreach (var message in _config.Messages)
        {
            _messages.Add(CloneMessage(message));
        }

        RenderMessages();
    }

    private void RenderMessages()
    {
        MessagesPanel.Items.Clear();

        foreach (var message in _messages)
        {
            MessagesPanel.Items.Add(CreateMessageEditor(message));
        }
    }

    private Border CreateMessageEditor(BlockMessage message)
    {
        var enabledCheckBox = new System.Windows.Controls.CheckBox
        {
            Content = "Mostrar este mensaje",
            IsChecked = message.IsEnabled,
            Margin = new Thickness(0, 0, 0, 8)
        };
        enabledCheckBox.Checked += (_, _) => message.IsEnabled = true;
        enabledCheckBox.Unchecked += (_, _) => message.IsEnabled = false;

        var titleLabel = new TextBlock { Text = "Título", Margin = new Thickness(0, 0, 0, 4) };
        var titleBox = new System.Windows.Controls.TextBox
        {
            Text = message.Title,
            Height = 32,
            Margin = new Thickness(0, 0, 0, 8)
        };
        titleBox.TextChanged += (_, _) => message.Title = titleBox.Text;

        var bodyLabel = new TextBlock { Text = "Mensaje", Margin = new Thickness(0, 0, 0, 4) };
        var bodyBox = new System.Windows.Controls.TextBox
        {
            Text = message.Body,
            Height = 72,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(0, 0, 0, 8)
        };
        bodyBox.TextChanged += (_, _) => message.Body = bodyBox.Text;

        var removeButton = new System.Windows.Controls.Button
        {
            Content = "Eliminar mensaje",
            Height = 28,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left
        };
        removeButton.Click += (_, _) =>
        {
            _messages.Remove(message);
            RenderMessages();
        };

        var panel = new StackPanel();
        panel.Children.Add(enabledCheckBox);
        panel.Children.Add(titleLabel);
        panel.Children.Add(titleBox);
        panel.Children.Add(bodyLabel);
        panel.Children.Add(bodyBox);
        panel.Children.Add(removeButton);

        return new Border
        {
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(221, 221, 221)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 10),
            Child = panel
        };
    }

    private void AddMessage_Click(object sender, RoutedEventArgs e)
    {
        _messages.Add(new BlockMessage
        {
            Title = "Nuevo mensaje",
            Body = "Escribe aquí el texto para tu papá.",
            IsEnabled = true
        });

        RenderMessages();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = string.Empty;

        var start = StartTimeBox.Text.Trim();
        var end = EndTimeBox.Text.Trim();

        if (!ScheduleService.IsValidTimeRange(start, end))
        {
            StatusText.Text = "Horario inválido. Usa formato HH:mm y evita que inicio y fin sean iguales.";
            return;
        }

        if (!MessageService.HasValidMessages(_messages))
        {
            StatusText.Text = "Debes tener al menos un mensaje activo con texto.";
            return;
        }

        if (!MessageService.TryParseSlideInterval(SlideIntervalBox.Text, out var slideInterval))
        {
            StatusText.Text = "Intervalo inválido. Usa un número entre 5 y 600 segundos.";
            return;
        }

        var newPassword = NewPasswordBox.Password;
        var confirmPassword = ConfirmPasswordBox.Password;

        if (!string.IsNullOrEmpty(newPassword) || !string.IsNullOrEmpty(confirmPassword))
        {
            if (newPassword.Length < 4)
            {
                StatusText.Text = "La contraseña debe tener al menos 4 caracteres.";
                return;
            }

            if (newPassword != confirmPassword)
            {
                StatusText.Text = "Las contraseñas no coinciden.";
                return;
            }

            _config.AdminPasswordHash = PasswordService.HashPassword(newPassword);
        }
        else if (string.IsNullOrEmpty(_config.AdminPasswordHash))
        {
            StatusText.Text = "Debes establecer una contraseña de administrador.";
            return;
        }

        _config.BlockStartTime = start;
        _config.BlockEndTime = end;
        _config.Enabled = EnabledCheckBox.IsChecked == true;
        _config.RunAtStartup = StartupCheckBox.IsChecked == true;
        _config.CreateAdminDesktopShortcut = DesktopShortcutCheckBox.IsChecked == true;
        _config.DisplayMode = SlideshowModeRadio.IsChecked == true
            ? MessageDisplayMode.Slideshow
            : MessageDisplayMode.Single;
        _config.SlideIntervalSeconds = slideInterval;
        _config.Messages = _messages.Select(CloneMessage).ToList();
        _config.IsConfigured = true;

        MessageService.SyncLegacyFields(_config);
        _configService.Save(_config);

        var executablePath = ShortcutService.ResolveExecutablePath();
        StartupService.SetEnabled(_config.RunAtStartup, executablePath);

        if (_config.CreateAdminDesktopShortcut)
        {
            try
            {
                ShortcutService.CreateAdminDesktopShortcut(executablePath);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Guardado, pero no se pudo crear el acceso directo: {ex.Message}";
            }
        }
        else
        {
            ShortcutService.RemoveAdminDesktopShortcut();
        }

        _onSaved?.Invoke(_config);

        System.Windows.MessageBox.Show(
            this,
            "Configuración guardada correctamente.",
            "Block Machine",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        NewPasswordBox.Clear();
        ConfirmPasswordBox.Clear();
    }

    private void DisplayMode_Changed(object sender, RoutedEventArgs e)
    {
        UpdateSlideIntervalPanelState();
    }

    private void UpdateSlideIntervalPanelState()
    {
        if (SlideIntervalPanel is null || SlideshowModeRadio is null)
        {
            return;
        }

        var slideshow = SlideshowModeRadio.IsChecked == true;
        SlideIntervalPanel.IsEnabled = slideshow;
        SlideIntervalPanel.Opacity = slideshow ? 1 : 0.45;
    }

    private void TestBlock_Click(object sender, RoutedEventArgs e)
    {
        _onTestBlock?.Invoke();
    }

    private static BlockMessage CloneMessage(BlockMessage source)
    {
        return new BlockMessage
        {
            Id = source.Id,
            Title = source.Title,
            Body = source.Body,
            IsEnabled = source.IsEnabled
        };
    }
}
