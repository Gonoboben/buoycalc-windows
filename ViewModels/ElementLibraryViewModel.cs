using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

namespace BuoyCalc.Windows.ViewModels;

public sealed class ElementLibraryViewModel : ViewModelBase
{
    private BuoyLibraryItem? _selectedBuoy;
    private string _buoyName = string.Empty;
    private string _buoyVolume = "0";
    private string _buoyWeight = "0";
    private string _buoyArea = "0";
    private string _buoyCd = "0";
    private string _buoyNote = string.Empty;

    private RopeLibraryItem? _selectedRope;
    private string _ropeName = string.Empty;
    private string _ropeMaterial = string.Empty;
    private string _ropeDiameter = "0";
    private string _ropeBreakingLoad = "0";
    private string _ropeWeightWater = "0";
    private string _ropeCd = "1.2";
    private string _ropeNote = string.Empty;

    private ConnectorLibraryItem? _selectedConnector;
    private string _connectorName = string.Empty;
    private string _connectorType = string.Empty;
    private string _connectorWeightAir = "0";
    private string _connectorVolume = "0";
    private string _connectorBreakingLoad = "0";
    private string _connectorArea = "0";
    private string _connectorCd = "1.2";
    private string _connectorNote = string.Empty;

    private AnchorLibraryItem? _selectedAnchor;
    private string _anchorName = string.Empty;
    private string _anchorType = string.Empty;
    private string _anchorMaterial = string.Empty;
    private string _anchorWeightAir = "0";
    private string _anchorVolume = "0";
    private string _anchorHoldingCoefficient = "1.0";
    private string _anchorNote = string.Empty;

    private string _statusText = string.Empty;

    public ElementLibraryViewModel()
    {
        Buoys = new ObservableCollection<BuoyLibraryItem>();
        Ropes = new ObservableCollection<RopeLibraryItem>();
        Connectors = new ObservableCollection<ConnectorLibraryItem>();
        Anchors = new ObservableCollection<AnchorLibraryItem>();

        NewBuoyCommand = new RelayCommand(NewBuoy);
        SaveBuoyCommand = new RelayCommand(SaveBuoy);
        DeleteBuoyCommand = new RelayCommand(DeleteBuoy);

        NewRopeCommand = new RelayCommand(NewRope);
        SaveRopeCommand = new RelayCommand(SaveRope);
        DeleteRopeCommand = new RelayCommand(DeleteRope);

        NewConnectorCommand = new RelayCommand(NewConnector);
        SaveConnectorCommand = new RelayCommand(SaveConnector);
        DeleteConnectorCommand = new RelayCommand(DeleteConnector);

        NewAnchorCommand = new RelayCommand(NewAnchor);
        SaveAnchorCommand = new RelayCommand(SaveAnchor);
        DeleteAnchorCommand = new RelayCommand(DeleteAnchor);

        RefreshCommand = new RelayCommand(RefreshAll);

        RefreshAll();
        StatusText = "Библиотека элементов открыта. Доступны разделы: буи, линии, соединители и якоря.";
    }

    public ObservableCollection<BuoyLibraryItem> Buoys { get; }
    public ObservableCollection<RopeLibraryItem> Ropes { get; }
    public ObservableCollection<ConnectorLibraryItem> Connectors { get; }
    public ObservableCollection<AnchorLibraryItem> Anchors { get; }

    public ICommand NewBuoyCommand { get; }
    public ICommand SaveBuoyCommand { get; }
    public ICommand DeleteBuoyCommand { get; }
    public ICommand NewRopeCommand { get; }
    public ICommand SaveRopeCommand { get; }
    public ICommand DeleteRopeCommand { get; }
    public ICommand NewConnectorCommand { get; }
    public ICommand SaveConnectorCommand { get; }
    public ICommand DeleteConnectorCommand { get; }
    public ICommand NewAnchorCommand { get; }
    public ICommand SaveAnchorCommand { get; }
    public ICommand DeleteAnchorCommand { get; }
    public ICommand RefreshCommand { get; }

