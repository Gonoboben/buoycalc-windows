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

    private string _statusText = string.Empty;

    public ElementLibraryViewModel()
    {
        Buoys = new ObservableCollection<BuoyLibraryItem>();
        Ropes = new ObservableCollection<RopeLibraryItem>();

        NewBuoyCommand = new RelayCommand(NewBuoy);
        SaveBuoyCommand = new RelayCommand(SaveBuoy);
        DeleteBuoyCommand = new RelayCommand(DeleteBuoy);

        NewRopeCommand = new RelayCommand(NewRope);
        SaveRopeCommand = new RelayCommand(SaveRope);
        DeleteRopeCommand = new RelayCommand(DeleteRope);

        RefreshCommand = new RelayCommand(RefreshAll);

        RefreshAll();
        StatusText = "Библиотека элементов открыта. Доступны разделы: буи и линии.";
    }

    public ObservableCollection<BuoyLibraryItem> Buoys { get; }
    public ObservableCollection<RopeLibraryItem> Ropes { get; }

    public ICommand NewBuoyCommand { get; }
    public ICommand SaveBuoyCommand { get; }
    public ICommand DeleteBuoyCommand { get; }

    public ICommand NewRopeCommand { get; }
    public ICommand SaveRopeCommand { get; }
    public ICommand DeleteRopeCommand { get; }

    public ICommand RefreshCommand { get; }

    public BuoyLibraryItem? SelectedBuoy
    {
        get => _selectedBuoy;
        set
        {
            if (SetProperty(ref _selectedBuoy, value))
            {
                LoadSelectedBuoyIntoForm();
            }
        }
    }

    public RopeLibraryItem? SelectedRope
    {
        get => _selectedRope;
        set
        {
            if (SetProperty(ref _selectedRope, value))
            {
                LoadSelectedRopeIntoForm();
            }
        }
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

    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }

    private void RefreshAll()
    {
        RefreshBuoys(SelectedBuoy?.Id);
        RefreshRopes(SelectedRope?.Id);
        StatusText = $"Библиотека обновлена. Буёв: {Buoys.Count}. Линий: {Ropes.Count}.";
    }

    private void RefreshBuoys(string? selectedId)
    {
        Buoys.Clear();
        foreach (var buoy in BuoyLibraryStorage.LoadAllBuoys())
        {
            Buoys.Add(buoy);
        }

        SelectedBuoy = Buoys.FirstOrDefault(x => x.Id == selectedId) ?? Buoys.FirstOrDefault();
    }

    private void RefreshRopes(string? selectedId)
    {
        Ropes.Clear();
        foreach (var rope in RopeLibraryStorage.LoadAllRopes())
        {
            Ropes.Add(rope);
        }

        SelectedRope = Ropes.FirstOrDefault(x => x.Id == selectedId) ?? Ropes.FirstOrDefault();
    }

    private void LoadSelectedBuoyIntoForm()
    {
        if (SelectedBuoy is null)
        {
            return;
        }

        BuoyName = SelectedBuoy.Name;
        BuoyVolume = FormatDouble(SelectedBuoy.VolumeM3);
        BuoyWeight = FormatDouble(SelectedBuoy.WeightKg);
        BuoyArea = FormatDouble(SelectedBuoy.ProjectedAreaM2);
        BuoyCd = FormatDouble(SelectedBuoy.DragCoefficient);
        BuoyNote = SelectedBuoy.Note;
    }

    private void LoadSelectedRopeIntoForm()
    {
        if (SelectedRope is null)
        {
            return;
        }

        RopeName = SelectedRope.Name;
        RopeMaterial = SelectedRope.Material;
        RopeDiameter = FormatDouble(SelectedRope.DiameterMm);
        RopeBreakingLoad = FormatDouble(SelectedRope.BreakingLoadKn);
        RopeWeightWater = FormatDouble(SelectedRope.WeightWaterKgM);
        RopeCd = FormatDouble(SelectedRope.DragCoefficient);
        RopeNote = SelectedRope.Note;
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

        var buoy = new BuoyLibraryItem
        {
            Id = id,
            Source = "User",
            Name = name,
            VolumeM3 = Parse(BuoyVolume),
            WeightKg = Parse(BuoyWeight),
            ProjectedAreaM2 = Parse(BuoyArea),
            DragCoefficient = Parse(BuoyCd),
            Note = BuoyNote
        };

        BuoyLibraryStorage.UpsertUserBuoy(buoy);
        RefreshBuoys(buoy.Id);
        StatusText = $"Сохранён пользовательский буй: {name}";
    }

    private void DeleteBuoy()
    {
        if (SelectedBuoy is null)
        {
            StatusText = "Выберите пользовательский буй для удаления.";
            return;
        }

        if (SelectedBuoy.Source != "User" || SelectedBuoy.Id.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase))
        {
            StatusText = "Встроенный буй удалить нельзя. Удалять можно только пользовательские буи.";
            return;
        }

        var deletedName = SelectedBuoy.Name;
        var deleted = BuoyLibraryStorage.DeleteUserBuoy(SelectedBuoy.Id);
        RefreshBuoys(null);
        StatusText = deleted
            ? $"Удалён пользовательский буй: {deletedName}"
            : "Пользовательский буй не найден в файле библиотеки.";
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

        var rope = new RopeLibraryItem
        {
            Id = id,
            Source = "User",
            Name = name,
            Material = RopeMaterial,
            DiameterMm = Parse(RopeDiameter),
            BreakingLoadKn = Parse(RopeBreakingLoad),
            WeightWaterKgM = Parse(RopeWeightWater),
            DragCoefficient = Parse(RopeCd),
            Note = RopeNote
        };

        RopeLibraryStorage.UpsertUserRope(rope);
        RefreshRopes(rope.Id);
        StatusText = $"Сохранена пользовательская линия: {name}";
    }

    private void DeleteRope()
    {
        if (SelectedRope is null)
        {
            StatusText = "Выберите пользовательскую линию для удаления.";
            return;
        }

        if (SelectedRope.Source != "User" || SelectedRope.Id.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase))
        {
            StatusText = "Встроенную линию удалить нельзя. Удалять можно только пользовательские линии.";
            return;
        }

        var deletedName = SelectedRope.Name;
        var deleted = RopeLibraryStorage.DeleteUserRope(SelectedRope.Id);
        RefreshRopes(null);
        StatusText = deleted
            ? $"Удалена пользовательская линия: {deletedName}"
            : "Пользовательская линия не найдена в файле библиотеки.";
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
