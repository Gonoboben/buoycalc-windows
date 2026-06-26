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
    private RopeLibraryItem? _selectedRope;
    private ConnectorLibraryItem? _selectedConnector;
    private AnchorLibraryItem? _selectedAnchor;
    private PayloadLibraryItem? _selectedPayload;

    private string _buoyName = string.Empty, _buoyVolume = "0", _buoyWeight = "0", _buoyArea = "0", _buoyCd = "0", _buoyNote = string.Empty;
    private string _ropeName = string.Empty, _ropeMaterial = string.Empty, _ropeDiameter = "0", _ropeBreakingLoad = "0", _ropeWeightWater = "0", _ropeCd = "1.2", _ropeNote = string.Empty;
    private string _connectorName = string.Empty, _connectorType = string.Empty, _connectorWeightAir = "0", _connectorVolume = "0", _connectorBreakingLoad = "0", _connectorArea = "0", _connectorCd = "1.2", _connectorNote = string.Empty;
    private string _anchorName = string.Empty, _anchorType = string.Empty, _anchorMaterial = string.Empty, _anchorWeightAir = "0", _anchorVolume = "0", _anchorHoldingCoefficient = "1.0", _anchorNote = string.Empty;
    private string _payloadName = string.Empty, _payloadType = string.Empty, _payloadWeightAir = "0", _payloadVolume = "0", _payloadArea = "0", _payloadCd = "1.0", _payloadNote = string.Empty;
    private string _statusText = string.Empty;

    public ElementLibraryViewModel()
    {
        Buoys = new ObservableCollection<BuoyLibraryItem>();
        Ropes = new ObservableCollection<RopeLibraryItem>();
        Connectors = new ObservableCollection<ConnectorLibraryItem>();
        Anchors = new ObservableCollection<AnchorLibraryItem>();
        Payloads = new ObservableCollection<PayloadLibraryItem>();

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
        NewPayloadCommand = new RelayCommand(NewPayload);
        SavePayloadCommand = new RelayCommand(SavePayload);
        DeletePayloadCommand = new RelayCommand(DeletePayload);
        RefreshCommand = new RelayCommand(RefreshAll);

        RefreshAll();
        StatusText = "Библиотека элементов открыта. Доступны разделы: буи, линии, соединители, якоря и приборы.";
    }

    public ObservableCollection<BuoyLibraryItem> Buoys { get; }
    public ObservableCollection<RopeLibraryItem> Ropes { get; }
    public ObservableCollection<ConnectorLibraryItem> Connectors { get; }
    public ObservableCollection<AnchorLibraryItem> Anchors { get; }
    public ObservableCollection<PayloadLibraryItem> Payloads { get; }

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
    public ICommand NewPayloadCommand { get; }
    public ICommand SavePayloadCommand { get; }
    public ICommand DeletePayloadCommand { get; }
    public ICommand RefreshCommand { get; }

    public BuoyLibraryItem? SelectedBuoy { get => _selectedBuoy; set { if (SetProperty(ref _selectedBuoy, value)) LoadSelectedBuoyIntoForm(); } }
    public RopeLibraryItem? SelectedRope { get => _selectedRope; set { if (SetProperty(ref _selectedRope, value)) LoadSelectedRopeIntoForm(); } }
    public ConnectorLibraryItem? SelectedConnector { get => _selectedConnector; set { if (SetProperty(ref _selectedConnector, value)) LoadSelectedConnectorIntoForm(); } }
    public AnchorLibraryItem? SelectedAnchor { get => _selectedAnchor; set { if (SetProperty(ref _selectedAnchor, value)) LoadSelectedAnchorIntoForm(); } }
    public PayloadLibraryItem? SelectedPayload { get => _selectedPayload; set { if (SetProperty(ref _selectedPayload, value)) LoadSelectedPayloadIntoForm(); } }

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

    public string PayloadName { get => _payloadName; set => SetProperty(ref _payloadName, value); }
    public string PayloadType { get => _payloadType; set => SetProperty(ref _payloadType, value); }
    public string PayloadWeightAir { get => _payloadWeightAir; set => SetProperty(ref _payloadWeightAir, value); }
    public string PayloadVolume { get => _payloadVolume; set => SetProperty(ref _payloadVolume, value); }
    public string PayloadArea { get => _payloadArea; set => SetProperty(ref _payloadArea, value); }
    public string PayloadCd { get => _payloadCd; set => SetProperty(ref _payloadCd, value); }
    public string PayloadNote { get => _payloadNote; set => SetProperty(ref _payloadNote, value); }

    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }

    private void RefreshAll()
    {
        RefreshBuoys(SelectedBuoy?.Id);
        RefreshRopes(SelectedRope?.Id);
        RefreshConnectors(SelectedConnector?.Id);
        RefreshAnchors(SelectedAnchor?.Id);
        RefreshPayloads(SelectedPayload?.Id);
        StatusText = $"Библиотека обновлена. Буёв: {Buoys.Count}. Линий: {Ropes.Count}. Соединителей: {Connectors.Count}. Якорей: {Anchors.Count}. Приборов: {Payloads.Count}.";
    }

    private void RefreshBuoys(string? selectedId) { Buoys.Clear(); foreach (var x in BuoyLibraryStorage.LoadAllBuoys()) Buoys.Add(x); SelectedBuoy = Buoys.FirstOrDefault(x => x.Id == selectedId) ?? Buoys.FirstOrDefault(); }
    private void RefreshRopes(string? selectedId) { Ropes.Clear(); foreach (var x in RopeLibraryStorage.LoadAllRopes()) Ropes.Add(x); SelectedRope = Ropes.FirstOrDefault(x => x.Id == selectedId) ?? Ropes.FirstOrDefault(); }
    private void RefreshConnectors(string? selectedId) { Connectors.Clear(); foreach (var x in ConnectorLibraryStorage.LoadAllConnectors()) Connectors.Add(x); SelectedConnector = Connectors.FirstOrDefault(x => x.Id == selectedId) ?? Connectors.FirstOrDefault(); }
    private void RefreshAnchors(string? selectedId) { Anchors.Clear(); foreach (var x in AnchorLibraryStorage.LoadAllAnchors()) Anchors.Add(x); SelectedAnchor = Anchors.FirstOrDefault(x => x.Id == selectedId) ?? Anchors.FirstOrDefault(); }
    private void RefreshPayloads(string? selectedId) { Payloads.Clear(); foreach (var x in PayloadLibraryStorage.LoadAllPayloads()) Payloads.Add(x); SelectedPayload = Payloads.FirstOrDefault(x => x.Id == selectedId) ?? Payloads.FirstOrDefault(); }

    private void LoadSelectedBuoyIntoForm() { if (SelectedBuoy is null) return; BuoyName = SelectedBuoy.Name; BuoyVolume = FormatDouble(SelectedBuoy.VolumeM3); BuoyWeight = FormatDouble(SelectedBuoy.WeightKg); BuoyArea = FormatDouble(SelectedBuoy.ProjectedAreaM2); BuoyCd = FormatDouble(SelectedBuoy.DragCoefficient); BuoyNote = SelectedBuoy.Note; }
    private void LoadSelectedRopeIntoForm() { if (SelectedRope is null) return; RopeName = SelectedRope.Name; RopeMaterial = SelectedRope.Material; RopeDiameter = FormatDouble(SelectedRope.DiameterMm); RopeBreakingLoad = FormatDouble(SelectedRope.BreakingLoadKn); RopeWeightWater = FormatDouble(SelectedRope.WeightWaterKgM); RopeCd = FormatDouble(SelectedRope.DragCoefficient); RopeNote = SelectedRope.Note; }
    private void LoadSelectedConnectorIntoForm() { if (SelectedConnector is null) return; ConnectorName = SelectedConnector.Name; ConnectorType = SelectedConnector.Type; ConnectorWeightAir = FormatDouble(SelectedConnector.WeightAirKg); ConnectorVolume = FormatDouble(SelectedConnector.VolumeM3); ConnectorBreakingLoad = FormatDouble(SelectedConnector.BreakingLoadKn); ConnectorArea = FormatDouble(SelectedConnector.ProjectedAreaM2); ConnectorCd = FormatDouble(SelectedConnector.DragCoefficient); ConnectorNote = SelectedConnector.Note; }
    private void LoadSelectedAnchorIntoForm() { if (SelectedAnchor is null) return; AnchorName = SelectedAnchor.Name; AnchorType = SelectedAnchor.Type; AnchorMaterial = SelectedAnchor.Material; AnchorWeightAir = FormatDouble(SelectedAnchor.WeightAirKg); AnchorVolume = FormatDouble(SelectedAnchor.VolumeM3); AnchorHoldingCoefficient = FormatDouble(SelectedAnchor.BaseHoldingCoefficient); AnchorNote = SelectedAnchor.Note; }
    private void LoadSelectedPayloadIntoForm() { if (SelectedPayload is null) return; PayloadName = SelectedPayload.Name; PayloadType = SelectedPayload.Type; PayloadWeightAir = FormatDouble(SelectedPayload.WeightAirKg); PayloadVolume = FormatDouble(SelectedPayload.VolumeM3); PayloadArea = FormatDouble(SelectedPayload.ProjectedAreaM2); PayloadCd = FormatDouble(SelectedPayload.DragCoefficient); PayloadNote = SelectedPayload.Note; }

    private void NewBuoy() { SelectedBuoy = null; BuoyName = "Новый буй"; BuoyVolume = "0.5"; BuoyWeight = "80"; BuoyArea = "0.5"; BuoyCd = "0.8"; BuoyNote = ""; StatusText = "Заполните параметры нового буя и нажмите «Сохранить»."; }
    private void SaveBuoy() { var name = string.IsNullOrWhiteSpace(BuoyName) ? "Пользовательский буй" : BuoyName.Trim(); var id = SelectedBuoy is { Source: "User" } ? SelectedBuoy.Id : string.Empty; var item = new BuoyLibraryItem { Id = id, Source = "User", Name = name, VolumeM3 = Parse(BuoyVolume), WeightKg = Parse(BuoyWeight), ProjectedAreaM2 = Parse(BuoyArea), DragCoefficient = Parse(BuoyCd), Note = BuoyNote }; BuoyLibraryStorage.UpsertUserBuoy(item); RefreshBuoys(item.Id); StatusText = $"Сохранён пользовательский буй: {name}"; }
    private void DeleteBuoy() { if (SelectedBuoy is null) { StatusText = "Выберите пользовательский буй для удаления."; return; } if (SelectedBuoy.Source != "User" || SelectedBuoy.Id.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase)) { StatusText = "Встроенный буй удалить нельзя. Удалять можно только пользовательские буи."; return; } var name = SelectedBuoy.Name; var ok = BuoyLibraryStorage.DeleteUserBuoy(SelectedBuoy.Id); RefreshBuoys(null); StatusText = ok ? $"Удалён пользовательский буй: {name}" : "Пользовательский буй не найден."; }

    private void NewRope() { SelectedRope = null; RopeName = "Новая линия"; RopeMaterial = "Polyester / Полиэстер"; RopeDiameter = "20"; RopeBreakingLoad = "70"; RopeWeightWater = "0.15"; RopeCd = "1.2"; RopeNote = ""; StatusText = "Заполните параметры новой линии и нажмите «Сохранить»."; }
    private void SaveRope() { var name = string.IsNullOrWhiteSpace(RopeName) ? "Пользовательская линия" : RopeName.Trim(); var id = SelectedRope is { Source: "User" } ? SelectedRope.Id : string.Empty; var item = new RopeLibraryItem { Id = id, Source = "User", Name = name, Material = RopeMaterial, DiameterMm = Parse(RopeDiameter), BreakingLoadKn = Parse(RopeBreakingLoad), WeightWaterKgM = Parse(RopeWeightWater), DragCoefficient = Parse(RopeCd), Note = RopeNote }; RopeLibraryStorage.UpsertUserRope(item); RefreshRopes(item.Id); StatusText = $"Сохранена пользовательская линия: {name}"; }
    private void DeleteRope() { if (SelectedRope is null) { StatusText = "Выберите пользовательскую линию для удаления."; return; } if (SelectedRope.Source != "User" || SelectedRope.Id.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase)) { StatusText = "Встроенную линию удалить нельзя."; return; } var name = SelectedRope.Name; var ok = RopeLibraryStorage.DeleteUserRope(SelectedRope.Id); RefreshRopes(null); StatusText = ok ? $"Удалена пользовательская линия: {name}" : "Пользовательская линия не найдена."; }

    private void NewConnector() { SelectedConnector = null; ConnectorName = "Новый соединитель"; ConnectorType = "Скоба"; ConnectorWeightAir = "1.2"; ConnectorVolume = "0.00008"; ConnectorBreakingLoad = "55"; ConnectorArea = "0.004"; ConnectorCd = "1.2"; ConnectorNote = ""; StatusText = "Заполните параметры нового соединителя и нажмите «Сохранить»."; }
    private void SaveConnector() { var name = string.IsNullOrWhiteSpace(ConnectorName) ? "Пользовательский соединитель" : ConnectorName.Trim(); var id = SelectedConnector is { Source: "User" } ? SelectedConnector.Id : string.Empty; var item = new ConnectorLibraryItem { Id = id, Source = "User", Name = name, Type = ConnectorType, WeightAirKg = Parse(ConnectorWeightAir), VolumeM3 = Parse(ConnectorVolume), BreakingLoadKn = Parse(ConnectorBreakingLoad), ProjectedAreaM2 = Parse(ConnectorArea), DragCoefficient = Parse(ConnectorCd), Note = ConnectorNote }; ConnectorLibraryStorage.UpsertUserConnector(item); RefreshConnectors(item.Id); StatusText = $"Сохранён пользовательский соединитель: {name}"; }
    private void DeleteConnector() { if (SelectedConnector is null) { StatusText = "Выберите пользовательский соединитель для удаления."; return; } if (SelectedConnector.Source != "User" || SelectedConnector.Id.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase)) { StatusText = "Встроенный соединитель удалить нельзя."; return; } var name = SelectedConnector.Name; var ok = ConnectorLibraryStorage.DeleteUserConnector(SelectedConnector.Id); RefreshConnectors(null); StatusText = ok ? $"Удалён пользовательский соединитель: {name}" : "Пользовательский соединитель не найден."; }

    private void NewAnchor() { SelectedAnchor = null; AnchorName = "Новый якорь"; AnchorType = "Deadweight"; AnchorMaterial = "Concrete / Бетон"; AnchorWeightAir = "500"; AnchorVolume = "0.2"; AnchorHoldingCoefficient = "1.0"; AnchorNote = ""; StatusText = "Заполните параметры нового якоря и нажмите «Сохранить»."; }
    private void SaveAnchor() { var name = string.IsNullOrWhiteSpace(AnchorName) ? "Пользовательский якорь" : AnchorName.Trim(); var id = SelectedAnchor is { Source: "User" } ? SelectedAnchor.Id : string.Empty; var item = new AnchorLibraryItem { Id = id, Source = "User", Name = name, Type = AnchorType, Material = AnchorMaterial, WeightAirKg = Parse(AnchorWeightAir), VolumeM3 = Parse(AnchorVolume), BaseHoldingCoefficient = Parse(AnchorHoldingCoefficient), Note = AnchorNote }; AnchorLibraryStorage.UpsertUserAnchor(item); RefreshAnchors(item.Id); StatusText = $"Сохранён пользовательский якорь: {name}"; }
    private void DeleteAnchor() { if (SelectedAnchor is null) { StatusText = "Выберите пользовательский якорь для удаления."; return; } if (SelectedAnchor.Source != "User" || SelectedAnchor.Id.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase)) { StatusText = "Встроенный якорь удалить нельзя."; return; } var name = SelectedAnchor.Name; var ok = AnchorLibraryStorage.DeleteUserAnchor(SelectedAnchor.Id); RefreshAnchors(null); StatusText = ok ? $"Удалён пользовательский якорь: {name}" : "Пользовательский якорь не найден."; }

    private void NewPayload() { SelectedPayload = null; PayloadName = "Новый прибор"; PayloadType = "Sensor"; PayloadWeightAir = "10"; PayloadVolume = "0.004"; PayloadArea = "0.02"; PayloadCd = "1.0"; PayloadNote = ""; StatusText = "Заполните параметры нового прибора и нажмите «Сохранить»."; }
    private void SavePayload() { var name = string.IsNullOrWhiteSpace(PayloadName) ? "Пользовательский прибор" : PayloadName.Trim(); var id = SelectedPayload is { Source: "User" } ? SelectedPayload.Id : string.Empty; var item = new PayloadLibraryItem { Id = id, Source = "User", Name = name, Type = PayloadType, WeightAirKg = Parse(PayloadWeightAir), VolumeM3 = Parse(PayloadVolume), ProjectedAreaM2 = Parse(PayloadArea), DragCoefficient = Parse(PayloadCd), Note = PayloadNote }; PayloadLibraryStorage.UpsertUserPayload(item); RefreshPayloads(item.Id); StatusText = $"Сохранён пользовательский прибор: {name}"; }
    private void DeletePayload() { if (SelectedPayload is null) { StatusText = "Выберите пользовательский прибор для удаления."; return; } if (SelectedPayload.Source != "User" || SelectedPayload.Id.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase)) { StatusText = "Встроенный прибор удалить нельзя. Удалять можно только пользовательские приборы."; return; } var name = SelectedPayload.Name; var ok = PayloadLibraryStorage.DeleteUserPayload(SelectedPayload.Id); RefreshPayloads(null); StatusText = ok ? $"Удалён пользовательский прибор: {name}" : "Пользовательский прибор не найден."; }

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
