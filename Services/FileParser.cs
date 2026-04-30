using System.Text.RegularExpressions;
using System.IO;

namespace AINovel.Services;

public class FileParser
{
    /// <summary>
    /// 解析文件提取核心梗
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>核心梗列表 (序号, 内容)</returns>
    public static List<(string SerialNumber, string Content)> ParseFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        var content = extension switch
        {
            ".md" => File.ReadAllText(filePath),
            ".docx" => ParseDocx(filePath),
            _ => throw new NotSupportedException($"不支持的文件格式: {extension}")
        };

        return ExtractCores(content);
    }

    private static string ParseDocx(string filePath)
    {
        // 使用 DocumentFormat.OpenXml 解析 docx
        try
        {
            using var document = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(filePath, false);
            var body = document.MainDocumentPart?.Document.Body;
            if (body == null) return string.Empty;

            var paragraphs = body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>();
            var lines = paragraphs.Select(p => p.InnerText);
            return string.Join(Environment.NewLine, lines);
        }
        catch
        {
            throw new InvalidOperationException("无法解析docx文件，请确保文件格式正确且未损坏");
        }
    }

    /// <summary>
    /// 按【序号】或 ### 核心梗 序号 格式提取核心梗
    /// </summary>
    private static List<(string, string)> ExtractCores(string content)
    {
        var result = new List<(string, string)>();

        // 尝试【序号】格式
        var pattern1 = @"【(\d+)】\s*([\s\S]*?)(?=(?:【\d+】)|$)";
        var matches1 = Regex.Matches(content, pattern1);
        if (matches1.Count > 0)
        {
            foreach (Match match in matches1)
            {
                var serialNumber = $"【{int.Parse(match.Groups[1].Value):D3}】";
                var coreContent = match.Groups[2].Value.Trim();
                if (!string.IsNullOrWhiteSpace(coreContent))
                {
                    result.Add((serialNumber, coreContent));
                }
            }
            return result;
        }

        // 尝试 ### 核心梗 序号 格式（包括标题行和内容）
        var pattern2 = @"(###\s*核心梗\s*(\d+)\s*[\s\S]*?)(?=(?:###\s*核心梗\s*\d+)|$)";
        var matches2 = Regex.Matches(content, pattern2, RegexOptions.IgnoreCase);
        foreach (Match match in matches2)
        {
            var serialNumber = $"【{int.Parse(match.Groups[2].Value):D3}】";
            var coreContent = match.Groups[1].Value.Trim();
            if (!string.IsNullOrWhiteSpace(coreContent))
            {
                result.Add((serialNumber, coreContent));
            }
        }

        return result;
    }

    /// <summary>
    /// 校验序号格式
    /// </summary>
    public static bool ValidateSerialNumber(string serialNumber)
    {
        return Regex.IsMatch(serialNumber, @"^【\d+】$");
    }
}