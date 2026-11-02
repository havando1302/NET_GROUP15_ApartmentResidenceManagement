using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class MyApartmentViewModel : ViewModelBase
{
    public void Initialize(UserAccount account) { }
    public Task LoadApartmentDataAsync() => Task.CompletedTask;
}
