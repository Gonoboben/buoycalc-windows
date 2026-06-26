using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace BuoyCalc.Windows.Views;

public partial class SequencePreviewWindow : Window
{
    public SequencePreviewWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void BackButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void ConfirmButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }
}
