using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    private string _sequenceSummary = "";
    private string _projectStatusText = "Проект ещё не сохранён.";

    public MainWindowViewModel()
    {
        AssemblyItems = new ObservableCollection<AssemblyItemViewModel>();

        CalculateCommand = new RelayCommand(Calculate);
        AddLineCommand = new RelayCommand(() => AddAssemblyItem(new AssemblyItemViewModel { Kind = "Line", Title = "Новый участок линии" }));
        AddConnectorCommand = new RelayCommand(() => AddAssemblyItem(new AssemblyItemViewModel { Kind = "Connector", Title = "Новый соединитель" }));
        AddPayloadCommand = new RelayCommand(() => AddAssemblyItem(new AssemblyItemViewModel { Kind = "Payload", Title = "Новый прибор", PayloadWeightAirKg = "10", PayloadProjectedAreaM2 = "0.02" }));
        NewProjectCommand = new RelayCommand(NewProject);
        SaveProjectCommand = new RelayCommand(SaveProject);
        LoadProjectCommand = new RelayCommand(LoadProject);

        ResetToDefaultProject();
    }

    public ObservableCollection<AssemblyItemViewModel> AssemblyItems { get; }
    public ICommand CalculateCommand { get; }
    public ICommand AddLineCommand { get; }
    public ICommand AddConnectorCommand { get; }
    public ICommand AddPayloadCommand { get; }
    public ICommand NewProjectCommand { get; }
    public ICommand SaveProjectCommand { get; }
    public ICommand LoadProjectCommand { get; }

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
    public string SequenceSummary { get => _sequenceSummary; set => SetProperty(ref _sequenceSummary, value); }
    public string ProjectStatusText { get => _projectStatusText; set => SetProperty(ref _projectStatusText, value); }

    private void AddAssemblyItem(AssemblyItemViewModel item)
    {
        WireItem(item);
        AssemblyItems.Add(item);
        UpdateSequenceSummary();
    }

    private void ClearAssemblyItems()
    {
        foreach (var item in AssemblyItems)
        {
            item.PropertyChanged -= OnAssemblyItemChanged;
            item.RemoveRequested -= RemoveItem;
            item.MoveUpRequested -= MoveItemUp;
            item.MoveDownRequested -= MoveItemDown;
            item.DuplicateRequested -= DuplicateItem;
        }

        AssemblyItems.Clear();
    }

    private void WireItem(AssemblyItemViewModel item)
    {
        item.RemoveRequested += RemoveItem;
        item.MoveUpRequested += MoveItemUp;
        item.MoveDownRequested += MoveItemDown;
        item.DuplicateRequested += DuplicateItem;
        item.PropertyChanged += OnAssemblyItemChanged;
    }

    private void OnAssemblyItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateSequenceSummary();
    }

    private void RemoveItem(AssemblyItemViewModel item)
    {
        item.PropertyChanged -= OnAssemblyItemChanged;
        item.RemoveRequested -= RemoveItem;
        item.MoveUpRequested -= MoveItemUp;
        item.MoveDownRequested -= MoveItemDown;
        item.DuplicateRequested -= DuplicateItem;
        AssemblyItems.Remove(item);
        UpdateSequenceSummary();
    }

    private void MoveItemUp(AssemblyItemViewModel item)
    {
        var index = AssemblyItems.IndexOf(item);
        if (index <= 0)
        {
            return;
        }

        AssemblyItems.Move(index, index - 1);
        UpdateSequenceSummary();
    }

    private void MoveItemDown(AssemblyItemViewModel item)
    {
        var index = AssemblyItems.IndexOf(item);
        if (index < 0 || index >= AssemblyItems.Count - 1)
        {
            return;
        }

        AssemblyItems.Move(index, index + 1);
        UpdateSequenceSummary();
    }

    private void DuplicateItem(AssemblyItemViewModel item)
    {
        var index = AssemblyItems.IndexOf(item);
        var copy = item.Clone();
        WireItem(copy);

        if (index < 0 || index >= AssemblyItems.Count - 1)
        {
            AssemblyItems.Add(copy);
        }
        else
        {
            AssemblyItems.Insert(index + 1, copy);
        }

        UpdateSequenceSummary();
    }

    private void NewProject()
    {
        ResetToDefaultProject();
        ProjectStatusText = "Создан новый проект на основе стандартного шаблона.";
    }

    private void ResetToDefaultProject()
    {
        ProjectName = "Тестовый проект";
        WaterDensity = "1025";
        Depth = "50";
        CurrentSpeed = "0.5";
        WaveHeight = "1.0";
        WavePeriod = "6.0";
        BuoyVolume = "0.50";
        BuoyWeight = "80";
        BuoyArea = "0.5";
        BuoyCd = "0.8";
        AnchorWeight = "500";
        AnchorVolume = "0.20";
        AnchorCoefficient = "1.0";
        SafetyFactor = "5";
        ResultText = "Нажмите «Рассчитать».";
        ReportText = "";

        ClearAssemblyItems();
        AddAssemblyItem(new AssemblyItemViewModel { Kind = "Connector", Title = "Скоба под буем", ConnectorPresetId = "shackle_55", Count = "1" });
        AddAssemblyItem(new AssemblyItemViewModel { Kind = "Line", Title = "Верхний буйреп", RopePresetId = "polyester_20", LengthM = "45" });
        AddAssemblyItem(new AssemblyItemViewModel { Kind = "Connector", Title = "Вертлюг", ConnectorPresetId = "swivel_60", Count = "1" });
        AddAssemblyItem(new AssemblyItemViewModel { Kind = "Payload", Title = "ADCP", PayloadWeightAirKg = "40", PayloadProjectedAreaM2 = "0.05", PayloadDragCoefficient = "1.0" });
        AddAssemblyItem(new AssemblyItemViewModel { Kind = "Line", Title = "Нижняя цепь", RopePresetId = "chain_10", LengthM = "10" });

        UpdateSequenceSummary();
    }

    private void SaveProject()
    {
        try
        {
            var dto = ToDto();
            ProjectJsonStorage.Save(dto);
            ProjectStatusText = $"Проект сохранён: {ProjectJsonStorage.ProjectPath}";
        }
        catch (Exception ex)
        {
            ProjectStatusText = $"Ошибка сохранения: {ex.Message}";
        }
    }

    private void LoadProject()
    {
        try
        {
            var dto = ProjectJsonStorage.Load();
            if (dto is null)
            {
                ProjectStatusText = $"Файл проекта не найден: {ProjectJsonStorage.ProjectPath}";
                return;
            }

            FromDto(dto);
            ProjectStatusText = $"Проект загружен: {ProjectJsonStorage.ProjectPath}";
        }
        catch (Exception ex)
        {
            ProjectStatusText = $"Ошибка загрузки: {ex.Message}";
        }
    }

    private BuoyProjectDto ToDto()
    {
        return new BuoyProjectDto
        {
            ProjectName = ProjectName,
            WaterDensity = WaterDensity,
            Depth = Depth,
            CurrentSpeed = CurrentSpeed,
            WaveHeight = WaveHeight,
            WavePeriod = WavePeriod,
            BuoyVolume = BuoyVolume,
            BuoyWeight = BuoyWeight,
            BuoyArea = BuoyArea,
            BuoyCd = BuoyCd,
            AnchorWeight = AnchorWeight,
            AnchorVolume = AnchorVolume,
            AnchorCoefficient = AnchorCoefficient,
            SafetyFactor = SafetyFactor,
            AssemblyItems = AssemblyItems.Select(x => new AssemblyItemDto
            {
                IsEnabled = x.IsEnabled,
                Kind = x.Kind,
                Title = x.Title,
                RopePresetId = x.RopePresetId,
                ConnectorPresetId = x.ConnectorPresetId,
                LengthM = x.LengthM,
                Count = x.Count,
                PayloadWeightAirKg = x.PayloadWeightAirKg,
                PayloadVolumeM3 = x.PayloadVolumeM3,
                PayloadProjectedAreaM2 = x.PayloadProjectedAreaM2,
                PayloadDragCoefficient = x.PayloadDragCoefficient
            }).ToList()
        };
    }

    private void FromDto(BuoyProjectDto dto)
    {
        ProjectName = dto.ProjectName;
        WaterDensity = dto.WaterDensity;
        Depth = dto.Depth;
        CurrentSpeed = dto.CurrentSpeed;
        WaveHeight = dto.WaveHeight;
        WavePeriod = dto.WavePeriod;
        BuoyVolume = dto.BuoyVolume;
        BuoyWeight = dto.BuoyWeight;
        BuoyArea = dto.BuoyArea;
        BuoyCd = dto.BuoyCd;
        AnchorWeight = dto.AnchorWeight;
        AnchorVolume = dto.AnchorVolume;
        AnchorCoefficient = dto.AnchorCoefficient;
        SafetyFactor = dto.SafetyFactor;
        ResultText = "Проект загружен. Нажмите «Рассчитать».";
        ReportText = "";

        ClearAssemblyItems();

        foreach (var item in dto.AssemblyItems)
        {
            AddAssemblyItem(new AssemblyItemViewModel
            {
                IsEnabled = item.IsEnabled,
                Kind = item.Kind,
                Title = item.Title,
                RopePresetId = item.RopePresetId,
                ConnectorPresetId = item.ConnectorPresetId,
                LengthM = item.LengthM,
                Count = item.Count,
                PayloadWeightAirKg = item.PayloadWeightAirKg,
                PayloadVolumeM3 = item.PayloadVolumeM3,
                PayloadProjectedAreaM2 = item.PayloadProjectedAreaM2,
                PayloadDragCoefficient = item.PayloadDragCoefficient
            });
        }

        UpdateSequenceSummary();
    }

    private void UpdateSequenceSummary()
    {
        var enabledItems = AssemblyItems.Where(x => x.IsEnabled).Select(x => x.ToInput()).ToList();

        var lineLengthM = enabledItems
            .Where(x => x.Kind == AssemblyItemKind.Line)
            .Sum(x => x.LengthM);

        var connectorCount = enabledItems
            .Where(x => x.Kind == AssemblyItemKind.Connector)
            .Sum(x => x.Count);

        var payloadWeightKg = enabledItems
            .Where(x => x.Kind == AssemblyItemKind.Payload)
            .Sum(x => x.PayloadWeightAirKg);

        SequenceSummary = $"Активных элементов: {enabledItems.Count} · линия: {lineLengthM:0.##} м · соединителей: {connectorCount} шт. · приборы: {payloadWeightKg:0.##} кг";
    }

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
        UpdateSequenceSummary();
    }

    private static double Parse(string value)
    {
        value = (value ?? string.Empty).Replace(',', '.');
        return double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : 0;
    }
}
