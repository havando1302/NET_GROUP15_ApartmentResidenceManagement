using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class MyProfileViewModel : ViewModelBase
{
    public void Initialize(UserAccount account) { }
    public Task LoadProfileAsync() => Task.CompletedTask;
}
