using System.Windows;
using System.Windows.Controls;
using Ledger.Desktop.ViewModels;

namespace Ledger.Desktop.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm && sender is PasswordBox box)
        {
            vm.Password = box.Password;
        }
    }
}
