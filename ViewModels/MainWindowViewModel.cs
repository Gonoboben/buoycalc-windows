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
    private bool _useCurrentProfile;
    private string _currentProfileSummary = "Профиль течения отключён. Используется одно значение скорости.";
    private string _waveHeight = "1.0";
    private string _wavePeriod = "6.0";
    private SeabedPreset? _selectedSeabedPreset;
    private string _buoyName = "Буй";
    private BuoyLibraryItem? _selectedBuoyPreset;
    private string _buoyVolume = "0.50";
    private string _buoyWeight = "80";
    private string _buoyArea = "0.5";
    private string _buoyCd = "0.8";
    private AnchorLibraryItem? _selectedAnchorPreset;
    private string _anchorName = "Concrete 500 kg";
    private string _anchorType = "Deadweight";
    private string _anchorMaterial = "Concrete / Бетон";
    private string _anchorWeight = "500";
    private string _anchorVolume = "0.20";
    private string _anchorCoefficient = "1.0";
    private string _safetyFactor = "5";
    private string _resultText = "Нажмите «Рассчитать».";
    private string _reportText = "";
    private string _sequenceSummary = "";
    private string _projectStatusText = "Проект ещё не сохранён.";
    private string _buoyLibraryStatusText = "Библиотека готова.";
    private string _visualizationDepthText = "Глубина: 50 м";
    private string _visualizationLineLengthText = "Длина линии: 55 м";
    private string _visualizationOffsetText = "Оценочный снос: после расчёта";
    private string _visualizationSlackRatioText = "L/Depth: 1.1";
    private string _visualizationStatusText = "OK: длина линии не меньше глубины";
    private double _visualizationDepthM = 50;
    private double _visualizationLineLengthM = 55;
    private double _visualizationOffsetM;

    public MainWindowViewModel(IProjectFileDialogService? fileDialogService = null)
    {
        _fileDialogService = fileDialogService;
        AssemblyItems = new ObservableCollection<AssemblyItemViewModel>();
        ElementRows = new ObservableCollection<ElementCalculationDisplayRow>();
        SequenceDiagramLines = new ObservableCollection<string>();
        CurrentProfilePoints = new ObservableCollection<CurrentProfilePointViewModel>();
        BuoyPresets = new ObservableCollection<BuoyLibraryItem>();
        AnchorPresets = new ObservableCollection<AnchorLibraryItem>();
        SeabedPresets = new ObservableCollection<SeabedPreset>(SeabedCatalog.Presets);

        CalculateCommand = new RelayCommand(Calculate);
        AddLineCommand = new RelayCommand(() => AddAssemblyItem(new AssemblyItemViewModel { Kind = "Line", Title = "Новый участок линии", RopePresetStorageId = "built-in:polyester_20" }));
        AddConnectorCommand = new RelayCommand(() => AddAssemblyItem(new AssemblyItemViewModel { Kind = "Connector", Title = "Новый соединитель", ConnectorPresetStorageId = "built-in:shackle_55" }));
        AddPayloadCommand = new RelayCommand(() => AddAssemblyItem(new AssemblyItemViewModel { Kind = "Payload", Title = "Новый прибор", PayloadPresetStorageId = "built-in:adcp_40" }));
        AddCurrentProfilePointCommand = new RelayCommand(AddCurrentProfilePoint);
        ResetCurrentProfileCommand = new RelayCommand(ResetCurrentProfile);
        NewProjectCommand = new RelayCommand(NewProject);
        SaveProjectCommand = new RelayCommand(async () => await SaveProjectAsync(promptForPath: false));
        SaveProjectAsCommand = new RelayCommand(async () => await SaveProjectAsync(promptForPath: true));
        LoadProjectCommand = new RelayCommand(async () => await LoadProjectAsync());
        SaveBuoyPresetCommand = new RelayCommand(SaveCurrentBuoyToLibrary);
        DeleteBuoyPresetCommand = new RelayCommand(DeleteSelectedBuoyPreset);
        RefreshBuoyLibraryCommand = new RelayCommand(RefreshLibraries);

        RefreshLibraries();
        ResetToDefaultProject();
    }

    public ObservableCollection<AssemblyItemViewModel> AssemblyItems { get; }
    public ObservableCollection<ElementCalculationDisplayRow> ElementRows { get; }
    public ObservableCollection<string> SequenceDiagramLines { get; }
    public ObservableCollection<CurrentProfilePointViewModel> CurrentProfilePoints { get; }
    public ObservableCollection<BuoyLibraryItem> BuoyPresets { get; }
    public ObservableCollection<AnchorLibraryItem> AnchorPresets { get; }
    public ObservableCollection<SeabedPreset> SeabedPresets { get; }

    public ICommand CalculateCommand { get; }
    public ICommand AddLineCommand { get; }
    public ICommand AddConnectorCommand { get; }
    public ICommand AddPayloadCommand { get; }
    public ICommand AddCurrentProfilePointCommand { get; }
    public ICommand ResetCurrentProfileCommand { get; }
    public ICommand NewProjectCommand { get; }
    public ICommand SaveProjectCommand { get; }
    public ICommand SaveProjectAsCommand { get; }
    public ICommand LoadProjectCommand { get; }
    public ICommand SaveBuoyPresetCommand { get; }
    public ICommand DeleteBuoyPresetCommand { get; }
    public ICommand RefreshBuoyLibraryCommand { get; }

    public string ProjectName { get => _projectName; set => SetProperty(ref _projectName, value); }
    public string ProjectFilePath { get => _projectFilePath; set => SetProperty(ref _projectFilePath, value); }
    public string WaterDensity { get => _waterDensity; set { if (SetProperty(ref _waterDensity, value)) UpdateVisualizationSummary(); } }
    public string Depth { get => _depth; set { if (SetProperty(ref _depth, value)) UpdateVisualizationSummary(); } }
    public string CurrentSpeed { get => _currentSpeed; set { if (SetProperty(ref _currentSpeed, value)) UpdateCurrentProfileSummary(); } }

    public bool UseCurrentProfile
    {
        get => _useCurrentProfile;
        set
        {
            if (SetProperty(ref _useCurrentProfile, value))
            {
                UpdateCurrentProfileSummary();
            }
        }
    }

    public string CurrentProfileSummary { get => _currentProfileSummary; set => SetProperty(ref _currentProfileSummary, value); }
    public string WaveHeight { get => _waveHeight; set => SetProperty(ref _waveHeight, value); }
    public string WavePeriod { get => _wavePeriod; set => SetProperty(ref _wavePeriod, value); }

    public SeabedPreset? SelectedSeabedPreset
    {
        get => _selectedSeabedPreset;
        set => SetProperty(ref _selectedSeabedPreset, value);
    }

    public string BuoyName { get => _buoyName; set { if (SetProperty(ref _buoyName, value)) UpdateSequenceDiagram(); } }

    public BuoyLibraryItem? SelectedBuoyPreset
    {
        get => _selectedBuoyPreset;
        set
        {
            if (SetProperty(ref _selectedBuoyPreset, value))
            {
                ApplySelectedBuoyPreset();
                UpdateSequenceDiagram();
            }
        }
    }

    public AnchorLibraryItem? SelectedAnchorPreset
    {
        get => _selectedAnchorPreset;
        set
        {
            if (SetProperty(ref _selectedAnchorPreset, value))
            {
                ApplySelectedAnchorPreset();
                UpdateSequenceDiagram();
            }
        }
    }

    public string BuoyVolume { get => _buoyVolume; set => SetProperty(ref _buoyVolume, value); }
    public string BuoyWeight { get => _buoyWeight; set => SetProperty(ref _buoyWeight, value); }
    public string BuoyArea { get => _buoyArea; set => SetProperty(ref _buoyArea, value); }
    public string BuoyCd { get => _buoyCd; set => SetProperty(ref _buoyCd, value); }
    public string AnchorName { get => _anchorName; set { if (SetProperty(ref _anchorName, value)) UpdateSequenceDiagram(); } }
    public string AnchorType { get => _anchorType; set { if (SetProperty(ref _anchorType, value)) UpdateSequenceDiagram(); } }
    public string AnchorMaterial { get => _anchorMaterial; set => SetProperty(ref _anchorMaterial, value); }
    public string AnchorWeight { get => _anchorWeight; set => SetProperty(ref _anchorWeight, value); }
    public string AnchorVolume { get => _anchorVolume; set => SetProperty(ref _anchorVolume, value); }
    public string AnchorCoefficient { get => _anchorCoefficient; set => SetProperty(ref _anchorCoefficient, value); }
    public string SafetyFactor { get => _safetyFactor; set => SetProperty(ref _safetyFactor, value); }
    public string ResultText { get => _resultText; set => SetProperty(ref _resultText, value); }
    public string ReportText { get => _reportText; set => SetProperty(ref _reportText, value); }
    public string SequenceSummary { get => _sequenceSummary; set => SetProperty(ref _sequenceSummary, value); }
    public string ProjectStatusText { get => _projectStatusText; set => SetProperty(ref _projectStatusText, value); }
    public string BuoyLibraryStatusText { get => _buoyLibraryStatusText; set => SetProperty(ref _buoyLibraryStatusText, value); }
    public string VisualizationDepthText { get => _visualizationDepthText; set => SetProperty(ref _visualizationDepthText, value); }
    public string VisualizationLineLengthText { get => _visualizationLineLengthText; set => SetProperty(ref _visualizationLineLengthText, value); }
    public string VisualizationOffsetText { get => _visualizationOffsetText; set => SetProperty(ref _visualizationOffsetText, value); }
    public string VisualizationSlackRatioText { get => _visualizationSlackRatioText; set => SetProperty(ref _visualizationSlackRatioText, value); }
    public string VisualizationStatusText { get => _visualizationStatusText; set => SetProperty(ref _visualizationStatusText, UserStatusPolicy.ToUserStatus(value)); }
    public double VisualizationDepthM { get => _visualizationDepthM; set => SetProperty(ref _visualizationDepthM, value); }
    public double VisualizationLineLengthM { get => _visualizationLineLengthM; set => SetProperty(ref _visualizationLineLengthM, value); }
    public double VisualizationOffsetM { get => _visualizationOffsetM; set => SetProperty(ref _visualizationOffsetM, value); }

    private void RefreshLibraries()
    {
        RefreshBuoyLibrary(SelectedBuoyPreset?.Id);
        RefreshAnchorLibrary(SelectedAnchorPreset?.Id);
        RefreshSequenceLibraryOptions();
    }

    private void RefreshBuoyLibrary(string? selectedId)
    {
        BuoyPresets.Clear();
        foreach (var buoy in BuoyLibraryStorage.LoadAllBuoys()) BuoyPresets.Add(buoy);
        SelectedBuoyPreset = BuoyPresets.FirstOrDefault(x => x.Id == selectedId) ?? BuoyPresets.FirstOrDefault();
        BuoyLibraryStatusText = $"Библиотека: буёв {BuoyPresets.Count}, якорей {AnchorPresets.Count}.";
    }

    private void RefreshAnchorLibrary(string? selectedId)
    {
        AnchorPresets.Clear();
        foreach (var anchor in AnchorLibraryStorage.LoadAllAnchors()) AnchorPresets.Add(anchor);
        SelectedAnchorPreset = AnchorPresets.FirstOrDefault(x => x.Id == selectedId) ?? AnchorPresets.FirstOrDefault();
        BuoyLibraryStatusText = $"Библиотека: буёв {BuoyPresets.Count}, якорей {AnchorPresets.Count}.";
    }

    private void RefreshSequenceLibraryOptions()
    {
        foreach (var item in AssemblyItems) item.RefreshLibraryOptions();
        UpdateSequenceSummary();
    }

    private void ApplySelectedBuoyPreset()
    {
        if (SelectedBuoyPreset is null) return;
        BuoyName = SelectedBuoyPreset.Name;
        BuoyVolume = FormatDouble(SelectedBuoyPreset.VolumeM3);
        BuoyWeight = FormatDouble(SelectedBuoyPreset.WeightKg);
        BuoyArea = FormatDouble(SelectedBuoyPreset.ProjectedAreaM2);
        BuoyCd = FormatDouble(SelectedBuoyPreset.DragCoefficient);
    }

    private void ApplySelectedAnchorPreset()
    {
        if (SelectedAnchorPreset is null) return;
        AnchorName = SelectedAnchorPreset.Name;
        AnchorType = SelectedAnchorPreset.Type;
        AnchorMaterial = SelectedAnchorPreset.Material;
        AnchorWeight = FormatDouble(SelectedAnchorPreset.WeightAirKg);
        AnchorVolume = FormatDouble(SelectedAnchorPreset.VolumeM3);
        AnchorCoefficient = FormatDouble(SelectedAnchorPreset.BaseHoldingCoefficient);
    }

    private void SaveCurrentBuoyToLibrary()
    {
        var name = string.IsNullOrWhiteSpace(BuoyName) ? "Пользовательский буй" : BuoyName.Trim();
        var selectedUserId = SelectedBuoyPreset is { Source: "User" } ? SelectedBuoyPreset.Id : string.Empty;
        var buoy = new BuoyLibraryItem { Id = selectedUserId, Source = "User", Name = name, VolumeM3 = Parse(BuoyVolume), WeightKg = Parse(BuoyWeight), ProjectedAreaM2 = Parse(BuoyArea), DragCoefficient = Parse(BuoyCd), Note = "Сохранено пользователем из формы буя." };
        BuoyLibraryStorage.UpsertUserBuoy(buoy);
        RefreshLibraries();
        BuoyLibraryStatusText = $"Буй сохранён в библиотеку: {name}";
    }

    private void DeleteSelectedBuoyPreset()
    {
        if (SelectedBuoyPreset is null) { BuoyLibraryStatusText = "Выберите пользовательский буй для удаления."; return; }
        if (SelectedBuoyPreset.Source != "User" || SelectedBuoyPreset.Id.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase)) { BuoyLibraryStatusText = "Встроенный буй удалить нельзя. Удалять можно только пользовательские буи."; return; }
        var deletedName = SelectedBuoyPreset.Name;
        var deleted = BuoyLibraryStorage.DeleteUserBuoy(SelectedBuoyPreset.Id);
        RefreshLibraries();
        BuoyLibraryStatusText = deleted ? $"Удалён пользовательский буй: {deletedName}" : "Пользовательский буй не найден в файле библиотеки.";
    }

    private void AddAssemblyItem(AssemblyItemViewModel item)
    {
        if (item.IsConnector) item.Count = "1";
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
        if (sender is AssemblyItemViewModel { IsConnector: true } connector) connector.Count = "1";
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
        if (index <= 0) return;
        AssemblyItems.Move(index, index - 1);
        UpdateSequenceSummary();
    }

    private void MoveItemDown(AssemblyItemViewModel item)
    {
        var index = AssemblyItems.IndexOf(item);
        if (index < 0 || index >= AssemblyItems.Count - 1) return;
        AssemblyItems.Move(index, index + 1);
        UpdateSequenceSummary();
    }

    private void DuplicateItem(AssemblyItemViewModel item)
    {
        var index = AssemblyItems.IndexOf(item);
        var copy = item.Clone();
        WireItem(copy);
        if (index < 0 || index >= AssemblyItems.Count - 1) AssemblyItems.Add(copy); else AssemblyItems.Insert(index + 1, copy);
        UpdateSequenceSummary();
    }

    private void AddCurrentProfilePoint()
    {
        var depth = CurrentProfilePoints.Count == 0 ? 0 : CurrentProfilePoints.Select(x => x.ToInput().DepthM).Max() + 10;
        var point = new CurrentProfilePointViewModel
        {
            DepthM = FormatDouble(depth),
            EastCurrentMS = CurrentProfilePoints.Count == 0 ? CurrentSpeed : "0.3",
            NorthCurrentMS = "0",
            VerticalCurrentMS = "0",
            WaterDensityKgM3 = WaterDensity
        };
        AddCurrentProfilePoint(point);
    }

    private void AddCurrentProfilePoint(CurrentProfilePointViewModel point)
    {
        point.RemoveRequested += RemoveCurrentProfilePoint;
        point.PropertyChanged += OnCurrentProfilePointChanged;
        CurrentProfilePoints.Add(point);
        UpdateCurrentProfileSummary();
    }

    private void RemoveCurrentProfilePoint(CurrentProfilePointViewModel point)
    {
        point.RemoveRequested -= RemoveCurrentProfilePoint;
        point.PropertyChanged -= OnCurrentProfilePointChanged;
        CurrentProfilePoints.Remove(point);
        UpdateCurrentProfileSummary();
    }

    private void ClearCurrentProfilePoints()
    {
        foreach (var point in CurrentProfilePoints)
        {
            point.RemoveRequested -= RemoveCurrentProfilePoint;
            point.PropertyChanged -= OnCurrentProfilePointChanged;
        }
        CurrentProfilePoints.Clear();
    }

    private void ResetCurrentProfile()
    {
        ClearCurrentProfilePoints();
        AddCurrentProfilePoint(new CurrentProfilePointViewModel { DepthM = "0", EastCurrentMS = CurrentSpeed, NorthCurrentMS = "0", VerticalCurrentMS = "0", WaterDensityKgM3 = WaterDensity });
        AddCurrentProfilePoint(new CurrentProfilePointViewModel { DepthM = "10", EastCurrentMS = "0.45", NorthCurrentMS = "0", VerticalCurrentMS = "0", WaterDensityKgM3 = WaterDensity });
        AddCurrentProfilePoint(new CurrentProfilePointViewModel { DepthM = "25", EastCurrentMS = "0.3", NorthCurrentMS = "0", VerticalCurrentMS = "0", WaterDensityKgM3 = WaterDensity });
        AddCurrentProfilePoint(new CurrentProfilePointViewModel { DepthM = Depth, EastCurrentMS = "0.1", NorthCurrentMS = "0", VerticalCurrentMS = "0", WaterDensityKgM3 = WaterDensity });
        UpdateCurrentProfileSummary();
    }

    private void OnCurrentProfilePointChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is CurrentProfilePointViewModel point)
        {
            point.RefreshSummary();
        }
        UpdateCurrentProfileSummary();
    }

    private void UpdateCurrentProfileSummary()
    {
        if (!UseCurrentProfile)
        {
            CurrentProfileSummary = $"Профиль течения отключён. Используется одно значение скорости: {CurrentSpeed} м/с.";
            return;
        }

        if (CurrentProfilePoints.Count == 0)
        {
            CurrentProfileSummary = "Профиль включён, но точки не заданы. Будет использовано одно значение скорости.";
            return;
        }

        var inputs = CurrentProfilePoints.Select(x => x.ToInput()).OrderBy(x => x.DepthM).ToList();
        var maxSpeed = inputs.Max(x => x.HorizontalSpeedMS);
        var minDepth = inputs.Min(x => x.DepthM);
        var maxDepth = inputs.Max(x => x.DepthM);
        CurrentProfileSummary = $"Профиль включён: {inputs.Count} точек, глубины {minDepth:0.##}–{maxDepth:0.##} м, max |Uгор|={maxSpeed:0.###} м/с. В v0.19 расчёт использует эту max-скорость как переходную оценку.";
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
        UseCurrentProfile = false;
        WaveHeight = "1.0";
        WavePeriod = "6.0";
        SelectedSeabedPreset = SeabedCatalog.ById("unknown");
        SelectedBuoyPreset = BuoyPresets.FirstOrDefault();
        SelectedAnchorPreset = AnchorPresets.FirstOrDefault(x => x.Id == "built-in:concrete_500") ?? AnchorPresets.FirstOrDefault();
        SafetyFactor = "5";
        ResultText = "Нажмите «Рассчитать».";
        ReportText = "";
        ElementRows.Clear();
        SequenceDiagramLines.Clear();

        ClearCurrentProfilePoints();
        ResetCurrentProfile();
        UseCurrentProfile = false;

        ClearAssemblyItems();
        AddAssemblyItem(new AssemblyItemViewModel { Kind = "Connector", Title = "Скоба под буем", ConnectorPresetStorageId = "built-in:shackle_55", Count = "1" });
        AddAssemblyItem(new AssemblyItemViewModel { Kind = "Line", Title = "Верхний буйреп", RopePresetStorageId = "built-in:polyester_20", LengthM = "45" });
        AddAssemblyItem(new AssemblyItemViewModel { Kind = "Connector", Title = "Вертлюг", ConnectorPresetStorageId = "built-in:swivel_60", Count = "1" });
        AddAssemblyItem(new AssemblyItemViewModel { Kind = "Payload", Title = "ADCP", PayloadPresetStorageId = "built-in:adcp_40" });
        AddAssemblyItem(new AssemblyItemViewModel { Kind = "Line", Title = "Нижняя цепь", RopePresetStorageId = "built-in:chain_10", LengthM = "10" });

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
                targetPath = _fileDialogService is not null ? await _fileDialogService.PickSavePathAsync(suggestedFileName) ?? string.Empty : ProjectJsonStorage.DefaultProjectPath;
            }
            if (string.IsNullOrWhiteSpace(targetPath)) { ProjectStatusText = "Сохранение отменено."; return; }
            targetPath = ProjectJsonStorage.NormalizeJsonPath(targetPath);
            ProjectJsonStorage.Save(ToDto(), targetPath);
            ProjectFilePath = targetPath;
            ProjectStatusText = $"Проект сохранён: {targetPath}";
        }
        catch (Exception ex) { ProjectStatusText = $"Ошибка сохранения: {ex.Message}"; }
    }

    private async Task LoadProjectAsync()
    {
        try
        {
            var selectedPath = _fileDialogService is not null ? await _fileDialogService.PickOpenPathAsync() ?? string.Empty : ProjectFilePath;
            if (string.IsNullOrWhiteSpace(selectedPath)) { ProjectStatusText = "Загрузка отменена."; return; }
            var dto = ProjectJsonStorage.Load(selectedPath);
            if (dto is null) { ProjectStatusText = $"Файл проекта не найден: {selectedPath}"; return; }
            FromDto(dto);
            ProjectFilePath = selectedPath;
            ProjectStatusText = $"Проект загружен: {selectedPath}";
        }
        catch (Exception ex) { ProjectStatusText = $"Ошибка загрузки: {ex.Message}"; }
    }

    private BuoyProjectDto ToDto()
    {
        return new BuoyProjectDto
        {
            ProjectName = ProjectName,
            WaterDensity = WaterDensity,
            Depth = Depth,
            CurrentSpeed = CurrentSpeed,
            UseCurrentProfile = UseCurrentProfile ? "true" : "false",
            WaveHeight = WaveHeight,
            WavePeriod = WavePeriod,
            SelectedSeabedPresetId = SelectedSeabedPreset?.Id ?? "unknown",
            BuoyName = BuoyName,
            SelectedBuoyPresetId = SelectedBuoyPreset?.Id ?? string.Empty,
            BuoyVolume = BuoyVolume,
            BuoyWeight = BuoyWeight,
            BuoyArea = BuoyArea,
            BuoyCd = BuoyCd,
            SelectedAnchorPresetId = SelectedAnchorPreset?.Id ?? string.Empty,
            AnchorName = AnchorName,
            AnchorType = AnchorType,
            AnchorMaterial = AnchorMaterial,
            AnchorWeight = AnchorWeight,
            AnchorVolume = AnchorVolume,
            AnchorCoefficient = AnchorCoefficient,
            SafetyFactor = SafetyFactor,
            CurrentProfilePoints = CurrentProfilePoints.Select(x => x.ToDto()).ToList(),
            AssemblyItems = AssemblyItems.Select(x => new AssemblyItemDto
            {
                IsEnabled = x.IsEnabled,
                Kind = x.Kind,
                Title = x.Title,
                RopePresetId = x.RopePresetStorageId,
                ConnectorPresetId = x.ConnectorPresetStorageId,
                PayloadPresetId = x.PayloadPresetStorageId,
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
        UseCurrentProfile = string.Equals(dto.UseCurrentProfile, "true", StringComparison.OrdinalIgnoreCase);
        WaveHeight = dto.WaveHeight;
        WavePeriod = dto.WavePeriod;
        SelectedSeabedPreset = SeabedPresets.FirstOrDefault(x => x.Id == dto.SelectedSeabedPresetId) ?? SeabedCatalog.ById("unknown");
        BuoyName = string.IsNullOrWhiteSpace(dto.BuoyName) ? "Буй" : dto.BuoyName;
        RefreshLibraries();
        SelectedBuoyPreset = BuoyPresets.FirstOrDefault(x => x.Id == dto.SelectedBuoyPresetId) ?? SelectedBuoyPreset;
        SelectedAnchorPreset = AnchorPresets.FirstOrDefault(x => x.Id == dto.SelectedAnchorPresetId) ?? SelectedAnchorPreset;
        if (!string.IsNullOrWhiteSpace(dto.AnchorName)) AnchorName = dto.AnchorName;
        if (!string.IsNullOrWhiteSpace(dto.AnchorType)) AnchorType = dto.AnchorType;
        if (!string.IsNullOrWhiteSpace(dto.AnchorMaterial)) AnchorMaterial = dto.AnchorMaterial;
        if (!string.IsNullOrWhiteSpace(dto.AnchorWeight)) AnchorWeight = dto.AnchorWeight;
        if (!string.IsNullOrWhiteSpace(dto.AnchorVolume)) AnchorVolume = dto.AnchorVolume;
        if (!string.IsNullOrWhiteSpace(dto.AnchorCoefficient)) AnchorCoefficient = dto.AnchorCoefficient;
        SafetyFactor = dto.SafetyFactor;
        ResultText = "Проект загружен. Нажмите «Рассчитать».";
        ReportText = "";
        ElementRows.Clear();
        SequenceDiagramLines.Clear();

        ClearCurrentProfilePoints();
        foreach (var point in dto.CurrentProfilePoints)
        {
            AddCurrentProfilePoint(CurrentProfilePointViewModel.FromDto(point));
        }
        if (CurrentProfilePoints.Count == 0)
        {
            ResetCurrentProfile();
        }

        ClearAssemblyItems();
        foreach (var item in dto.AssemblyItems)
        {
            AddAssemblyItem(new AssemblyItemViewModel
            {
                IsEnabled = item.IsEnabled,
                Kind = item.Kind,
                Title = item.Title,
                RopePresetStorageId = NormalizeRopeId(item.RopePresetId),
                ConnectorPresetStorageId = NormalizeConnectorId(item.ConnectorPresetId),
                PayloadPresetStorageId = NormalizePayloadId(item.PayloadPresetId),
                LengthM = item.LengthM,
                Count = item.Kind == "Connector" ? "1" : item.Count,
                PayloadWeightAirKg = item.PayloadWeightAirKg,
                PayloadVolumeM3 = item.PayloadVolumeM3,
                PayloadProjectedAreaM2 = item.PayloadProjectedAreaM2,
                PayloadDragCoefficient = item.PayloadDragCoefficient
            });
        }
        UpdateSequenceSummary();
        UpdateCurrentProfileSummary();
    }

    private void UpdateSequenceSummary(CalculationResult? result = null)
    {
        var enabledItems = AssemblyItems.Where(x => x.IsEnabled).Select(x => x.ToInput()).ToList();
        var lineLengthM = enabledItems.Where(x => x.Kind == AssemblyItemKind.Line).Sum(x => x.LengthM);
        var connectorCount = enabledItems.Count(x => x.Kind == AssemblyItemKind.Connector);
        var payloadWeightKg = enabledItems.Where(x => x.Kind == AssemblyItemKind.Payload).Sum(x => x.PayloadWeightAirKg);
        SequenceSummary = $"Активных элементов: {enabledItems.Count} · линия: {lineLengthM:0.##} м · соединителей: {connectorCount} · приборы: {payloadWeightKg:0.##} кг";
        UpdateSequenceDiagram();
        UpdateVisualizationSummary(result);
    }

    private void UpdateSequenceDiagram()
    {
        SequenceDiagramLines.Clear();
        SequenceDiagramLines.Add($"● Буй: {SafeText(BuoyName, "Буй")}");

        foreach (var item in AssemblyItems.Where(x => x.IsEnabled))
        {
            SequenceDiagramLines.Add("↓");
            SequenceDiagramLines.Add($"○ {item.KindDisplayName}: {SafeText(item.Title, "Элемент")} · {item.Summary}");
        }

        SequenceDiagramLines.Add("↓");
        SequenceDiagramLines.Add($"■ Якорь: {SafeText(AnchorName, "Якорь")} · {SafeText(AnchorType, "тип не задан")}");
    }

    private void UpdateVisualizationSummary(CalculationResult? result = null)
    {
        var depthM = Parse(Depth);
        var lineLengthM = AssemblyItems
            .Where(x => x.IsEnabled)
            .Select(x => x.ToInput())
            .Where(x => x.Kind == AssemblyItemKind.Line)
            .Sum(x => x.LengthM);

        var slackRatio = depthM > 0 ? lineLengthM / depthM : 0;
        var offsetM = result?.EstimatedOffsetM ?? 0;
        var offsetText = result is null ? "после расчёта" : $"{offsetM:0.##} м";

        VisualizationDepthM = depthM;
        VisualizationLineLengthM = lineLengthM;
        VisualizationOffsetM = offsetM;

        VisualizationDepthText = $"Глубина: {depthM:0.##} м";
        VisualizationLineLengthText = $"Длина линии: {lineLengthM:0.##} м";
        VisualizationOffsetText = $"Оценочный снос: {offsetText}";
        VisualizationSlackRatioText = depthM > 0 ? $"L/Depth: {slackRatio:0.###}" : "L/Depth: не определено";

        VisualizationStatusText = depthM <= 0
            ? "WARNING: глубина не задана"
            : lineLengthM >= depthM
                ? "OK: длина линии не меньше глубины"
                : "WARNING: линия короче глубины";
    }

    private void Calculate()
    {
        var currentProfile = CurrentProfilePoints.Select(x => x.ToInput()).OrderBy(x => x.DepthM).ToList();
        var environment = new EnvironmentInput(
            Parse(WaterDensity),
            Parse(Depth),
            Parse(CurrentSpeed),
            Parse(WaveHeight),
            Parse(WavePeriod),
            SelectedSeabedPreset ?? SeabedCatalog.ById("unknown"),
            UseCurrentProfile,
            currentProfile);

        var buoy = new BuoyInput(BuoyName, Parse(BuoyVolume), Parse(BuoyWeight), Parse(BuoyArea), Parse(BuoyCd));
        var anchor = new AnchorInput(AnchorName, AnchorType, AnchorMaterial, Parse(AnchorWeight), Parse(AnchorVolume), Parse(AnchorCoefficient));
        var items = AssemblyItems.Select(x => x.ToInput()).ToList();
        var result = BuoyCalculator.Calculate(environment, buoy, items, anchor, Parse(SafetyFactor));
        var sequenceItems = AssemblyItems
            .Select(x => new MainWindowSequenceDisplayItem(x.IsEnabled, x.KindDisplayName, x.Title, x.Summary))
            .ToList();
        var display = MainWindowCalculationDisplayBuilder.Build(
            ProjectName,
            environment,
            buoy,
            anchor,
            items,
            sequenceItems,
            BuoyName,
            AnchorName,
            AnchorType,
            result);

        PublishCalculationDisplay(display);
        UpdateCurrentProfileSummary();
    }

    private void PublishCalculationDisplay(MainWindowCalculationDisplay display)
    {
        ElementRows.Clear();
        foreach (var row in display.ElementRows)
        {
            ElementRows.Add(row);
        }

        ResultText = display.UserResultText;
        ReportText = display.TechnicalReportText;
        SequenceSummary = display.SequenceSummary;

        SequenceDiagramLines.Clear();
        foreach (var line in display.SequenceDiagramLines)
        {
            SequenceDiagramLines.Add(line);
        }

        VisualizationDepthM = display.VisualizationDepthM;
        VisualizationLineLengthM = display.VisualizationLineLengthM;
        VisualizationOffsetM = display.VisualizationOffsetM;
        VisualizationDepthText = display.VisualizationDepthText;
        VisualizationLineLengthText = display.VisualizationLineLengthText;
        VisualizationOffsetText = display.VisualizationOffsetText;
        VisualizationSlackRatioText = display.VisualizationSlackRatioText;
        VisualizationStatusText = display.VisualizationStatusText;
    }

    private static double Parse(string value)
    {
        value = (value ?? string.Empty).Replace(',', '.');
        return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0;
    }

    private static string FormatDouble(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string SafeText(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string NormalizeRopeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "built-in:polyester_20";
        var byDisplayName = RopeLibraryStorage.LoadAllRopes().FirstOrDefault(x => x.DisplayName == value);
        if (byDisplayName is not null) return byDisplayName.Id;
        if (value.StartsWith("user:", StringComparison.OrdinalIgnoreCase) || value.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase)) return value;
        return "built-in:" + value;
    }

    private static string NormalizeConnectorId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "built-in:shackle_55";
        var byDisplayName = ConnectorLibraryStorage.LoadAllConnectors().FirstOrDefault(x => x.DisplayName == value);
        if (byDisplayName is not null) return byDisplayName.Id;
        if (value.StartsWith("user:", StringComparison.OrdinalIgnoreCase) || value.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase)) return value;
        return "built-in:" + value;
    }

    private static string NormalizePayloadId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "built-in:adcp_40";
        var byDisplayName = PayloadLibraryStorage.LoadAllPayloads().FirstOrDefault(x => x.DisplayName == value);
        if (byDisplayName is not null) return byDisplayName.Id;
        if (value.StartsWith("user:", StringComparison.OrdinalIgnoreCase) || value.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase)) return value;
        return value;
    }

    private static string MakeSafeFileName(string value)
    {
        value = string.IsNullOrWhiteSpace(value) ? "BuoyCalc_Project" : value.Trim();
        foreach (var invalidChar in Path.GetInvalidFileNameChars()) value = value.Replace(invalidChar, '_');
        return value.Replace(' ', '_');
    }
}
