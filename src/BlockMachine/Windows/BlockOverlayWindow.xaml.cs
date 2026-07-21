using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using BlockMachine.Models;
using BlockMachine.Services;

namespace BlockMachine.Windows;

public partial class BlockOverlayWindow : Window
{
    private readonly Func<string, bool> _unlockHandler;
    private AppConfig _config;
    private bool _allowClose;
    private readonly List<BlockMessage> _activeMessages = [];
    private int _currentSlideIndex;
    private readonly System.Windows.Threading.DispatcherTimer _slideTimer;
    private readonly System.Windows.Threading.DispatcherTimer _clockTimer;

    public BlockOverlayWindow(AppConfig config, Rect bounds, Func<string, bool> unlockHandler)
    {
        InitializeComponent();

        _config = config;
        _unlockHandler = unlockHandler;

        Left = bounds.Left;
        Top = bounds.Top;
        Width = bounds.Width;
        Height = bounds.Height;

        _slideTimer = new System.Windows.Threading.DispatcherTimer();
        _slideTimer.Tick += (_, _) => AdvanceSlide();

        _clockTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _clockTimer.Tick += (_, _) => ShowCurrentSlide();

        UpdateContent(config);

        _clockTimer.Start();

        Loaded += (_, _) =>
        {
            Activate();
            Focus();
        };

        Closing += (_, e) =>
        {
            if (!_allowClose)
            {
                e.Cancel = true;
            }
        };
    }

    public void UpdateContent(AppConfig config)
    {
        _config = config;
        RebuildActiveMessages();
        ConfigureSlideTimer();
        ShowCurrentSlide();
        UpdateUntilText();
    }

    public void AllowClose()
    {
        _allowClose = true;
        _slideTimer.Stop();
        _clockTimer.Stop();
    }

    private void RebuildActiveMessages()
    {
        _activeMessages.Clear();
        _activeMessages.AddRange(MessageService.GetEnabledMessages(_config));

        if (_currentSlideIndex >= _activeMessages.Count)
        {
            _currentSlideIndex = 0;
        }
    }

    private void ConfigureSlideTimer()
    {
        _slideTimer.Stop();

        if (_config.DisplayMode != MessageDisplayMode.Slideshow || _activeMessages.Count <= 1)
        {
            return;
        }

        _slideTimer.Interval = TimeSpan.FromSeconds(Math.Max(5, _config.SlideIntervalSeconds));
        _slideTimer.Start();
    }

    private void ShowCurrentSlide(bool animate = false)
    {
        if (_activeMessages.Count == 0)
        {
            TitleText.Text = _config.Title;
            MessageText.Text = MessageService.FormatText(_config.Message);
            SlideIndicator.Visibility = Visibility.Collapsed;
            return;
        }

        var message = _config.DisplayMode == MessageDisplayMode.Single
            ? _activeMessages[0]
            : _activeMessages[_currentSlideIndex];

        void ApplyContent()
        {
            TitleText.Text = MessageService.FormatText(message.Title);
            MessageText.Text = MessageService.FormatText(message.Body);

            if (_config.DisplayMode == MessageDisplayMode.Slideshow && _activeMessages.Count > 1)
            {
                SlideIndicator.Text = $"{_currentSlideIndex + 1} / {_activeMessages.Count}";
                SlideIndicator.Visibility = Visibility.Visible;
            }
            else
            {
                SlideIndicator.Visibility = Visibility.Collapsed;
            }

            UpdateUntilText();
        }

        if (animate)
        {
            AnimateTransition(ApplyContent);
        }
        else
        {
            ApplyContent();
        }
    }

    private void AdvanceSlide()
    {
        if (_config.DisplayMode != MessageDisplayMode.Slideshow || _activeMessages.Count <= 1)
        {
            return;
        }

        _currentSlideIndex = (_currentSlideIndex + 1) % _activeMessages.Count;
        ShowCurrentSlide(animate: true);
    }

    private void UpdateUntilText()
    {
        if (ScheduleService.IsBlockedNow(_config))
        {
            var remaining = ScheduleService.TimeUntilUnblock(_config);
            UntilText.Text = $"Podrás usar la computadora a las {_config.BlockEndTime} " +
                             $"(en aprox. {remaining.Hours} h {remaining.Minutes} min)";
        }
        else
        {
            UntilText.Text = string.Empty;
        }
    }

    private void AnimateTransition(Action applyContent)
    {
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(350));
        fadeOut.Completed += (_, _) =>
        {
            applyContent();
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(350));
            ContentPanel.BeginAnimation(OpacityProperty, fadeIn);
        };

        ContentPanel.BeginAnimation(OpacityProperty, fadeOut);
    }

    private void Unlock_Click(object sender, RoutedEventArgs e)
    {
        if (PasswordDialog.TryPrompt(
                "Ingresa la contraseña para desbloquear temporalmente:",
                out var password,
                this))
        {
            if (_unlockHandler(password))
            {
                return;
            }

            System.Windows.MessageBox.Show(
                this,
                "Contraseña incorrecta.",
                "Block Machine",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void Window_OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;
    }

    private void Window_OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.System && e.SystemKey == Key.F4)
        {
            e.Handled = true;
        }
    }
}
