using System;
using System.Windows.Input;
using ApartmentResidenceManagement.Wpf.Commands;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class UnderDevelopmentViewModel : ViewModelBase
{
    private string _featureTitle = "Tính năng đang phát triển";
    private string _featureDescription = "Tính năng này đang được đội ngũ kỹ thuật hoàn thiện và sẽ ra mắt trong bản cập nhật tiếp theo.";
    private string _featureIcon = "🚧";

    public string FeatureTitle
    {
        get => _featureTitle;
        set => SetProperty(ref _featureTitle, value);
    }

    public string FeatureDescription
    {
        get => _featureDescription;
        set => SetProperty(ref _featureDescription, value);
    }

    public string FeatureIcon
    {
        get => _featureIcon;
        set => SetProperty(ref _featureIcon, value);
    }

    public ICommand BackToHomeCommand { get; }

    public UnderDevelopmentViewModel(Action? onBackToHome = null)
    {
        BackToHomeCommand = new RelayCommand(_ => onBackToHome?.Invoke());
    }

    public void SetFeature(string title, string description = "", string icon = "🚧")
    {
        FeatureTitle = title;
        FeatureDescription = string.IsNullOrWhiteSpace(description)
            ? "Tính năng này đang được đội ngũ kỹ thuật hoàn thiện và sẽ ra mắt trong bản cập nhật tiếp theo."
            : description;
        FeatureIcon = icon;
    }
}
