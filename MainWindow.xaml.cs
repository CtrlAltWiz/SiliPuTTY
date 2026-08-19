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
        var initialName = $"Session {++_sessionNumber}";
        var name = new TextBlock
        {
            Text = initialName, MinWidth = 74, MaxWidth = 180, Tag = "TabName",
            VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis,
            Foreground = new SolidColorBrush(Color.FromRgb(220, 231, 238)), Margin = new Thickness(3, 1, 3, 1)
        };
        var editor = new TextBox
        {
            Text = initialName, MinWidth = 74, MaxWidth = 180, Tag = "TabNameEditor",
            Visibility = Visibility.Collapsed, BorderThickness = new Thickness(1), Padding = new Thickness(3, 1, 3, 1)
        };
        var edit = new Button();
        if (SessionTabs.TryFindResource("TabEditButtonStyle") is Style editStyle) edit.Style = editStyle;
        var close = new Button
        {
            Content = "×", ToolTip = "Close session", Padding = new Thickness(5, 0, 5, 1),
            Margin = new Thickness(5, 0, 0, 0), Background = Brushes.Transparent,
            BorderThickness = new Thickness(0), FontSize = 16
        };
        var header = new StackPanel { Orientation = Orientation.Horizontal };
        header.Children.Add(name); header.Children.Add(editor); header.Children.Add(edit); header.Children.Add(close);
        var tab = new TabItem { Header = header, Content = session };
        close.Click += (_, _) => CloseSession(tab);
        edit.Click += (_, e) => { e.Handled = true; BeginTabRename(name, editor); };
        editor.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter) { CommitTabRename(tab, name, editor); e.Handled = true; }
            else if (e.Key == Key.Escape) { CancelTabRename(name, editor); e.Handled = true; }
        };
        editor.LostKeyboardFocus += (_, _) => CommitTabRename(tab, name, editor);
        session.TitleSuggested += title => { name.Text = title; editor.Text = title; if (SessionTabs.SelectedItem == tab) Title = $"SiliPuTTY — {title}"; };
        SessionTabs.Items.Add(tab);
        SessionTabs.SelectedItem = tab;
    }

    private static void BeginTabRename(TextBlock name, TextBox editor)
    {
        editor.Text = name.Text; name.Visibility = Visibility.Collapsed; editor.Visibility = Visibility.Visible;
        editor.Focus(); editor.SelectAll();
    }

    private void CommitTabRename(TabItem tab, TextBlock name, TextBox editor)
    {
        if (editor.Visibility != Visibility.Visible) return;
        var value = editor.Text.Trim(); if (value.Length == 0) value = name.Text;
        name.Text = value; editor.Text = value; editor.Visibility = Visibility.Collapsed; name.Visibility = Visibility.Visible;
        if (SessionTabs.SelectedItem == tab) Title = $"SiliPuTTY — {value}";
    }

    private static void CancelTabRename(TextBlock name, TextBox editor)
    { editor.Text = name.Text; editor.Visibility = Visibility.Collapsed; name.Visibility = Visibility.Visible; }

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
        foreach (var item in SessionTabs.Items.OfType<TabItem>())
        {
            if (item.Header is StackPanel tabHeader && tabHeader.Children[0] is TextBlock tabName)
                tabName.Foreground = new SolidColorBrush(item.IsSelected ? Color.FromRgb(176, 56, 217) : Color.FromRgb(220, 231, 238));
        }
        if (SessionTabs.SelectedItem is TabItem { Header: StackPanel header } && header.Children[0] is TextBlock name)
            Title = $"SiliPuTTY — {name.Text}";
    }
}
