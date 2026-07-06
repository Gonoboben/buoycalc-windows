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
        SelectedBuoyPreset = MainWindowLibraryRefreshSelectionBuilder.SelectBuoy(BuoyPresets, selectedId);
        BuoyLibraryStatusText = $"Библиотека: буёв {BuoyPresets.Count}, якорей {AnchorPresets.Count}.";
    }

    private void RefreshAnchorLibrary(string? selectedId)
    {
        AnchorPresets.Clear();
        foreach (var anchor in AnchorLibraryStorage.LoadAllAnchors()) AnchorPresets.Add(anchor);
        SelectedAnchorPreset = MainWindowLibraryRefreshSelectionBuilder.SelectAnchor(AnchorPresets, selectedId);
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
        var display = MainWindowSelectedPresetDisplayBuilder.BuildBuoy(SelectedBuoyPreset);
        BuoyName = display.Name;
        BuoyVolume = display.Volume;
        BuoyWeight = display.Weight;
        BuoyArea = display.ProjectedArea;
        BuoyCd = display.DragCoefficient;
    }

    private void ApplySelectedAnchorPreset()
    {
        if (SelectedAnchorPreset is null) return;
        var display = MainWindowSelectedPresetDisplayBuilder.BuildAnchor(SelectedAnchorPreset);
        AnchorName = display.Name;
        AnchorType = display.Type;
        AnchorMaterial = display.Material;
        AnchorWeight = display.Weight;
        AnchorVolume = display.Volume;
        AnchorCoefficient = display.BaseHoldingCoefficient;
    }

    private void SaveCurrentBuoyToLibrary()
    {
        var request = MainWindowUserBuoySaveBuilder.Build(
            new MainWindowUserBuoySaveSource(
                BuoyName,
                SelectedBuoyPreset,
                BuoyVolume,
                BuoyWeight,
                BuoyArea,
                BuoyCd));
        BuoyLibraryStorage.UpsertUserBuoy(request.Buoy);
        RefreshLibraries();
        BuoyLibraryStatusText = $"Буй сохранён в библиотеку: {request.NormalizedName}";
    }

    private void DeleteSelectedBuoyPreset()
    {
        var decision = MainWindowUserBuoyDeleteDecisionBuilder.Build(SelectedBuoyPreset);
        if (!decision.CanDelete)
        {
            BuoyLibraryStatusText = decision.BlockedStatusText;
            return;
        }

        var deleted = BuoyLibraryStorage.DeleteUserBuoy(decision.SelectedId);
        RefreshLibraries();
        BuoyLibraryStatusText = deleted
            ? $"Удалён пользовательский буй: {decision.CapturedName}"
            : "Пользовательский буй не найден в файле библиотеки.";
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
        var routes = MainWindowAssemblyItemLifecyclePlanBuilder.Build().UnwireRoutes;
        foreach (var item in AssemblyItems)
        {
            ApplyAssemblyItemLifecycleRoutes(item, routes, subscribe: false);
        }
        AssemblyItems.Clear();
    }

    private void WireItem(AssemblyItemViewModel item)
    {
        var routes = MainWindowAssemblyItemLifecyclePlanBuilder.Build().WireRoutes;
        ApplyAssemblyItemLifecycleRoutes(item, routes, subscribe: true);
    }

    private void ApplyAssemblyItemLifecycleRoutes(
        AssemblyItemViewModel item,
        System.Collections.Generic.IReadOnlyList<MainWindowAssemblyItemLifecycleRoute> routes,
        bool subscribe)
    {
        foreach (var route in routes)
        {
            switch (route)
            {
                case MainWindowAssemblyItemLifecycleRoute.RemoveRequested:
                    if (subscribe) item.RemoveRequested += RemoveItem; else item.RemoveRequested -= RemoveItem;
                    break;
                case MainWindowAssemblyItemLifecycleRoute.MoveUpRequested:
                    if (subscribe) item.MoveUpRequested += MoveItemUp; else item.MoveUpRequested -= MoveItemUp;
                    break;
                case MainWindowAssemblyItemLifecycleRoute.MoveDownRequested:
                    if (subscribe) item.MoveDownRequested += MoveItemDown; else item.MoveDownRequested -= MoveItemDown;
                    break;
                case MainWindowAssemblyItemLifecycleRoute.DuplicateRequested:
                    if (subscribe) item.DuplicateRequested += DuplicateItem; else item.DuplicateRequested -= DuplicateItem;
                    break;
                case MainWindowAssemblyItemLifecycleRoute.PropertyChanged:
                    if (subscribe) item.PropertyChanged += OnAssemblyItemChanged; else item.PropertyChanged -= OnAssemblyItemChanged;
                    break;
            }
        }
    }

    private void OnAssemblyItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is AssemblyItemViewModel { IsConnector: true } connector) connector.Count = "1";
        UpdateSequenceSummary();
    }

    private void RemoveItem(AssemblyItemViewModel item)
    {
        var routes = MainWindowAssemblyItemLifecyclePlanBuilder.Build().UnwireRoutes;
        ApplyAssemblyItemLifecycleRoutes(item, routes, subscribe: false);
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
        var template = MainWindowDefaultProjectTemplateBuilder.Build();
        ClearCurrentProfilePoints();
        foreach (var pointTemplate in template.CurrentProfilePoints)
        {
            AddCurrentProfilePoint(CreateDefaultCurrentProfilePoint(pointTemplate));
        }
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
            CurrentProfileSummary = MainWindowCurrentProfileSummaryBuilder.Build(
                false,
                CurrentSpeed,
                Array.Empty<CurrentProfilePointInput>());
            return;
        }

        if (CurrentProfilePoints.Count == 0)
        {
            CurrentProfileSummary = MainWindowCurrentProfileSummaryBuilder.Build(
                true,
                string.Empty,
                Array.Empty<CurrentProfilePointInput>());
            return;
        }

        var inputs = CurrentProfilePoints.Select(x => x.ToInput()).ToList();
        CurrentProfileSummary = MainWindowCurrentProfileSummaryBuilder.Build(
            true,
            string.Empty,
            inputs);
    }

    private void NewProject()
    {
        ResetToDefaultProject();
        ProjectStatusText = "Создан новый проект на основе стандартного шаблона.";
    }

    private void ResetToDefaultProject()
    {
        var template = MainWindowDefaultProjectTemplateBuilder.Build();

        ProjectName = template.ProjectName;
        ProjectFilePath = template.ProjectFilePath;
        WaterDensity = template.WaterDensity;
        Depth = template.Depth;
        CurrentSpeed = template.CurrentSpeed;
        UseCurrentProfile = template.UseCurrentProfile;
        WaveHeight = template.WaveHeight;
        WavePeriod = template.WavePeriod;
        SelectedSeabedPreset = SeabedCatalog.ById(template.SeabedPresetId);
        SelectedBuoyPreset = template.BuoyPresetId is null
            ? BuoyPresets.FirstOrDefault()
            : BuoyPresets.FirstOrDefault(x => x.Id == template.BuoyPresetId) ?? BuoyPresets.FirstOrDefault();
        SelectedAnchorPreset = AnchorPresets.FirstOrDefault(x => x.Id == template.PreferredAnchorPresetId) ?? AnchorPresets.FirstOrDefault();
        SafetyFactor = template.SafetyFactor;
        ResultText = template.ResultText;
        ReportText = template.ReportText;
        ElementRows.Clear();
        SequenceDiagramLines.Clear();

        ClearCurrentProfilePoints();
        ResetCurrentProfile();
        UseCurrentProfile = template.UseCurrentProfile;

        ClearAssemblyItems();
        foreach (var itemTemplate in template.AssemblyItems)
        {
            AddAssemblyItem(CreateDefaultAssemblyItem(itemTemplate));
        }

        UpdateSequenceSummary();
    }

    private CurrentProfilePointViewModel CreateDefaultCurrentProfilePoint(MainWindowDefaultCurrentProfilePointTemplate template)
    {
        return new CurrentProfilePointViewModel
        {
            DepthM = ResolveDefaultProjectValue(template.DepthM),
            EastCurrentMS = ResolveDefaultProjectValue(template.EastCurrentMS),
            NorthCurrentMS = ResolveDefaultProjectValue(template.NorthCurrentMS),
            VerticalCurrentMS = ResolveDefaultProjectValue(template.VerticalCurrentMS),
            WaterDensityKgM3 = ResolveDefaultProjectValue(template.WaterDensityKgM3)
        };
    }

    private string ResolveDefaultProjectValue(MainWindowDefaultProjectValue value)
    {
        return value.Source switch
        {
            MainWindowDefaultProjectValueSource.CurrentSpeed => CurrentSpeed,
            MainWindowDefaultProjectValueSource.Depth => Depth,
            MainWindowDefaultProjectValueSource.WaterDensity => WaterDensity,
            _ => value.Value
        };
    }

    private static AssemblyItemViewModel CreateDefaultAssemblyItem(MainWindowDefaultAssemblyItemTemplate template)
    {
        var item = new AssemblyItemViewModel
        {
            Kind = template.Kind,
            Title = template.Title
        };

        if (template.RopePresetStorageId is not null) item.RopePresetStorageId = template.RopePresetStorageId;
        if (template.ConnectorPresetStorageId is not null) item.ConnectorPresetStorageId = template.ConnectorPresetStorageId;
        if (template.PayloadPresetStorageId is not null) item.PayloadPresetStorageId = template.PayloadPresetStorageId;
        if (template.LengthM is not null) item.LengthM = template.LengthM;
        if (template.Count is not null) item.Count = template.Count;

        return item;
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
        return MainWindowProjectDtoMapper.ToDto(
            new MainWindowProjectSaveSource(
                new MainWindowProjectEnvironmentSaveSource(
                    ProjectName,
                    WaterDensity,
                    Depth,
                    CurrentSpeed,
                    UseCurrentProfile,
                    WaveHeight,
                    WavePeriod,
                    SelectedSeabedPreset?.Id),
                new MainWindowProjectBuoySaveSource(
                    BuoyName,
                    SelectedBuoyPreset?.Id,
                    BuoyVolume,
                    BuoyWeight,
                    BuoyArea,
                    BuoyCd),
                new MainWindowProjectAnchorSaveSource(
                    SelectedAnchorPreset?.Id,
                    AnchorName,
                    AnchorType,
                    AnchorMaterial,
                    AnchorWeight,
                    AnchorVolume,
                    AnchorCoefficient),
                SafetyFactor,
                CurrentProfilePoints.ToList(),
                AssemblyItems.ToList()));
    }

    private void FromDto(BuoyProjectDto dto)
    {
        var restore = MainWindowProjectDtoMapper.FromDto(dto);

        ProjectName = restore.Environment.ProjectName;
        WaterDensity = restore.Environment.WaterDensity;
        Depth = restore.Environment.Depth;
        CurrentSpeed = restore.Environment.CurrentSpeed;
        UseCurrentProfile = restore.Environment.UseCurrentProfile;
        WaveHeight = restore.Environment.WaveHeight;
        WavePeriod = restore.Environment.WavePeriod;
        SelectedSeabedPreset = SeabedPresets.FirstOrDefault(x => x.Id == restore.Environment.SelectedSeabedPresetId) ?? SeabedCatalog.ById("unknown");
        BuoyName = restore.Buoy.Name;
        RefreshLibraries();
        SelectedBuoyPreset = BuoyPresets.FirstOrDefault(x => x.Id == restore.Buoy.SelectedPresetId) ?? SelectedBuoyPreset;
        SelectedAnchorPreset = AnchorPresets.FirstOrDefault(x => x.Id == restore.Anchor.SelectedPresetId) ?? SelectedAnchorPreset;
        if (!string.IsNullOrWhiteSpace(restore.Anchor.Name)) AnchorName = restore.Anchor.Name;
        if (!string.IsNullOrWhiteSpace(restore.Anchor.Type)) AnchorType = restore.Anchor.Type;
        if (!string.IsNullOrWhiteSpace(restore.Anchor.Material)) AnchorMaterial = restore.Anchor.Material;
        if (!string.IsNullOrWhiteSpace(restore.Anchor.Weight)) AnchorWeight = restore.Anchor.Weight;
        if (!string.IsNullOrWhiteSpace(restore.Anchor.Volume)) AnchorVolume = restore.Anchor.Volume;
        if (!string.IsNullOrWhiteSpace(restore.Anchor.BaseHoldingCoefficient)) AnchorCoefficient = restore.Anchor.BaseHoldingCoefficient;
        SafetyFactor = restore.SafetyFactor;
        ResultText = "Проект загружен. Нажмите «Рассчитать».";
        ReportText = "";
        ElementRows.Clear();
        SequenceDiagramLines.Clear();

        ClearCurrentProfilePoints();
        foreach (var point in restore.CurrentProfilePoints)
        {
            AddCurrentProfilePoint(CurrentProfilePointViewModel.FromDto(point));
        }
        if (CurrentProfilePoints.Count == 0)
        {
            ResetCurrentProfile();
        }

        ClearAssemblyItems();
        foreach (var item in restore.AssemblyItems)
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
        SequenceSummary = MainWindowSequenceVisualizationDisplayBuilder.BuildSummary(enabledItems);
        UpdateSequenceDiagram();
        UpdateVisualizationSummary(result);
    }

    private void UpdateSequenceDiagram()
    {
        SequenceDiagramLines.Clear();

        var sequenceItems = AssemblyItems
            .Where(x => x.IsEnabled)
            .Select(x => new MainWindowSequenceDisplayItem(x.IsEnabled, x.KindDisplayName, x.Title, x.Summary))
            .ToList();
        var lines = MainWindowSequenceVisualizationDisplayBuilder.BuildDiagram(
            sequenceItems,
            BuoyName,
            AnchorName,
            AnchorType);

        foreach (var line in lines)
        {
            SequenceDiagramLines.Add(line);
        }
    }

    private void UpdateVisualizationSummary(CalculationResult? result = null)
    {
        var depthM = Parse(Depth);
        var enabledItems = AssemblyItems.Where(x => x.IsEnabled).Select(x => x.ToInput()).ToList();
        var visualization = MainWindowSequenceVisualizationDisplayBuilder.BuildVisualization(
            depthM,
            enabledItems,
            result?.EstimatedOffsetM);

        VisualizationDepthM = visualization.VisualizationDepthM;
        VisualizationLineLengthM = visualization.VisualizationLineLengthM;
        VisualizationOffsetM = visualization.VisualizationOffsetM;
        VisualizationDepthText = visualization.VisualizationDepthText;
        VisualizationLineLengthText = visualization.VisualizationLineLengthText;
        VisualizationOffsetText = visualization.VisualizationOffsetText;
        VisualizationSlackRatioText = visualization.VisualizationSlackRatioText;
        VisualizationStatusText = visualization.VisualizationStatusText;
    }

    private void Calculate()
    {
        var input = MainWindowCalculationInputBuilder.Build(
            new MainWindowCalculationInputSource(
                new MainWindowEnvironmentInputSource(
                    WaterDensity,
                    Depth,
                    CurrentSpeed,
                    WaveHeight,
                    WavePeriod,
                    SelectedSeabedPreset,
                    UseCurrentProfile,
                    CurrentProfilePoints.ToList()),
                new MainWindowBuoyInputSource(
                    BuoyName,
                    BuoyVolume,
                    BuoyWeight,
                    BuoyArea,
                    BuoyCd),
                new MainWindowAnchorInputSource(
                    AnchorName,
                    AnchorType,
                    AnchorMaterial,
                    AnchorWeight,
                    AnchorVolume,
                    AnchorCoefficient),
                AssemblyItems.ToList(),
                SafetyFactor));

        var result = BuoyCalculator.Calculate(
            input.Environment,
            input.Buoy,
            input.AssemblyItems,
            input.Anchor,
            input.SafetyFactor);
        var sequenceItems = AssemblyItems
            .Select(x => new MainWindowSequenceDisplayItem(x.IsEnabled, x.KindDisplayName, x.Title, x.Summary))
            .ToList();
        var display = MainWindowCalculationDisplayBuilder.Build(
            ProjectName,
            input.Environment,
            input.Buoy,
            input.Anchor,
            input.AssemblyItems,
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
