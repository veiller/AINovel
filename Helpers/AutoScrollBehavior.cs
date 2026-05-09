using System.Windows.Controls;

namespace AINovel.Helpers;

public static class AutoScrollBehavior
{
    public static readonly System.Windows.DependencyProperty AutoScrollProperty =
        System.Windows.DependencyProperty.RegisterAttached(
            "AutoScroll", typeof(bool), typeof(AutoScrollBehavior),
            new System.Windows.PropertyMetadata(false, OnAutoScrollChanged));

    public static bool GetAutoScroll(System.Windows.DependencyObject obj) =>
        (bool)obj.GetValue(AutoScrollProperty);

    public static void SetAutoScroll(System.Windows.DependencyObject obj, bool value) =>
        obj.SetValue(AutoScrollProperty, value);

    private static void OnAutoScrollChanged(System.Windows.DependencyObject d, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer sv) return;

        if ((bool)e.NewValue)
        {
            sv.ScrollToBottom();
            sv.ScrollChanged += OnScrollChanged;
        }
        else
        {
            sv.ScrollChanged -= OnScrollChanged;
        }
    }

    private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        // 仅当有新内容追加时自动滚动，不影响用户手动滚动
        if (e.ExtentHeightChange > 0 && sender is ScrollViewer sv)
        {
            sv.ScrollToBottom();
        }
    }
}
