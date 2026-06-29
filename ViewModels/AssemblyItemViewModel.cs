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
    private string _connectorPresetStorageId = "built-in:shackle_55";
    private string _payloadPresetStorageId = "built-in:adcp_40";
    private string _lengthM = "10";
    private string _count = "1";
    private string _payloadWeightAirKg = "40";
    private string _payloadVolumeM3 = "0.015";
    private string _payloadProjectedAreaM2 = "0.05";
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
    public IReadOnlyList<string> ConnectorPresetOptions => ConnectorLibraryStorage.LoadAllConnectors().Select(x => x.DisplayName).ToList();
    public IReadOnlyList<string> PayloadPresetOptions => PayloadLibraryStorage.LoadAllPayloads().Select(x => x.DisplayName).ToList();

    public ICommand RemoveCommand { get; }
    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }
    public ICommand DuplicateCommand { get; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetProperty(ref _isEnabled, value))
            {
                OnPropertyChanged(nameof(Summary));
                OnPropertyChanged(nameof(EditorHint));
            }
        }
    }

    public string Kind
    {
        get => _kind;
        set
        {
            if (SetProperty(ref _kind, value))
            {
                if (IsConnector) Count = "1";
                if (IsPayload) ApplyPayloadPreset();
                OnPropertyChanged(nameof(KindDisplayName));
                OnPropertyChanged(nameof(IsLine));
                OnPropertyChanged(nameof(IsConnector));
                OnPropertyChanged(nameof(IsPayload));
                OnPropertyChanged(nameof(EditorHint));
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
        AssemblyItemKind.Payload => "Прибор",
        _ => "Линия"
    };

    public string EditorHint => ParseKind(Kind) switch
    {
        AssemblyItemKind.Connector => "Точечный соединитель: количество фиксировано 1, положение задаётся порядком в цепочке.",
        AssemblyItemKind.Payload => "Дискретная нагрузка: вес, объём, площадь и Cd берутся из пресета прибора.",
        _ => "Распределённый участок: длина входит в общую длину линии и форму X/Z."
    };

    public string Title
    {
        get => _title;
        set { if (SetProperty(ref _title, value)) OnPropertyChanged(nameof(Summary)); }
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
        get => GetConnectorDisplayName(_connectorPresetStorageId);
        set
        {
            var nextId = ResolveConnectorId(value);
            if (SetProperty(ref _connectorPresetStorageId, nextId))
            {
                OnPropertyChanged(nameof(ConnectorPresetId));
                OnPropertyChanged(nameof(Summary));
            }
        }
    }

    public string ConnectorPresetStorageId
    {
        get => _connectorPresetStorageId;
        set
        {
            var nextId = ResolveConnectorId(value);
            if (SetProperty(ref _connectorPresetStorageId, nextId))
            {
                OnPropertyChanged(nameof(ConnectorPresetId));
                OnPropertyChanged(nameof(Summary));
            }
        }
    }

    public string PayloadPresetId
    {
        get => GetPayloadDisplayName(_payloadPresetStorageId);
        set
        {
            var nextId = ResolvePayloadId(value);
            if (SetProperty(ref _payloadPresetStorageId, nextId))
            {
                ApplyPayloadPreset();
                OnPropertyChanged(nameof(PayloadPresetId));
                OnPropertyChanged(nameof(Summary));
            }
        }
    }

    public string PayloadPresetStorageId
    {
        get => _payloadPresetStorageId;
        set
        {
            var nextId = ResolvePayloadId(value);
            if (SetProperty(ref _payloadPresetStorageId, nextId))
            {
                ApplyPayloadPreset();
                OnPropertyChanged(nameof(PayloadPresetId));
                OnPropertyChanged(nameof(Summary));
            }
        }
    }

    public string LengthM { get => _lengthM; set { if (SetProperty(ref _lengthM, value)) OnPropertyChanged(nameof(Summary)); } }
    public string Count { get => _count; set { if (SetProperty(ref _count, value)) OnPropertyChanged(nameof(Summary)); } }
    public string PayloadWeightAirKg { get => _payloadWeightAirKg; set { if (SetProperty(ref _payloadWeightAirKg, value)) OnPropertyChanged(nameof(Summary)); } }
    public string PayloadVolumeM3 { get => _payloadVolumeM3; set { if (SetProperty(ref _payloadVolumeM3, value)) OnPropertyChanged(nameof(Summary)); } }
    public string PayloadProjectedAreaM2 { get => _payloadProjectedAreaM2; set { if (SetProperty(ref _payloadProjectedAreaM2, value)) OnPropertyChanged(nameof(Summary)); } }
    public string PayloadDragCoefficient { get => _payloadDragCoefficient; set { if (SetProperty(ref _payloadDragCoefficient, value)) OnPropertyChanged(nameof(Summary)); } }

    public string Summary
    {
        get
        {
            var state = IsEnabled ? "активен" : "отключён";
            return ParseKind(Kind) switch
            {
                AssemblyItemKind.Connector => $"{state} | точечный соединитель | {GetConnectorDisplayName(_connectorPresetStorageId)} | кол-во=1",
                AssemblyItemKind.Payload => $"{state} | дискретная нагрузка | {GetPayloadDisplayName(_payloadPresetStorageId)} | вес={PayloadWeightAirKg} кг | A={PayloadProjectedAreaM2} м2 | Cd={PayloadDragCoefficient}",
                _ => $"{state} | распределённая линия | {GetRopeDisplayName(_ropePresetStorageId)} | L={LengthM} м"
            };
        }
    }

    public void RefreshLibraryOptions()
    {
        OnPropertyChanged(nameof(RopePresetOptions));
        OnPropertyChanged(nameof(RopePresetId));
        OnPropertyChanged(nameof(ConnectorPresetOptions));
        OnPropertyChanged(nameof(ConnectorPresetId));
        OnPropertyChanged(nameof(PayloadPresetOptions));
        OnPropertyChanged(nameof(PayloadPresetId));
        OnPropertyChanged(nameof(EditorHint));
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
            ConnectorPresetStorageId = ConnectorPresetStorageId,
            PayloadPresetStorageId = PayloadPresetStorageId,
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
            kind == AssemblyItemKind.Connector ? ConnectorLibraryStorage.ById(ConnectorPresetStorageId) : null,
            ParseDouble(LengthM),
            count,
            ParseDouble(PayloadWeightAirKg),
            ParseDouble(PayloadVolumeM3),
            ParseDouble(PayloadProjectedAreaM2),
            ParseDouble(PayloadDragCoefficient));
    }

    private void ApplyPayloadPreset()
    {
        var payload = PayloadLibraryStorage.ById(_payloadPresetStorageId);
        PayloadWeightAirKg = FormatDouble(payload.WeightAirKg);
        PayloadVolumeM3 = FormatDouble(payload.VolumeM3);
        PayloadProjectedAreaM2 = FormatDouble(payload.ProjectedAreaM2);
        PayloadDragCoefficient = FormatDouble(payload.DragCoefficient);
        if (string.IsNullOrWhiteSpace(Title) || Title is "Новый прибор" or "Прибор" or "New payload" or "Payload")
        {
            Title = payload.Name;
        }
    }

    private static string GetRopeDisplayName(string id)
    {
        var rope = RopeLibraryStorage.LoadAllRopes().FirstOrDefault(x => x.Id == id || x.Id == "built-in:" + id);
        return rope?.DisplayName ?? id;
    }

    private static string ResolveRopeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "built-in:polyester_20";
        var byDisplayName = RopeLibraryStorage.LoadAllRopes().FirstOrDefault(x => x.DisplayName == value);
        if (byDisplayName is not null) return byDisplayName.Id;
        if (value.StartsWith("user:", StringComparison.OrdinalIgnoreCase) || value.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase)) return value;
        var builtIn = RopeCatalog.Presets.FirstOrDefault(x => x.Id == value);
        return builtIn is not null ? "built-in:" + value : value;
    }

    private static string GetConnectorDisplayName(string id)
    {
        var connector = ConnectorLibraryStorage.LoadAllConnectors().FirstOrDefault(x => x.Id == id || x.Id == "built-in:" + id);
        return connector?.DisplayName ?? id;
    }

    private static string ResolveConnectorId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "built-in:shackle_55";
        var byDisplayName = ConnectorLibraryStorage.LoadAllConnectors().FirstOrDefault(x => x.DisplayName == value);
        if (byDisplayName is not null) return byDisplayName.Id;
        if (value.StartsWith("user:", StringComparison.OrdinalIgnoreCase) || value.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase)) return value;
        var builtIn = ConnectorCatalog.Presets.FirstOrDefault(x => x.Id == value);
        return builtIn is not null ? "built-in:" + value : value;
    }

    private static string GetPayloadDisplayName(string id)
    {
        var payload = PayloadLibraryStorage.LoadAllPayloads().FirstOrDefault(x => x.Id == id || x.Id == "built-in:" + id);
        return payload?.DisplayName ?? id;
    }

    private static string ResolvePayloadId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "built-in:adcp_40";
        var byDisplayName = PayloadLibraryStorage.LoadAllPayloads().FirstOrDefault(x => x.DisplayName == value);
        if (byDisplayName is not null) return byDisplayName.Id;
        if (value.StartsWith("user:", StringComparison.OrdinalIgnoreCase) || value.StartsWith("built-in:", StringComparison.OrdinalIgnoreCase)) return value;
        return value;
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
        return double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : 0;
    }

    private static int ParseInt(string value)
    {
        return int.TryParse(value, out var result) ? result : 0;
    }

    private static string FormatDouble(double value)
    {
        return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    }
}
