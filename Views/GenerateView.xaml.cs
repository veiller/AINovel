using System.Windows;
using System.Windows.Controls;
using AINovel.ViewModels;

namespace AINovel.Views;

public partial class GenerateView : UserControl
{
    public GenerateView()
    {
        InitializeComponent();
    }

    private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is GenerateViewModel vm)
        {
            vm.ShowCoreDetailCommand.Execute(null);
        }
    }
}
