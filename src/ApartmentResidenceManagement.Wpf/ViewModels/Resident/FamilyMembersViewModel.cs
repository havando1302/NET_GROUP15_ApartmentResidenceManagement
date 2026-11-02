using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class FamilyMembersViewModel : ViewModelBase
{
    public void Initialize(UserAccount account) { }
    public Task LoadFamilyMembersAsync() => Task.CompletedTask;
}
