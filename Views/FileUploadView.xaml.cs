using System.Windows.Controls;
using AINovel.ViewModels;

namespace AINovel.Views;

public partial class FileUploadView : UserControl
{
    public FileUploadView()
    {
        InitializeComponent();
    }

    private void PreviewDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is FileUploadViewModel vm)
        {
            vm.ShowPreviewDetailCommand.Execute(null);
        }
    }
}
