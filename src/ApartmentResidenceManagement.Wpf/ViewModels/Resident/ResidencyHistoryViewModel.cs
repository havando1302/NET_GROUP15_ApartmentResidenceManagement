using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class ResidencyHistoryViewModel : ViewModelBase
{
    public void Initialize(UserAccount account) { }
    public Task LoadHistoryAsync() => Task.CompletedTask;
}
