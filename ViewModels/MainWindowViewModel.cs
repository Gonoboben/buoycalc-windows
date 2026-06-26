using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

namespace BuoyCalc.Windows.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private string _projectName = "Тестовый проект";
    private string _waterDensity = "1025";
    private string _depth = "50";
    private string _currentSpeed = "0.5";
    private string _waveHeight = "1.0";
    private string _wavePeriod = "6.0";
    private string _buoyVolume = "0.50";
    private string _buoyWeight = "80";
    private string _buoyArea = "0.5";
    private string _buoyCd = "0.8";
    private string _anchorWeight = "500";
    private string _anchorVolume = "0.20";
    private string _anchorCoefficient = "1.0";
    private string _safetyFactor = "5";
    private string _resultText = "Нажмите «Рассчитать».";
    private string _reportText = "";

    public MainWindowViewModel()
    {
        AssemblyItems = new ObservableCollection<AssemblyItemViewModel>
        {
            new() { Kind = "Connector", Title = "Скоба под буем", ConnectorPresetId = "shackle_55", Count = "1" },
            new() { Kind = "Line", Title = "Верхний буйреп", RopePresetId = "polyester_20", LengthM = "45" },
            new() { Kind = "Connector", Title = "Вертлюг", ConnectorPresetId = "swivel_60", Count = "1" },
            new() { Kind = "Payload", Title = "ADCP", PayloadWeightAirKg = "40", PayloadProjectedAreaM2 = "0.05", PayloadDragCoefficient = "1.0" },
            new() { Kind = "Line", Title = "Нижняя цепь", RopePresetId = "chain_10", LengthM = "10" }
        };

        CalculateCommand = new RelayCommand(Calculate);
        AddLineCommand = new RelayCommand(() => AssemblyItems.Add(new AssemblyItemViewModel { Kind = "Line", Title = "Новый участок линии" }));
        AddConnectorCommand = new RelayCommand(() => AssemblyItems.Add(new AssemblyItemViewModel { Kind = "Connector", Title = "Новый соединитель" }));
        AddPayloadCommand = new RelayCommand(() => AssemblyItems.Add(new AssemblyItemViewModel { Kind = "Payload", Title = "Новый прибор", PayloadWeightAirKg = "10", PayloadProjectedAreaM2 = "0.02" }));
    }

    public ObservableCollection<AssemblyItemViewModel> AssemblyItems { get; }
    public ICommand CalculateCommand { get; }
    public ICommand AddLineCommand { get; }
    public ICommand AddConnectorCommand { get; }
    public ICommand AddPayloadCommand { get; }

    public string ProjectName { get => _projectName; set => SetProperty(ref _projectName, value); }
    public string WaterDensity { get => _waterDensity; set => SetProperty(ref _waterDensity, value); }
    public string Depth { get => _depth; set => SetProperty(ref _depth, value); }
    public string CurrentSpeed { get => _currentSpeed; set => SetProperty(ref _currentSpeed, value); }
    public string WaveHeight { get => _waveHeight; set => SetProperty(ref _waveHeight, value); }
    public string WavePeriod { get => _wavePeriod; set => SetProperty(ref _wavePeriod, value); }
    public string BuoyVolume { get => _buoyVolume; set => SetProperty(ref _buoyVolume, value); }
    public string BuoyWeight { get => _buoyWeight; set => SetProperty(ref _buoyWeight, value); }
    public string BuoyArea { get => _buoyArea; set => SetProperty(ref _buoyArea, value); }
    public string BuoyCd { get => _buoyCd; set => SetProperty(ref _buoyCd, value); }
    public string AnchorWeight { get => _anchorWeight; set => SetProperty(ref _anchorWeight, value); }
    public string AnchorVolume { get => _anchorVolume; set => SetProperty(ref _anchorVolume, value); }
    public string AnchorCoefficient { get => _anchorCoefficient; set => SetProperty(ref _anchorCoefficient, value); }
    public string SafetyFactor { get => _safetyFactor; set => SetProperty(ref _safetyFactor, value); }
    public string ResultText { get => _resultText; set => SetProperty(ref _resultText, value); }
    public string ReportText { get => _reportText; set => SetProperty(ref _reportText, value); }

    private void Calculate()
    {
        var environment = new EnvironmentInput(
            Parse(WaterDensity),
            Parse(Depth),
            Parse(CurrentSpeed),
            Parse(WaveHeight),
            Parse(WavePeriod),
            SeabedCatalog.ById("unknown"));

        var buoy = new BuoyInput("Буй", Parse(BuoyVolume), Parse(BuoyWeight), Parse(BuoyArea), Parse(BuoyCd));
        var anchor = new AnchorInput("Якорь", "Deadweight", "Concrete", Parse(AnchorWeight), Parse(AnchorVolume), Parse(AnchorCoefficient));
        var items = AssemblyItems.Select(x => x.ToInput()).ToList();

        var result = BuoyCalculator.Calculate(environment, buoy, items, anchor, Parse(SafetyFactor));

        ResultText = $"Вердикт: {result.Verdict}\nГлавный риск: {result.MainRisk}\nПлавучесть: {result.NetBuoyancyKg:0.##} кг\nНатяжение: {result.TensionKn:0.##} кН\nЗапас якоря: {result.AnchorReserve:0.##}";
        ReportText = ReportBuilder.Build(ProjectName, environment, buoy, anchor, result);
    }

    private static double Parse(string value)
    {
        value = (value ?? string.Empty).Replace(',', '.');
        return double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : 0;
    }
}
