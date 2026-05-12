using System.Windows;
using System.Windows.Controls;
using Ledger.Desktop.ViewModels;

namespace Ledger.Desktop.Views;

public partial class FirstRunWindow : Window
{
    public FirstRunWindow()
    {
        InitializeComponent();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is FirstRunViewModel vm && sender is PasswordBox box)
        {
            vm.Password = box.Password;
        }
    }

    private void PasswordConfirmBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is FirstRunViewModel vm && sender is PasswordBox box)
        {
            vm.PasswordConfirm = box.Password;
        }
    }
}
