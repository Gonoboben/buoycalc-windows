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
    private string _statusText = string.Empty;

    public ElementLibraryViewModel()
    {
        Buoys = new ObservableCollection<BuoyLibraryItem>();

        NewBuoyCommand = new RelayCommand(NewBuoy);
        SaveBuoyCommand = new RelayCommand(SaveBuoy);
        DeleteBuoyCommand = new RelayCommand(DeleteBuoy);
        RefreshCommand = new RelayCommand(() => RefreshBuoys(null));

        RefreshBuoys(null);
        StatusText = "Библиотека элементов открыта. Сейчас доступен раздел буёв.";
    }

    public ObservableCollection<BuoyLibraryItem> Buoys { get; }

    public ICommand NewBuoyCommand { get; }
    public ICommand SaveBuoyCommand { get; }
    public ICommand DeleteBuoyCommand { get; }
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

    public string BuoyName { get => _buoyName; set => SetProperty(ref _buoyName, value); }
    public string BuoyVolume { get => _buoyVolume; set => SetProperty(ref _buoyVolume, value); }
    public string BuoyWeight { get => _buoyWeight; set => SetProperty(ref _buoyWeight, value); }
    public string BuoyArea { get => _buoyArea; set => SetProperty(ref _buoyArea, value); }
    public string BuoyCd { get => _buoyCd; set => SetProperty(ref _buoyCd, value); }
    public string BuoyNote { get => _buoyNote; set => SetProperty(ref _buoyNote, value); }
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }

    private void RefreshBuoys(string? selectedId)
    {
        selectedId ??= SelectedBuoy?.Id;

        Buoys.Clear();
        foreach (var buoy in BuoyLibraryStorage.LoadAllBuoys())
        {
            Buoys.Add(buoy);
        }

        SelectedBuoy = Buoys.FirstOrDefault(x => x.Id == selectedId) ?? Buoys.FirstOrDefault();
        StatusText = $"Буёв в списке: {Buoys.Count}. Файл пользователя: {BuoyLibraryStorage.LibraryPath}";
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
