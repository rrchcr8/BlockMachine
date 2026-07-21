using System.Drawing;
using System.Windows;
using BlockMachine.Models;
using BlockMachine.Services;
using BlockMachine.Windows;
using Forms = System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace BlockMachine;

public partial class App : System.Windows.Application
{
    private const string MutexName = "BlockMachine_SingleInstance_Mutex";
    private Mutex? _mutex;
    private ConfigService? _configService;
    private BlockMonitorService? _blockMonitor;
    private Forms.NotifyIcon? _notifyIcon;
    private AppConfig _config = new();
    private bool _testBlockActive;
    private bool _openAdminOnStartup;
    private EventWaitHandle? _openAdminEvent;
    private CancellationTokenSource? _adminListenerCts;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var isAdminLaunch = AdminPortalService.IsAdminLaunch(e.Args);

        _mutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            if (isAdminLaunch && AdminPortalService.SignalOpenAdminPortal())
            {
                MessageBox.Show(
                    "Block Machine ya está en ejecución.\nSe abrirá el panel de administración.",
                    "Block Machine",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(
                    isAdminLaunch
                        ? "Block Machine ya está en ejecución. Usa el icono de la bandeja o el acceso directo del escritorio."
                        : "Block Machine ya está en ejecución.",
                    "Block Machine",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            Shutdown();
            return;
        }

        _openAdminOnStartup = isAdminLaunch;
        _configService = new ConfigService();
        _config = _configService.Load();

        _blockMonitor = new BlockMonitorService(_configService, _config);
        _blockMonitor.Start();

        InitializeTrayIcon();
        StartAdminPortalListener();

        if (!_config.IsConfigured || string.IsNullOrEmpty(_config.AdminPasswordHash))
        {
            ShowSetup();
        }
        else if (_openAdminOnStartup)
        {
            Dispatcher.BeginInvoke(OpenSettingsWithPassword);
        }
        else if (_config.CreateAdminDesktopShortcut && !ShortcutService.AdminDesktopShortcutExists())
        {
            TryCreateAdminShortcut(_config);
        }
    }

    private void InitializeTrayIcon()
    {
        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "Block Machine",
            Icon = SystemIcons.Shield,
            Visible = true
        };

        _notifyIcon.DoubleClick += (_, _) => OpenSettingsWithPassword();

        UpdateTrayMenu();
    }

    private void StartAdminPortalListener()
    {
        _openAdminEvent = AdminPortalService.CreateOpenAdminListener();
        _adminListenerCts = new CancellationTokenSource();

        Task.Run(() =>
        {
            while (!_adminListenerCts.Token.IsCancellationRequested)
            {
                if (!_openAdminEvent.WaitOne(TimeSpan.FromSeconds(1)))
                {
                    continue;
                }

                Dispatcher.BeginInvoke(OpenSettingsWithPassword);
            }
        }, _adminListenerCts.Token);
    }

    private void UpdateTrayMenu()
    {
        if (_notifyIcon is null)
        {
            return;
        }

        var menu = new Forms.ContextMenuStrip();

        var statusItem = new Forms.ToolStripMenuItem(GetTrayStatusText())
        {
            Enabled = false
        };
        menu.Items.Add(statusItem);
        menu.Items.Add(new Forms.ToolStripSeparator());

        menu.Items.Add("Panel de administración...", null, (_, _) => OpenSettingsWithPassword());
        menu.Items.Add("Probar bloqueo ahora", null, (_, _) => ToggleTestBlock());
        menu.Items.Add("Reactivar bloqueo automático", null, (_, _) => ReactivateAutoBlock());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Salir", null, (_, _) => ExitWithPassword());

        _notifyIcon.ContextMenuStrip = menu;
    }

    private string GetTrayStatusText()
    {
        if (_testBlockActive)
        {
            return "Estado: prueba de bloqueo activa";
        }

        if (!_config.Enabled)
        {
            return "Estado: bloqueo desactivado";
        }

        if (ScheduleService.IsBlockedNow(_config))
        {
            return $"Estado: bloqueando ({_config.BlockStartTime}-{_config.BlockEndTime})";
        }

        return "Estado: activo, fuera de horario";
    }

    private void ShowSetup()
    {
        var setup = new SetupWindow(_configService!, config =>
        {
            _config = config;
            _blockMonitor!.UpdateConfig(_config);
            TryCreateAdminShortcut(config);
            UpdateTrayMenu();
        });

        setup.ShowDialog();
    }

    private static void TryCreateAdminShortcut(AppConfig config)
    {
        if (!config.CreateAdminDesktopShortcut)
        {
            return;
        }

        try
        {
            ShortcutService.CreateAdminDesktopShortcut();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se pudo crear el acceso directo en el escritorio:\n{ex.Message}",
                "Block Machine",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void OpenSettingsWithPassword()
    {
        if (!PasswordDialog.TryPrompt(
                "Contraseña de administrador para abrir el panel:",
                out var password))
        {
            return;
        }

        _config = _configService!.Load();

        if (!PasswordService.VerifyPassword(password, _config.AdminPasswordHash))
        {
            MessageBox.Show(
                "Contraseña incorrecta.",
                "Block Machine",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var settings = new SettingsWindow(
            _configService,
            _config,
            onSaved: config =>
            {
                _config = config;
                _blockMonitor!.SetManualOverride(false);
                _testBlockActive = false;
                _blockMonitor.SetTestMode(false);
                _blockMonitor.UpdateConfig(_config);
                TryCreateAdminShortcut(config);
                UpdateTrayMenu();
            },
            onTestBlock: ToggleTestBlock);

        settings.ShowDialog();
        UpdateTrayMenu();
    }

    private void ToggleTestBlock()
    {
        _testBlockActive = !_testBlockActive;
        _blockMonitor!.SetManualOverride(false);
        _blockMonitor.SetTestMode(_testBlockActive);
        UpdateTrayMenu();
    }

    private void ReactivateAutoBlock()
    {
        _testBlockActive = false;
        _blockMonitor!.SetManualOverride(false);
        _blockMonitor.SetTestMode(false);
        _blockMonitor.Refresh();
        UpdateTrayMenu();

        MessageBox.Show(
            "El bloqueo automático por horario está activo de nuevo.",
            "Block Machine",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ExitWithPassword()
    {
        _config = _configService!.Load();
        var password = string.Empty;

        if (_config.IsConfigured &&
            !PasswordDialog.TryPrompt(
                "Contraseña de administrador para salir:",
                out password))
        {
            return;
        }

        if (_config.IsConfigured &&
            !PasswordService.VerifyPassword(password, _config.AdminPasswordHash))
        {
            MessageBox.Show(
                "Contraseña incorrecta.",
                "Block Machine",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        ShutdownApplication();
    }

    private void ShutdownApplication()
    {
        _adminListenerCts?.Cancel();
        _openAdminEvent?.Dispose();
        _notifyIcon!.Visible = false;
        _notifyIcon.Dispose();
        _blockMonitor!.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _adminListenerCts?.Cancel();
        _openAdminEvent?.Dispose();
        _notifyIcon?.Dispose();
        _blockMonitor?.Dispose();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
