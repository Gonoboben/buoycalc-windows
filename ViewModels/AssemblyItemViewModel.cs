using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

namespace BuoyCalc.Windows.ViewModels;

public sealed class AssemblyItemViewModel : ViewModelBase
{
    private bool _isEnabled = true;
    private string _kind = "Line";
    private string _title = "Участок линии";
    private string _ropePresetStorageId = "built-in:polyester_20";
    private string _connectorPresetId = "shackle_55";
    private string _lengthM = "10";
    private string _count = "1";
    private string _payloadWeightAirKg = "0";
    private string _payloadVolumeM3 = "0";
    private string _payloadProjectedAreaM2 = "0";
    private string _payloadDragCoefficient = "1.0";

    public AssemblyItemViewModel()
    {
        RemoveCommand = new RelayCommand(() => RemoveRequested?.Invoke(this));
        MoveUpCommand = new RelayCommand(() => MoveUpRequested?.Invoke(this));
        MoveDownCommand = new RelayCommand(() => MoveDownRequested?.Invoke(this));
        DuplicateCommand = new RelayCommand(() => DuplicateRequested?.Invoke(this));
    }

    public event Action<AssemblyItemViewModel>? RemoveRequested;
    public event Action<AssemblyItemViewModel>? MoveUpRequested;
    public event Action<AssemblyItemViewModel>? MoveDownRequested;
    public event Action<AssemblyItemViewModel>? DuplicateRequested;

    public IReadOnlyList<string> KindOptions { get; } = new[] { "Line", "Connector", "Payload" };
    public IReadOnlyList<string> RopePresetOptions => RopeLibraryStorage.LoadAllRopes().Select(x => x.DisplayName).ToList();
    public IReadOnlyList<string> ConnectorPresetOptions { get; } = ConnectorCatalog.Presets.Select(x => x.Id).ToList();

