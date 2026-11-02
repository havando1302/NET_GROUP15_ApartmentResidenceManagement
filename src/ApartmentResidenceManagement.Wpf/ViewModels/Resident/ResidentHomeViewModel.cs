using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class ResidentHomeViewModel : ViewModelBase
{
    public void Initialize(UserAccount account) { }
    public Task LoadStatsAsync() => Task.CompletedTask;
}
