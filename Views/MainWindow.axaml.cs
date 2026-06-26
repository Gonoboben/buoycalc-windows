using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BuoyCalc.Windows.ViewModels;

namespace BuoyCalc.Windows.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        DataContext = new MainWindowViewModel();
    }
}
