using System.Windows.Controls;
using System.Windows.Input;
using AINovel.ViewModels;

namespace AINovel.Views;

public partial class CpView : UserControl
{
    public CpView()
    {
        InitializeComponent();
    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is CpViewModel vm)
        {
            vm.EditCpCommand.Execute(null);
        }
    }
}