    public BuoyLibraryItem? SelectedBuoy
    {
        get => _selectedBuoy;
        set { if (SetProperty(ref _selectedBuoy, value)) LoadSelectedBuoyIntoForm(); }
    }

    public RopeLibraryItem? SelectedRope
    {
        get => _selectedRope;
        set { if (SetProperty(ref _selectedRope, value)) LoadSelectedRopeIntoForm(); }
    }

    public ConnectorLibraryItem? SelectedConnector
    {
        get => _selectedConnector;
        set { if (SetProperty(ref _selectedConnector, value)) LoadSelectedConnectorIntoForm(); }
    }

    public AnchorLibraryItem? SelectedAnchor
    {
        get => _selectedAnchor;
        set { if (SetProperty(ref _selectedAnchor, value)) LoadSelectedAnchorIntoForm(); }
    }

    public string BuoyName { get => _buoyName; set => SetProperty(ref _buoyName, value); }
    public string BuoyVolume { get => _buoyVolume; set => SetProperty(ref _buoyVolume, value); }
    public string BuoyWeight { get => _buoyWeight; set => SetProperty(ref _buoyWeight, value); }
    public string BuoyArea { get => _buoyArea; set => SetProperty(ref _buoyArea, value); }
    public string BuoyCd { get => _buoyCd; set => SetProperty(ref _buoyCd, value); }
    public string BuoyNote { get => _buoyNote; set => SetProperty(ref _buoyNote, value); }

    public string RopeName { get => _ropeName; set => SetProperty(ref _ropeName, value); }
    public string RopeMaterial { get => _ropeMaterial; set => SetProperty(ref _ropeMaterial, value); }
    public string RopeDiameter { get => _ropeDiameter; set => SetProperty(ref _ropeDiameter, value); }
    public string RopeBreakingLoad { get => _ropeBreakingLoad; set => SetProperty(ref _ropeBreakingLoad, value); }
    public string RopeWeightWater { get => _ropeWeightWater; set => SetProperty(ref _ropeWeightWater, value); }
    public string RopeCd { get => _ropeCd; set => SetProperty(ref _ropeCd, value); }
    public string RopeNote { get => _ropeNote; set => SetProperty(ref _ropeNote, value); }

    public string ConnectorName { get => _connectorName; set => SetProperty(ref _connectorName, value); }
    public string ConnectorType { get => _connectorType; set => SetProperty(ref _connectorType, value); }
    public string ConnectorWeightAir { get => _connectorWeightAir; set => SetProperty(ref _connectorWeightAir, value); }
    public string ConnectorVolume { get => _connectorVolume; set => SetProperty(ref _connectorVolume, value); }
    public string ConnectorBreakingLoad { get => _connectorBreakingLoad; set => SetProperty(ref _connectorBreakingLoad, value); }
    public string ConnectorArea { get => _connectorArea; set => SetProperty(ref _connectorArea, value); }
    public string ConnectorCd { get => _connectorCd; set => SetProperty(ref _connectorCd, value); }
    public string ConnectorNote { get => _connectorNote; set => SetProperty(ref _connectorNote, value); }

