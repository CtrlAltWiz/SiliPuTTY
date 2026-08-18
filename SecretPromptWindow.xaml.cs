using System.Windows;
using System.Windows.Input;

namespace SillyPutty;

public partial class SecretPromptWindow : Window
{
    public string Secret { get; private set; } = "";
    public SecretPromptWindow() { InitializeComponent(); Loaded += (_, _) => SecretBox.Focus(); }
    private void Send_Click(object sender, RoutedEventArgs e) { Secret = SecretBox.Password; DialogResult = true; }
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    private void SecretBox_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) { Secret = SecretBox.Password; DialogResult = true; } }
}
