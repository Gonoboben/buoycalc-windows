using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

namespace BuoyCalc.Windows.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IProjectFileDialogService? _fileDialogService;

    private string _projectName = "Тестовый проект";
    private string _projectFilePath = ProjectJsonStorage.DefaultProjectPath;
    private string _waterDensity = "1025";
    private string _depth = "50";
    private string _currentSpeed = "0.5";
    private string _waveHeight = "1.0";
    private string _wavePeriod = "6.0";
    private string _buoyName = "Буй";
    private BuoyLibraryItem? _selectedBuoyPreset;
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
    private string _buoyLibraryStatusText = "Библиотека буёв готова.";

    public MainWindowViewModel(IProjectFileDialogService? fileDialogService = null)
    {
        _fileDialogService = fileDialogService;
        AssemblyItems = new ObservableCollection<AssemblyItemViewModel>();
        BuoyPresets = new ObservableCollection<BuoyLibraryItem>();

        CalculateCommand = new RelayCommand(Calculate);
        AddLineCommand = new RelayCommand(() => AddAssemblyItem(new AssemblyItemViewModel { Kind = "Line", Title = "Новый участок линии", RopePresetId = "built-in:polyester_20" }));
        AddConnectorCommand = new RelayCommand(() => AddAssemblyItem(new AssemblyItemViewModel { Kind = "Connector", Title = "Новый соединитель" }));
        AddPayloadCommand = new RelayCommand(() => AddAssemblyItem(new AssemblyItemViewModel { Kind = "Payload", Title = "Новый прибор", PayloadWeightAirKg = "10", PayloadProjectedAreaM2 = "0.02" }));
        NewProjectCommand = new RelayCommand(NewProject);
        SaveProjectCommand = new RelayCommand(async () => await SaveProjectAsync(promptForPath: false));
        SaveProjectAsCommand = new RelayCommand(async () => await SaveProjectAsync(promptForPath: true));
        LoadProjectCommand = new RelayCommand(async () => await LoadProjectAsync());
        SaveBuoyPresetCommand = new RelayCommand(SaveCurrentBuoyToLibrary);
        DeleteBuoyPresetCommand = new RelayCommand(DeleteSelectedBuoyPreset);
        RefreshBuoyLibraryCommand = new RelayCommand(() => RefreshBuoyLibrary(null));

        RefreshBuoyLibrary(null);
        ResetToDefaultProject();
    }

    public ObservableCollection<AssemblyItemViewModel> AssemblyItems { get; }
    public ObservableCollection<BuoyLibraryItem> BuoyPresets { get; }

    public ICommand CalculateCommand { get; }
    public ICommand AddLineCommand { get; }
    public ICommand AddConnectorCommand { get; }
    public ICommand AddPayloadCommand { get; }
    public ICommand NewProjectCommand { get; }
    public ICommand SaveProjectCommand { get; }
    public ICommand SaveProjectAsCommand { get; }
    public ICommand LoadProjectCommand { get; }
    public ICommand SaveBuoyPresetCommand { get; }
    public ICommand DeleteBuoyPresetCommand { get; }
    public ICommand RefreshBuoyLibraryCommand { get; }

    public string ProjectName { get => _projectName; set => SetProperty(ref _projectName, value); }
    public string ProjectFilePath { get => _projectFilePath; set => SetProperty(ref _projectFilePath, value); }
    public string WaterDensity { get => _waterDensity; set => SetProperty(ref _waterDensity, value); }
    public string Depth { get => _depth; set => SetProperty(ref _depth, value); }
    public string CurrentSpeed { get => _currentSpeed; set => SetProperty(ref _currentSpeed, value); }
    public string WaveHeight { get => _waveHeight; set => SetProperty(ref _waveHeight, value); }
    public string WavePeriod { get => _wavePeriod; set => SetProperty(ref _wavePeriod, value); }
    public string BuoyName { get => _buoyName; set => SetProperty(ref _buoyName, value); }

    public BuoyLibraryItem? SelectedBuoyPreset
    {
        get => _selectedBuoyPreset;
        set
        {
            if (SetProperty(ref _selectedBuoyPreset, value))
            {
                ApplySelectedBuoyPreset();
            }
        }
    }

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
    public string BuoyLibraryStatusText { get => _buoyLibraryStatusText; set => SetProperty(ref _buoyLibraryStatusText, value); }

    private void RefreshBuoyLibrary(string? selectedId)
    {
        selectedId ??= SelectedBuoyPreset?.Id;

        BuoyPresets.Clear();
        foreach (var buoy in BuoyLibraryStorage.LoadAllBuoys())
        {
            BuoyPresets.Add(buoy);
        }

        SelectedBuoyPreset = BuoyPresets.FirstOrDefault(x => x.Id == selectedId) ?? BuoyPresets.FirstOrDefault();
        BuoyLibraryStatusText = $"Буёв в библиотеке: {BuoyPresets.Count}. Пользовательский файл: {BuoyLibraryStorage.LibraryPath}";
        RefreshSequenceLibraryOptions();
    }

    private void RefreshSequenceLibraryOptions()
    {
        foreach (var item in AssemblyItems)
        {
            item.RefreshLibraryOptions();
        }

        UpdateSequenceSummary();
    }

    private void ApplySelectedBuoyPreset()
    {
        if (SelectedBuoyPreset is null)
        {
            return;
        }

        BuoyName = SelectedBuoyPreset.Name;
        BuoyVolume = FormatDouble(SelectedBuoyPreset.VolumeM3);
        BuoyWeight = FormatDouble(SelectedBuoyPreset.WeightKg);
        BuoyArea = FormatDouble(SelectedBuoyPreset.ProjectedAreaM2);
        BuoyCd = FormatDouble(SelectedBuoyPreset.DragCoefficient);
        BuoyLibraryStatusText = $"Выбран буй: {SelectedBuoyPreset.DisplayName}";
    }

    private void SaveCurrentBuoyToLibrary()
    {
        var name = string.IsNullOrWhiteSpace(BuoyName) ? "Пользовательский буй" : BuoyName.Trim();
        var selectedUserId = SelectedBuoyPreset is { Source: "User" } ? SelectedBuoyPreset.Id : string.Empty;

        var buoy = new BuoyLibraryItem
        {
            Id = selectedUserId,
            Source = "User",
            Name = name,
            VolumeM3 = Parse(BuoyVolume),
            WeightKg = Parse(BuoyWeight),
            ProjectedAreaM2 = Parse(BuoyArea),
            DragCoefficient = Parse(BuoyCd),
            Note = "Сохранено пользователем из формы буя."
        };

        BuoyLibraryStorage.UpsertUserBuoy(buoy);
        RefreshBuoyLibrary(buoy.Id);
        BuoyLibraryStatusText = $"Буй сохранён в библиотеку: {name}";
    }

    private void DeleteSelectedBuoyPreset()
    {
        if (SelectedBuoyPreset is null)
        {
            BuoyLibraryStatusText = "Выберите пользовательский буй для удаления.";
            return;
        }

        if (SelectedBuoyPreset.Source != "User" || SelectedBuoyPreset.Id.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase))
        {
            BuoyLibraryStatusText = "Встроенный буй удалить нельзя. Удалять можно только пользовательские буи.";
            return;
        }

        var deletedName = SelectedBuoyPreset.Name;
        var deleted = BuoyLibraryStorage.DeleteUserBuoy(SelectedBuoyPreset.Id);

        RefreshBuoyLibrary(null);
        BuoyLibraryStatusText = deleted
            ? $"Удалён пользовательский буй: {deletedName}"
            : "Пользовательский буй не найден в файле библиотеки.";
    }

    private void AddAssemblyItem(AssemblyItemViewModel item)
    {
        if (item.IsConnector)
        {
            item.Count = "1";
        }

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
        if (sender is AssemblyItemViewModel { IsConnector: true } connector)
        {
            connector.Count = "1";
        }

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
        ProjectFilePath = ProjectJsonStorage.DefaultProjectPath;
        WaterDensity = "1025";
        Depth = "50";
        CurrentSpeed = "0.5";
        WaveHeight = "1.0";
        WavePeriod = "6.0";
        BuoyName = "Буй";
        SelectedBuoyPreset = BuoyPresets.FirstOrDefault();
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
        AddAssemblyItem(new AssemblyItemViewModel { Kind = "Line", Title = "Верхний буйреп", RopePresetId = "built-in:polyester_20", LengthM = "45" });
        AddAssemblyItem(new AssemblyItemViewModel { Kind = "Connector", Title = "Вертлюг", ConnectorPresetId = "swivel_60", Count = "1" });
        AddAssemblyItem(new AssemblyItemViewModel { Kind = "Payload", Title = "ADCP", PayloadWeightAirKg = "40", PayloadProjectedAreaM2 = "0.05", PayloadDragCoefficient = "1.0" });
        AddAssemblyItem(new AssemblyItemViewModel { Kind = "Line", Title = "Нижняя цепь", RopePresetId = "built-in:chain_10", LengthM = "10" });

        UpdateSequenceSummary();
    }

    private async Task SaveProjectAsync(bool promptForPath)
    {
        try
        {
            var targetPath = ProjectFilePath;

            if (promptForPath || string.IsNullOrWhiteSpace(targetPath))
            {
                var suggestedFileName = MakeSafeFileName(ProjectName) + ".json";
                targetPath = _fileDialogService is not null
                    ? await _fileDialogService.PickSavePathAsync(suggestedFileName) ?? string.Empty
                    : ProjectJsonStorage.DefaultProjectPath;
            }

            if (string.IsNullOrWhiteSpace(targetPath))
            {
                ProjectStatusText = "Сохранение отменено.";
                return;
            }

            targetPath = ProjectJsonStorage.NormalizeJsonPath(targetPath);
            ProjectJsonStorage.Save(ToDto(), targetPath);
            ProjectFilePath = targetPath;
            ProjectStatusText = $"Проект сохранён: {targetPath}";
        }
        catch (Exception ex)
        {
            ProjectStatusText = $"Ошибка сохранения: {ex.Message}";
        }
    }

    private async Task LoadProjectAsync()
    {
        try
        {
            var selectedPath = _fileDialogService is not null
                ? await _fileDialogService.PickOpenPathAsync() ?? string.Empty
                : ProjectFilePath;

            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                ProjectStatusText = "Загрузка отменена.";
                return;
            }

            var dto = ProjectJsonStorage.Load(selectedPath);
            if (dto is null)
            {
                ProjectStatusText = $"Файл проекта не найден: {selectedPath}";
                return;
            }

            FromDto(dto);
            ProjectFilePath = selectedPath;
            ProjectStatusText = $"Проект загружен: {selectedPath}";
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
            BuoyName = BuoyName,
            SelectedBuoyPresetId = SelectedBuoyPreset?.Id ?? string.Empty,
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
                Count = x.IsConnector ? "1" : x.Count,
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
        BuoyName = string.IsNullOrWhiteSpace(dto.BuoyName) ? "Буй" : dto.BuoyName;
        RefreshBuoyLibrary(dto.SelectedBuoyPresetId);
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
                RopePresetId = NormalizeRopeId(item.RopePresetId),
                ConnectorPresetId = item.ConnectorPresetId,
                LengthM = item.LengthM,
                Count = item.Kind == "Connector" ? "1" : item.Count,
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
            .Count(x => x.Kind == AssemblyItemKind.Connector);

        var payloadWeightKg = enabledItems
            .Where(x => x.Kind == AssemblyItemKind.Payload)
            .Sum(x => x.PayloadWeightAirKg);

        SequenceSummary = $"Активных элементов: {enabledItems.Count} · линия: {lineLengthM:0.##} м · соединителей: {connectorCount} · приборы: {payloadWeightKg:0.##} кг";
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

        var buoy = new BuoyInput(BuoyName, Parse(BuoyVolume), Parse(BuoyWeight), Parse(BuoyArea), Parse(BuoyCd));
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
        return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0;
    }

    private static string FormatDouble(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string NormalizeRopeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "built-in:polyester_20";
        }

        if (value.StartsWith("user:", StringComparison.OrdinalIgnoreCase) || value.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        return "built-in:" + value;
    }

    private static string MakeSafeFileName(string value)
    {
        value = string.IsNullOrWhiteSpace(value) ? "BuoyCalc_Project" : value.Trim();

        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalidChar, '_');
        }

        return value.Replace(' ', '_');
    }
}
