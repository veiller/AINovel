using System.Text;
using System.Text.RegularExpressions;
using Markdig;

namespace AINovel.Helpers;

public static class MarkdownHelper
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public static string ToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        // 预处理：将单个换行转为空行分隔段落，避免 markdown 把多行合并为一段
        var normalized = markdown.Replace("\r\n", "\n");
        normalized = Regex.Replace(normalized, @"(?<!\n)\n(?!\n)", "\n\n");

        var body = Markdown.ToHtml(normalized, Pipeline);

        return $@"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'/>
<style>
    body {{
        font-family: 'Microsoft YaHei', 'Segoe UI', sans-serif;
        padding: 16px;
        line-height: 1.8;
        color: #333;
        font-size: 14px;
        margin: 0;
    }}
    h1, h2, h3, h4 {{ color: #1B95D9; }}
    h1 {{ border-bottom: 2px solid #1B95D9; padding-bottom: 8px; }}
    h2 {{ border-bottom: 1px solid #eee; padding-bottom: 4px; }}
    code {{ background: #f5f5f5; padding: 2px 6px; border-radius: 3px; font-family: Consolas, monospace; }}
    pre {{ background: #f5f5f5; padding: 12px; border-radius: 6px; overflow-x: auto; }}
    blockquote {{ border-left: 4px solid #1B95D9; margin: 8px 0; padding: 8px 16px; background: #f9f9f9; }}
    table {{ border-collapse: collapse; width: 100%; }}
    th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
    th {{ background: #f5f5f5; }}
    p {{ margin: 8px 0; }}
    img {{ max-width: 100%; }}
</style>
</head>
<body>{body}</body>
</html>";
    }

    /// <summary>
    /// 将 markdown 文本转为 Windows 剪贴板 HTML 格式（保留加粗等样式）
    /// </summary>
    public static string ToClipboardHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        var normalized = markdown.Replace("\r\n", "\n");
        normalized = Regex.Replace(normalized, @"(?<!\n)\n(?!\n)", "\n\n");

        var body = Markdown.ToHtml(normalized, Pipeline);
        var html = $"<!DOCTYPE html><html><meta charset=\"utf-8\"><body>{body}</body></html>";

        const string fragmentStart = "<!--StartFragment-->";
        const string fragmentEnd = "<!--EndFragment-->";
        const string prefixFormat = "Version:0.9\r\nStartHTML:{0:D8}\r\nEndHTML:{1:D8}\r\nStartFragment:{2:D8}\r\nEndFragment:{3:D8}\r\n";

        var encoding = Encoding.UTF8;

        // 前缀长度与具体数值无关（{0:D8} 固定 8 位数字）
        var prefixLen = encoding.GetByteCount(string.Format(prefixFormat, 0, 0, 0, 0));
        var fragStartLen = encoding.GetByteCount(fragmentStart);
        var htmlLen = encoding.GetByteCount(html);
        var fragEndLen = encoding.GetByteCount(fragmentEnd);

        var startHtml = prefixLen;
        var startFragment = startHtml + fragStartLen;
        var endFragment = startFragment + htmlLen;
        var endHtml = endFragment + fragEndLen;

        var prefix = string.Format(prefixFormat, startHtml, endHtml, startFragment, endFragment);
        return prefix + fragmentStart + html + fragmentEnd;
    }
}