    public string AnchorName { get => _anchorName; set => SetProperty(ref _anchorName, value); }
    public string AnchorType { get => _anchorType; set => SetProperty(ref _anchorType, value); }
    public string AnchorMaterial { get => _anchorMaterial; set => SetProperty(ref _anchorMaterial, value); }
    public string AnchorWeightAir { get => _anchorWeightAir; set => SetProperty(ref _anchorWeightAir, value); }
    public string AnchorVolume { get => _anchorVolume; set => SetProperty(ref _anchorVolume, value); }
    public string AnchorHoldingCoefficient { get => _anchorHoldingCoefficient; set => SetProperty(ref _anchorHoldingCoefficient, value); }
    public string AnchorNote { get => _anchorNote; set => SetProperty(ref _anchorNote, value); }

    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }

    private void RefreshAll()
    {
        RefreshBuoys(SelectedBuoy?.Id);
        RefreshRopes(SelectedRope?.Id);
        RefreshConnectors(SelectedConnector?.Id);
        RefreshAnchors(SelectedAnchor?.Id);
        StatusText = $"Библиотека обновлена. Буёв: {Buoys.Count}. Линий: {Ropes.Count}. Соединителей: {Connectors.Count}. Якорей: {Anchors.Count}.";
    }

    private void RefreshBuoys(string? selectedId)
    {
        Buoys.Clear();
        foreach (var buoy in BuoyLibraryStorage.LoadAllBuoys()) Buoys.Add(buoy);
        SelectedBuoy = Buoys.FirstOrDefault(x => x.Id == selectedId) ?? Buoys.FirstOrDefault();
    }

    private void RefreshRopes(string? selectedId)
    {
        Ropes.Clear();
        foreach (var rope in RopeLibraryStorage.LoadAllRopes()) Ropes.Add(rope);
        SelectedRope = Ropes.FirstOrDefault(x => x.Id == selectedId) ?? Ropes.FirstOrDefault();
    }

    private void RefreshConnectors(string? selectedId)
    {
        Connectors.Clear();
        foreach (var connector in ConnectorLibraryStorage.LoadAllConnectors()) Connectors.Add(connector);
        SelectedConnector = Connectors.FirstOrDefault(x => x.Id == selectedId) ?? Connectors.FirstOrDefault();
    }

    private void RefreshAnchors(string? selectedId)
    {
        Anchors.Clear();
        foreach (var anchor in AnchorLibraryStorage.LoadAllAnchors()) Anchors.Add(anchor);
        SelectedAnchor = Anchors.FirstOrDefault(x => x.Id == selectedId) ?? Anchors.FirstOrDefault();
    }

    private void LoadSelectedBuoyIntoForm()
    {
        if (SelectedBuoy is null) return;
        BuoyName = SelectedBuoy.Name;
        BuoyVolume = FormatDouble(SelectedBuoy.VolumeM3);
        BuoyWeight = FormatDouble(SelectedBuoy.WeightKg);
        BuoyArea = FormatDouble(SelectedBuoy.ProjectedAreaM2);
        BuoyCd = FormatDouble(SelectedBuoy.DragCoefficient);
        BuoyNote = SelectedBuoy.Note;
    }

    private void LoadSelectedRopeIntoForm()
    {
        if (SelectedRope is null) return;
        RopeName = SelectedRope.Name;
        RopeMaterial = SelectedRope.Material;
        RopeDiameter = FormatDouble(SelectedRope.DiameterMm);
        RopeBreakingLoad = FormatDouble(SelectedRope.BreakingLoadKn);
        RopeWeightWater = FormatDouble(SelectedRope.WeightWaterKgM);
        RopeCd = FormatDouble(SelectedRope.DragCoefficient);
        RopeNote = SelectedRope.Note;
    }

    private void LoadSelectedConnectorIntoForm()
    {
        if (SelectedConnector is null) return;
        ConnectorName = SelectedConnector.Name;
        ConnectorType = SelectedConnector.Type;
        ConnectorWeightAir = FormatDouble(SelectedConnector.WeightAirKg);
        ConnectorVolume = FormatDouble(SelectedConnector.VolumeM3);
        ConnectorBreakingLoad = FormatDouble(SelectedConnector.BreakingLoadKn);
        ConnectorArea = FormatDouble(SelectedConnector.ProjectedAreaM2);
        ConnectorCd = FormatDouble(SelectedConnector.DragCoefficient);
        ConnectorNote = SelectedConnector.Note;
    }

    private void LoadSelectedAnchorIntoForm()
    {
        if (SelectedAnchor is null) return;
        AnchorName = SelectedAnchor.Name;
        AnchorType = SelectedAnchor.Type;
        AnchorMaterial = SelectedAnchor.Material;
        AnchorWeightAir = FormatDouble(SelectedAnchor.WeightAirKg);
        AnchorVolume = FormatDouble(SelectedAnchor.VolumeM3);
        AnchorHoldingCoefficient = FormatDouble(SelectedAnchor.BaseHoldingCoefficient);
        AnchorNote = SelectedAnchor.Note;
    }

    private void NewBuoy()
    {
        SelectedBuoy = null;
        BuoyName = "Новый буй";
        BuoyVolume = "0.5";
        BuoyWeight = "80";
        BuoyArea = "0.5";
        BuoyCd = "0.8";
        BuoyNote = "";
        StatusText = "Заполните параметры нового буя и нажмите «Сохранить».";
    }

    private void SaveBuoy()
    {
        var name = string.IsNullOrWhiteSpace(BuoyName) ? "Пользовательский буй" : BuoyName.Trim();
        var id = SelectedBuoy is { Source: "User" } ? SelectedBuoy.Id : string.Empty;
        var buoy = new BuoyLibraryItem { Id = id, Source = "User", Name = name, VolumeM3 = Parse(BuoyVolume), WeightKg = Parse(BuoyWeight), ProjectedAreaM2 = Parse(BuoyArea), DragCoefficient = Parse(BuoyCd), Note = BuoyNote };
        BuoyLibraryStorage.UpsertUserBuoy(buoy);
        RefreshBuoys(buoy.Id);
        StatusText = $"Сохранён пользовательский буй: {name}";
    }

    private void DeleteBuoy()
    {
        if (SelectedBuoy is null) { StatusText = "Выберите пользовательский буй для удаления."; return; }
        if (SelectedBuoy.Source != "User" || SelectedBuoy.Id.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase)) { StatusText = "Встроенный буй удалить нельзя. Удалять можно только пользовательские буи."; return; }
        var deletedName = SelectedBuoy.Name;
        var deleted = BuoyLibraryStorage.DeleteUserBuoy(SelectedBuoy.Id);
        RefreshBuoys(null);
        StatusText = deleted ? $"Удалён пользовательский буй: {deletedName}" : "Пользовательский буй не найден в файле библиотеки.";
    }

    private void NewRope()
    {
        SelectedRope = null;
        RopeName = "Новая линия";
        RopeMaterial = "Polyester / Полиэстер";
        RopeDiameter = "20";
        RopeBreakingLoad = "70";
        RopeWeightWater = "0.15";
        RopeCd = "1.2";
        RopeNote = "";
        StatusText = "Заполните параметры новой линии и нажмите «Сохранить».";
    }

    private void SaveRope()
    {
        var name = string.IsNullOrWhiteSpace(RopeName) ? "Пользовательская линия" : RopeName.Trim();
        var id = SelectedRope is { Source: "User" } ? SelectedRope.Id : string.Empty;
        var rope = new RopeLibraryItem { Id = id, Source = "User", Name = name, Material = RopeMaterial, DiameterMm = Parse(RopeDiameter), BreakingLoadKn = Parse(RopeBreakingLoad), WeightWaterKgM = Parse(RopeWeightWater), DragCoefficient = Parse(RopeCd), Note = RopeNote };
        RopeLibraryStorage.UpsertUserRope(rope);
        RefreshRopes(rope.Id);
        StatusText = $"Сохранена пользовательская линия: {name}";
    }

    private void DeleteRope()
    {
        if (SelectedRope is null) { StatusText = "Выберите пользовательскую линию для удаления."; return; }
        if (SelectedRope.Source != "User" || SelectedRope.Id.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase)) { StatusText = "Встроенную линию удалить нельзя. Удалять можно только пользовательские линии."; return; }
        var deletedName = SelectedRope.Name;
        var deleted = RopeLibraryStorage.DeleteUserRope(SelectedRope.Id);
        RefreshRopes(null);
        StatusText = deleted ? $"Удалена пользовательская линия: {deletedName}" : "Пользовательская линия не найдена в файле библиотеки.";
    }

    private void NewConnector()
    {
        SelectedConnector = null;
        ConnectorName = "Новый соединитель";
        ConnectorType = "Скоба";
        ConnectorWeightAir = "1.2";
        ConnectorVolume = "0.00008";
        ConnectorBreakingLoad = "55";
        ConnectorArea = "0.004";
        ConnectorCd = "1.2";
        ConnectorNote = "";
        StatusText = "Заполните параметры нового соединителя и нажмите «Сохранить».";
    }

    private void SaveConnector()
    {
        var name = string.IsNullOrWhiteSpace(ConnectorName) ? "Пользовательский соединитель" : ConnectorName.Trim();
        var id = SelectedConnector is { Source: "User" } ? SelectedConnector.Id : string.Empty;
        var connector = new ConnectorLibraryItem { Id = id, Source = "User", Name = name, Type = ConnectorType, WeightAirKg = Parse(ConnectorWeightAir), VolumeM3 = Parse(ConnectorVolume), BreakingLoadKn = Parse(ConnectorBreakingLoad), ProjectedAreaM2 = Parse(ConnectorArea), DragCoefficient = Parse(ConnectorCd), Note = ConnectorNote };
        ConnectorLibraryStorage.UpsertUserConnector(connector);
        RefreshConnectors(connector.Id);
        StatusText = $"Сохранён пользовательский соединитель: {name}";
    }

    private void DeleteConnector()
    {
        if (SelectedConnector is null) { StatusText = "Выберите пользовательский соединитель для удаления."; return; }
        if (SelectedConnector.Source != "User" || SelectedConnector.Id.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase)) { StatusText = "Встроенный соединитель удалить нельзя. Удалять можно только пользовательские соединители."; return; }
        var deletedName = SelectedConnector.Name;
        var deleted = ConnectorLibraryStorage.DeleteUserConnector(SelectedConnector.Id);
        RefreshConnectors(null);
        StatusText = deleted ? $"Удалён пользовательский соединитель: {deletedName}" : "Пользовательский соединитель не найден в файле библиотеки.";
    }

    private void NewAnchor()
    {
        SelectedAnchor = null;
        AnchorName = "Новый якорь";
        AnchorType = "Deadweight";
        AnchorMaterial = "Concrete / Бетон";
        AnchorWeightAir = "500";
        AnchorVolume = "0.2";
        AnchorHoldingCoefficient = "1.0";
        AnchorNote = "";
        StatusText = "Заполните параметры нового якоря и нажмите «Сохранить».";
    }

    private void SaveAnchor()
    {
        var name = string.IsNullOrWhiteSpace(AnchorName) ? "Пользовательский якорь" : AnchorName.Trim();
        var id = SelectedAnchor is { Source: "User" } ? SelectedAnchor.Id : string.Empty;
        var anchor = new AnchorLibraryItem { Id = id, Source = "User", Name = name, Type = AnchorType, Material = AnchorMaterial, WeightAirKg = Parse(AnchorWeightAir), VolumeM3 = Parse(AnchorVolume), BaseHoldingCoefficient = Parse(AnchorHoldingCoefficient), Note = AnchorNote };
        AnchorLibraryStorage.UpsertUserAnchor(anchor);
        RefreshAnchors(anchor.Id);
        StatusText = $"Сохранён пользовательский якорь: {name}";
    }

    private void DeleteAnchor()
    {
        if (SelectedAnchor is null) { StatusText = "Выберите пользовательский якорь для удаления."; return; }
        if (SelectedAnchor.Source != "User" || SelectedAnchor.Id.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase)) { StatusText = "Встроенный якорь удалить нельзя. Удалять можно только пользовательские якоря."; return; }
        var deletedName = SelectedAnchor.Name;
        var deleted = AnchorLibraryStorage.DeleteUserAnchor(SelectedAnchor.Id);
        RefreshAnchors(null);
        StatusText = deleted ? $"Удалён пользовательский якорь: {deletedName}" : "Пользовательский якорь не найден в файле библиотеки.";
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
}
