using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SiliPuTTY;

public partial class MainWindow : Window
{
    private int _sessionNumber;

    public MainWindow()
    {
        InitializeComponent();
        AddSession();
    }

    private void AddSession()
    {
        var session = new SessionView();
        var name = new TextBox
        {
            Text = $"Session {++_sessionNumber}", MinWidth = 74, MaxWidth = 180,
            Background = Brushes.Transparent, Foreground = new SolidColorBrush(Color.FromRgb(220, 231, 238)),
            BorderThickness = new Thickness(0), Padding = new Thickness(3, 1, 3, 1)
        };
        var close = new Button
        {
            Content = "×", ToolTip = "Close session", Padding = new Thickness(5, 0, 5, 1),
            Margin = new Thickness(5, 0, 0, 0), Background = Brushes.Transparent,
            BorderThickness = new Thickness(0), FontSize = 16
        };
        var header = new StackPanel { Orientation = Orientation.Horizontal };
        header.Children.Add(name); header.Children.Add(close);
        var tab = new TabItem { Header = header, Content = session };
        close.Click += (_, _) => CloseSession(tab);
        session.TitleSuggested += title => name.Text = title;
        name.TextChanged += (_, _) => { if (SessionTabs.SelectedItem == tab) Title = $"SiliPuTTY — {name.Text}"; };
        SessionTabs.Items.Add(tab);
        SessionTabs.SelectedItem = tab;
        name.SelectAll();
    }

    private void CloseSession(TabItem tab)
    {
        var index = SessionTabs.Items.IndexOf(tab);
        if (tab.Content is SessionView session) session.Shutdown();
        SessionTabs.Items.Remove(tab);
        if (SessionTabs.Items.Count == 0) AddSession();
        else SessionTabs.SelectedIndex = Math.Min(index, SessionTabs.Items.Count - 1);
    }

    private void NewTab_Click(object sender, RoutedEventArgs e) => AddSession();
    private void Configure_Click(object sender, RoutedEventArgs e) => new ConfigurationWindow { Owner = this }.ShowDialog();
    private void Network_Click(object sender, RoutedEventArgs e) => new NetworkCenterWindow { Owner = this }.Show();

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
        if (e.Key == Key.T) { AddSession(); e.Handled = true; }
        else if (e.Key == Key.W && SessionTabs.SelectedItem is TabItem tab) { CloseSession(tab); e.Handled = true; }
        else if (e.Key == Key.Tab && SessionTabs.Items.Count > 1)
        {
            var direction = (Keyboard.Modifiers & ModifierKeys.Shift) != 0 ? -1 : 1;
            SessionTabs.SelectedIndex = (SessionTabs.SelectedIndex + direction + SessionTabs.Items.Count) % SessionTabs.Items.Count;
            e.Handled = true;
        }
    }

    private void SessionTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SessionTabs.SelectedItem is TabItem { Header: StackPanel header } && header.Children[0] is TextBox name)
            Title = $"SiliPuTTY — {name.Text}";
    }
}
