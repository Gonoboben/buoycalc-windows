using System;
using System.Globalization;
using System.Windows.Input;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.ViewModels;

public sealed class CurrentProfilePointViewModel : ViewModelBase
{
    private string _depthM = "0";
    private string _eastCurrentMS = "0.5";
    private string _northCurrentMS = "0";
    private string _verticalCurrentMS = "0";
    private string _waterDensityKgM3 = "1025";

    public CurrentProfilePointViewModel()
    {
        RemoveCommand = new RelayCommand(() => RemoveRequested?.Invoke(this));
    }

    public event Action<CurrentProfilePointViewModel>? RemoveRequested;

    public ICommand RemoveCommand { get; }

    public string DepthM { get => _depthM; set => SetProperty(ref _depthM, value); }
    public string EastCurrentMS { get => _eastCurrentMS; set => SetProperty(ref _eastCurrentMS, value); }
    public string NorthCurrentMS { get => _northCurrentMS; set => SetProperty(ref _northCurrentMS, value); }
    public string VerticalCurrentMS { get => _verticalCurrentMS; set => SetProperty(ref _verticalCurrentMS, value); }
    public string WaterDensityKgM3 { get => _waterDensityKgM3; set => SetProperty(ref _waterDensityKgM3, value); }

    public string Summary
    {
        get
        {
            var input = ToInput();
            return $"z={input.DepthM:0.##} м · U={input.EastCurrentMS:0.###} · V={input.NorthCurrentMS:0.###} · W={input.VerticalCurrentMS:0.###} · |U|={input.SpeedMS:0.###} м/с · ρ={input.WaterDensityKgM3:0.##}";
        }
    }

    public CurrentProfilePointInput ToInput()
    {
        return new CurrentProfilePointInput(
            Parse(DepthM),
            Parse(EastCurrentMS),
            Parse(NorthCurrentMS),
            Parse(VerticalCurrentMS),
            Parse(WaterDensityKgM3));
    }

    public CurrentProfilePointDto ToDto()
    {
        return new CurrentProfilePointDto
        {
            DepthM = DepthM,
            EastCurrentMS = EastCurrentMS,
            NorthCurrentMS = NorthCurrentMS,
            VerticalCurrentMS = VerticalCurrentMS,
            WaterDensityKgM3 = WaterDensityKgM3
        };
    }

    public static CurrentProfilePointViewModel FromDto(CurrentProfilePointDto dto)
    {
        return new CurrentProfilePointViewModel
        {
            DepthM = dto.DepthM,
            EastCurrentMS = dto.EastCurrentMS,
            NorthCurrentMS = dto.NorthCurrentMS,
            VerticalCurrentMS = dto.VerticalCurrentMS,
            WaterDensityKgM3 = dto.WaterDensityKgM3
        };
    }

    public void RefreshSummary()
    {
        OnPropertyChanged(nameof(Summary));
    }

    private static double Parse(string value)
    {
        value = (value ?? string.Empty).Replace(',', '.');
        return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0;
    }
}
