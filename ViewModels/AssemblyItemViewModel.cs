using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.ViewModels;

public sealed class AssemblyItemViewModel : ViewModelBase
{
    private bool _isEnabled = true;
    private string _kind = "Line";
    private string _title = "Участок линии";
    private string _ropePresetId = "polyester_20";
    private string _connectorPresetId = "shackle_55";
    private string _lengthM = "10";
    private string _count = "1";
    private string _payloadWeightAirKg = "0";
    private string _payloadVolumeM3 = "0";
    private string _payloadProjectedAreaM2 = "0";
    private string _payloadDragCoefficient = "1.0";

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public string Kind
    {
        get => _kind;
        set => SetProperty(ref _kind, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string RopePresetId
    {
        get => _ropePresetId;
        set => SetProperty(ref _ropePresetId, value);
    }

    public string ConnectorPresetId
    {
        get => _connectorPresetId;
        set => SetProperty(ref _connectorPresetId, value);
    }

    public string LengthM
    {
        get => _lengthM;
        set => SetProperty(ref _lengthM, value);
    }

    public string Count
    {
        get => _count;
        set => SetProperty(ref _count, value);
    }

    public string PayloadWeightAirKg
    {
        get => _payloadWeightAirKg;
        set => SetProperty(ref _payloadWeightAirKg, value);
    }

    public string PayloadVolumeM3
    {
        get => _payloadVolumeM3;
        set => SetProperty(ref _payloadVolumeM3, value);
    }

    public string PayloadProjectedAreaM2
    {
        get => _payloadProjectedAreaM2;
        set => SetProperty(ref _payloadProjectedAreaM2, value);
    }

    public string PayloadDragCoefficient
    {
        get => _payloadDragCoefficient;
        set => SetProperty(ref _payloadDragCoefficient, value);
    }

    public AssemblyItemInput ToInput()
    {
        var kind = ParseKind(Kind);

        return new AssemblyItemInput(
            kind,
            Title,
            IsEnabled,
            kind == AssemblyItemKind.Line ? RopeCatalog.ById(RopePresetId) : null,
            kind == AssemblyItemKind.Connector ? ConnectorCatalog.ById(ConnectorPresetId) : null,
            ParseDouble(LengthM),
            ParseInt(Count),
            ParseDouble(PayloadWeightAirKg),
            ParseDouble(PayloadVolumeM3),
            ParseDouble(PayloadProjectedAreaM2),
            ParseDouble(PayloadDragCoefficient));
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
