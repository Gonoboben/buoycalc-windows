using System.Threading.Tasks;

namespace BuoyCalc.Windows.Services;

public interface IProjectFileDialogService
{
    Task<string?> PickSavePathAsync(string suggestedFileName);
    Task<string?> PickOpenPathAsync();
}
