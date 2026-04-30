using System.Windows;
using System.Windows.Controls;
using AINovel.ViewModels;

namespace AINovel;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void NavListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is string selectedModule)
        {
            var vm = DataContext as MainViewModel;
            vm?.NavigateCommand.Execute(selectedModule);
        }
    }
}