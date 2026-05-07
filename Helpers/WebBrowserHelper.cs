using System.Windows.Controls;

namespace AINovel.Helpers;

public static class WebBrowserHelper
{
    public static readonly System.Windows.DependencyProperty HtmlProperty =
        System.Windows.DependencyProperty.RegisterAttached(
            "Html", typeof(string), typeof(WebBrowserHelper),
            new System.Windows.PropertyMetadata(OnHtmlChanged));

    public static string GetHtml(System.Windows.DependencyObject obj) =>
        (string)obj.GetValue(HtmlProperty);

    public static void SetHtml(System.Windows.DependencyObject obj, string value) =>
        obj.SetValue(HtmlProperty, value);

    private static void OnHtmlChanged(System.Windows.DependencyObject d, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (d is WebBrowser wb && e.NewValue is string html && !string.IsNullOrEmpty(html))
        {
            wb.NavigateToString(html);
        }
    }
}
