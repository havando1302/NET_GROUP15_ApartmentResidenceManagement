using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Exceptions;
using ApartmentResidenceManagement.Application.Services;
using ApartmentResidenceManagement.Wpf.Commands;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly AccountService _accountService;
    private string _username = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _hasError;
    private bool _isLoading;
    private string _selectedRole = "Admin"; // "Admin" hoặc "Resident"
    private bool _rememberMe;

    // Cơ chế chống tấn công Brute-force (Rate Limiting / Lockout)
    private int _failedAttempts = 0;
    private DateTime? _lockoutEndTime = null;

    public event EventHandler? OnLoginSuccess;
    public UserAccount? LoggedInAccount { get; private set; }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                HasError = !string.IsNullOrEmpty(value);
            }
        }
    }

    public bool HasError
    {
        get => _hasError;
        set => SetProperty(ref _hasError, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string SelectedRole
    {
        get => _selectedRole;
        set
        {
            if (SetProperty(ref _selectedRole, value))
            {
                OnPropertyChanged(nameof(IsAdminSelected));
                OnPropertyChanged(nameof(IsResidentSelected));
            }
        }
    }

    public bool IsAdminSelected => SelectedRole == "Admin";
    public bool IsResidentSelected => SelectedRole == "Resident";

    public bool RememberMe
    {
        get => _rememberMe;
        set => SetProperty(ref _rememberMe, value);
    }

    public ICommand LoginCommand { get; }
    public ICommand SelectAdminRoleCommand { get; }
    public ICommand SelectResidentRoleCommand { get; }
    public ICommand FillAdminDemoCommand { get; }
    public ICommand FillResidentDemoCommand { get; }

    public LoginViewModel(AccountService accountService)
    {
        _accountService = accountService;

        SelectAdminRoleCommand = new RelayCommand(_ => SelectedRole = "Admin");
        SelectResidentRoleCommand = new RelayCommand(_ => SelectedRole = "Resident");

        // Điền tài khoản mẫu Ban quản lý: admin / admin123
        FillAdminDemoCommand = new RelayCommand(parameter =>
        {
            SelectedRole = "Admin";
            Username = "admin";
            if (parameter is PasswordBox pbox)
            {
                pbox.Password = "admin123";
            }
            ErrorMessage = string.Empty;
        });

        // Điền tài khoản mẫu Cư dân: resident_a / resident123
        FillResidentDemoCommand = new RelayCommand(parameter =>
        {
            SelectedRole = "Resident";
            Username = "resident_a";
            if (parameter is PasswordBox pbox)
            {
                pbox.Password = "resident123";
            }
            ErrorMessage = string.Empty;
        });

        LoginCommand = new AsyncRelayCommand(ExecuteLoginAsync, CanExecuteLogin);
    }

    private bool CanExecuteLogin(object? parameter)
    {
        return !string.IsNullOrWhiteSpace(Username) && !IsLoading;
    }

    private async Task ExecuteLoginAsync(object? parameter)
    {
        ErrorMessage = string.Empty;

        // Kiểm tra khóa tạm thời do nhập sai quá 5 lần
        if (_lockoutEndTime.HasValue && DateTime.Now < _lockoutEndTime.Value)
        {
            var remaining = Math.Ceiling((_lockoutEndTime.Value - DateTime.Now).TotalSeconds);
            ErrorMessage = $"Tài khoản tạm thời bị khóa do nhập sai nhiều lần. Vui lòng đợi {remaining} giây trước khi thử lại.";
            return;
        }

        if (parameter is not PasswordBox passwordBox)
        {
            ErrorMessage = "Không thể đọc thông tin mật khẩu.";
            return;
        }

        string password = passwordBox.Password;
        IsLoading = true;

        try
        {
            var account = await _accountService.LoginAsync(Username, password);
            if (account != null)
            {
                // Reset số lần đăng nhập sai khi thành công
                _failedAttempts = 0;
                _lockoutEndTime = null;

                LoggedInAccount = account;
                OnLoginSuccess?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (BusinessRuleException ex)
        {
            _failedAttempts++;
            if (_failedAttempts >= 5)
            {
                _lockoutEndTime = DateTime.Now.AddSeconds(60);
                _failedAttempts = 0;
                ErrorMessage = "Bạn đã nhập sai thông tin 5 lần liên tiếp. Để đảm bảo an toàn, tính năng đăng nhập tạm khóa trong 60 giây.";
            }
            else
            {
                ErrorMessage = ex.Message;
            }
        }
        catch (Exception)
        {
            ErrorMessage = "Đã xảy ra lỗi không mong muốn trong quá trình đăng nhập. Vui lòng thử lại.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
