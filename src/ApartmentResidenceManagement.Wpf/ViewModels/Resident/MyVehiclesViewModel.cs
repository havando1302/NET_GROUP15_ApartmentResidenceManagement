using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class MyVehiclesViewModel : ViewModelBase
{
    public void Initialize(UserAccount account) { }
    public Task LoadMyVehiclesAsync() => Task.CompletedTask;
}
