using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace AINovel.Helpers;

public static class DataGridBehavior
{
    public static readonly DependencyProperty SelectedItemsProperty =
        DependencyProperty.RegisterAttached(
            "SelectedItems", typeof(IList), typeof(DataGridBehavior),
            new FrameworkPropertyMetadata(null, OnSelectedItemsChanged));

    public static IList GetSelectedItems(DependencyObject obj) =>
        (IList)obj.GetValue(SelectedItemsProperty);

    public static void SetSelectedItems(DependencyObject obj, IList value) =>
        obj.SetValue(SelectedItemsProperty, value);

    private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataGrid grid)
        {
            grid.SelectionChanged -= OnGridSelectionChanged;
            grid.SelectionChanged += OnGridSelectionChanged;
        }
    }

    private static void OnGridSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is DataGrid grid)
        {
            var selected = new ArrayList(grid.SelectedItems);
            SetSelectedItems(grid, selected);
        }
    }
}
