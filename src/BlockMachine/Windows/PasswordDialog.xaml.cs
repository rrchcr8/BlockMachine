using System.Windows;
using System.Windows.Input;

namespace BlockMachine.Windows;

public partial class PasswordDialog : Window
{
    public PasswordDialog(string prompt)
    {
        InitializeComponent();
        PromptText.Text = prompt;
        Loaded += (_, _) => PasswordBox.Focus();
    }

    public string? EnteredPassword { get; private set; }

    public static bool TryPrompt(string prompt, out string password, Window? owner = null)
    {
        var dialog = new PasswordDialog(prompt);
        if (owner is not null)
        {
            dialog.Owner = owner;
        }

        var result = dialog.ShowDialog() == true;
        password = dialog.EnteredPassword ?? string.Empty;
        return result;
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
        EnteredPassword = PasswordBox.Password;
        DialogResult = true;
    }

    private void PasswordBox_OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Accept_Click(sender, e);
        }
    }

    public void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
        PasswordBox.Clear();
        PasswordBox.Focus();
    }
}
