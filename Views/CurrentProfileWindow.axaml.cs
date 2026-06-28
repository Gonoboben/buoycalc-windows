using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BuoyCalc.Windows.Views;

public partial class CurrentProfileWindow : Window
{
    public CurrentProfileWindow()
    {
        AvaloniaXamlLoader.Load(this);
        WindowVersionHelper.Apply(this, "Профиль течения по глубине");
    }
}
