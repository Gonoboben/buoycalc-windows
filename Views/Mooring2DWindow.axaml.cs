using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace BuoyCalc.Windows.Views;

public partial class Mooring2DWindow : Window
{
    public Mooring2DWindow()
    {
        AvaloniaXamlLoader.Load(this);
        WindowVersionHelper.Apply(this, "2D-схема постановки");
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
