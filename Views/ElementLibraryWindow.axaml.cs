using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BuoyCalc.Windows.ViewModels;

namespace BuoyCalc.Windows.Views;

public partial class ElementLibraryWindow : Window
{
    public ElementLibraryWindow()
    {
        AvaloniaXamlLoader.Load(this);
        DataContext = new ElementLibraryViewModel();
    }
}