    public ICommand RemoveCommand { get; }
    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }
    public ICommand DuplicateCommand { get; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public string Kind
    {
        get => _kind;
        set
        {
            if (SetProperty(ref _kind, value))
            {
                if (IsConnector)
                {
                    Count = "1";
                }

                OnPropertyChanged(nameof(KindDisplayName));
                OnPropertyChanged(nameof(IsLine));
                OnPropertyChanged(nameof(IsConnector));
                OnPropertyChanged(nameof(IsPayload));
                OnPropertyChanged(nameof(Summary));
            }
        }
    }

    public bool IsLine => ParseKind(Kind) == AssemblyItemKind.Line;
    public bool IsConnector => ParseKind(Kind) == AssemblyItemKind.Connector;
    public bool IsPayload => ParseKind(Kind) == AssemblyItemKind.Payload;

    public string KindDisplayName => ParseKind(Kind) switch
    {
        AssemblyItemKind.Connector => "Соединитель",
        AssemblyItemKind.Payload => "Прибор / нагрузка",
        _ => "Буйреп / линия"
    };

    public string Title
    {
        get => _title;
        set
        {
            if (SetProperty(ref _title, value))
            {
                OnPropertyChanged(nameof(Summary));
            }
        }
    }

    public string RopePresetId
    {
        get => GetRopeDisplayName(_ropePresetStorageId);
        set
        {
            var nextId = ResolveRopeId(value);
            if (SetProperty(ref _ropePresetStorageId, nextId))
            {
                OnPropertyChanged(nameof(RopePresetId));
                OnPropertyChanged(nameof(Summary));
            }
        }
    }

    public string RopePresetStorageId
    {
        get => _ropePresetStorageId;
        set
        {
            var nextId = ResolveRopeId(value);
            if (SetProperty(ref _ropePresetStorageId, nextId))
            {
                OnPropertyChanged(nameof(RopePresetId));
                OnPropertyChanged(nameof(Summary));
            }
        }
    }

    public string ConnectorPresetId
    {
        get => _connectorPresetId;
        set
        {
            if (SetProperty(ref _connectorPresetId, value))
            {
                OnPropertyChanged(nameof(Summary));
            }
        }
    }

    public string LengthM
    {
        get => _lengthM;
        set
        {
            if (SetProperty(ref _lengthM, value))
            {
                OnPropertyChanged(nameof(Summary));
            }
        }
    }

    public string Count
    {
        get => _count;
        set
        {
            if (SetProperty(ref _count, value))
            {
                OnPropertyChanged(nameof(Summary));
            }
        }
    }

    public string PayloadWeightAirKg
    {
        get => _payloadWeightAirKg;
        set
        {
            if (SetProperty(ref _payloadWeightAirKg, value))
            {
                OnPropertyChanged(nameof(Summary));
            }
        }
    }

    public string PayloadVolumeM3
    {
        get => _payloadVolumeM3;
        set => SetProperty(ref _payloadVolumeM3, value);
    }

    public string PayloadProjectedAreaM2
    {
        get => _payloadProjectedAreaM2;
        set
        {
            if (SetProperty(ref _payloadProjectedAreaM2, value))
            {
                OnPropertyChanged(nameof(Summary));
            }
        }
    }

    public string PayloadDragCoefficient
    {
        get => _payloadDragCoefficient;
        set => SetProperty(ref _payloadDragCoefficient, value);
    }

    public string Summary
    {
        get
        {
            return ParseKind(Kind) switch
            {
                AssemblyItemKind.Connector => $"{ConnectorPresetId} · 1 элемент",
                AssemblyItemKind.Payload => $"{PayloadWeightAirKg} кг · A={PayloadProjectedAreaM2} м² · Cd={PayloadDragCoefficient}",
                _ => $"{GetRopeDisplayName(_ropePresetStorageId)} · {LengthM} м"
            };
        }
    }

    public void RefreshLibraryOptions()
    {
        OnPropertyChanged(nameof(RopePresetOptions));
        OnPropertyChanged(nameof(RopePresetId));
        OnPropertyChanged(nameof(Summary));
    }

    public AssemblyItemViewModel Clone()
    {
        return new AssemblyItemViewModel
        {
            IsEnabled = IsEnabled,
            Kind = Kind,
            Title = $"{Title} копия",
            RopePresetStorageId = RopePresetStorageId,
            ConnectorPresetId = ConnectorPresetId,
            LengthM = LengthM,
            Count = IsConnector ? "1" : Count,
            PayloadWeightAirKg = PayloadWeightAirKg,
            PayloadVolumeM3 = PayloadVolumeM3,
            PayloadProjectedAreaM2 = PayloadProjectedAreaM2,
            PayloadDragCoefficient = PayloadDragCoefficient
        };
    }

    public AssemblyItemInput ToInput()
    {
        var kind = ParseKind(Kind);
        var count = kind == AssemblyItemKind.Connector ? 1 : ParseInt(Count);

        return new AssemblyItemInput(
            kind,
            Title,
            IsEnabled,
            kind == AssemblyItemKind.Line ? RopeLibraryStorage.ById(RopePresetStorageId) : null,
            kind == AssemblyItemKind.Connector ? ConnectorCatalog.ById(ConnectorPresetId) : null,
            ParseDouble(LengthM),
            count,
            ParseDouble(PayloadWeightAirKg),
            ParseDouble(PayloadVolumeM3),
            ParseDouble(PayloadProjectedAreaM2),
            ParseDouble(PayloadDragCoefficient));
    }

    private static string GetRopeDisplayName(string id)
    {
        var rope = RopeLibraryStorage.LoadAllRopes().FirstOrDefault(x => x.Id == id || x.Id == "built-in:" + id);
        return rope?.DisplayName ?? id;
    }

    private static string ResolveRopeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "built-in:polyester_20";
        }

        var ropes = RopeLibraryStorage.LoadAllRopes();
        var byDisplayName = ropes.FirstOrDefault(x => x.DisplayName == value);
        if (byDisplayName is not null)
        {
            return byDisplayName.Id;
        }

        if (value.StartsWith("user:", StringComparison.OrdinalIgnoreCase) || value.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        var builtIn = RopeCatalog.Presets.FirstOrDefault(x => x.Id == value);
        return builtIn is not null ? "built-in:" + value : value;
    }

    private static AssemblyItemKind ParseKind(string value)
    {
        return value switch
        {
            "Connector" => AssemblyItemKind.Connector,
            "Payload" => AssemblyItemKind.Payload,
            _ => AssemblyItemKind.Line
        };
    }

    private static double ParseDouble(string value)
    {
        value = (value ?? string.Empty).Replace(',', '.');
        return double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result)
            ? result
            : 0;
    }

    private static int ParseInt(string value)
    {
        return int.TryParse(value, out var result) ? result : 0;
    }
}
