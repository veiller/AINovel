using System.Globalization;
using System.Windows.Data;

namespace AINovel.Converters;

public class GenerateStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int status)
        {
            return status switch
            {
                0 => "待生成",
                1 => "生成中",
                2 => "已生成",
                3 => "生成失败",
                4 => "已发布",
                5 => "等待生成",
                _ => "未知"
            };
        }
        return "未知";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}