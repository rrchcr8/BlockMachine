using BlockMachine.Models;
using BlockMachine.Windows;

namespace BlockMachine.Services;

public sealed class BlockMonitorService : IDisposable
{
    private readonly ConfigService _configService;
    private readonly List<BlockOverlayWindow> _overlayWindows = [];
    private readonly System.Windows.Threading.DispatcherTimer _timer;
    private AppConfig _config;
    private bool _manualOverrideActive;
    private bool _testModeActive;

    public BlockMonitorService(ConfigService configService, AppConfig initialConfig)
    {
        _configService = configService;
        _config = initialConfig;

        _timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(15)
        };
        _timer.Tick += (_, _) => Refresh();
    }

    public event EventHandler? BlockStateChanged;

    public bool IsBlockingVisible => _overlayWindows.Count > 0;

    public void Start()
    {
        _timer.Start();
        Refresh();
    }

    public void UpdateConfig(AppConfig config)
    {
        _config = config;
        Refresh();
    }

    public void SetManualOverride(bool active)
    {
        _manualOverrideActive = active;
        Refresh();
    }

    public void SetTestMode(bool active)
    {
        _testModeActive = active;
        Refresh();
    }

    public void Refresh()
    {
        _config = _configService.Load();

        var shouldBlock = !_manualOverrideActive &&
                          (_testModeActive || ScheduleService.IsBlockedNow(_config));

        if (!_testModeActive && _manualOverrideActive && !ScheduleService.IsBlockedNow(_config))
        {
            _manualOverrideActive = false;
        }

        if (shouldBlock)
        {
            ShowOverlays();
        }
        else
        {
            HideOverlays();
        }

        BlockStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ShowOverlays()
    {
        if (_overlayWindows.Count > 0)
        {
            UpdateOverlayContent();
            return;
        }

        foreach (var bounds in Helpers.ScreenHelper.GetScreenBounds())
        {
            var window = new BlockOverlayWindow(_config, bounds, OnUnlockRequested);
            window.Closed += (_, _) => _overlayWindows.Remove(window);
            _overlayWindows.Add(window);
            window.Show();
        }
    }

    private void HideOverlays()
    {
        foreach (var window in _overlayWindows.ToList())
        {
            window.AllowClose();
            window.Close();
        }

        _overlayWindows.Clear();
    }

    private void UpdateOverlayContent()
    {
        foreach (var window in _overlayWindows)
        {
            window.UpdateContent(_config);
        }
    }

    private bool OnUnlockRequested(string password)
    {
        var config = _configService.Load();

        if (!PasswordService.VerifyPassword(password, config.AdminPasswordHash))
        {
            return false;
        }

        _manualOverrideActive = true;
        HideOverlays();
        BlockStateChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void Dispose()
    {
        _timer.Stop();
        HideOverlays();
    }
}
