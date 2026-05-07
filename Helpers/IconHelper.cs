using System.IO;

namespace AINovel.Helpers;

public static class IconHelper
{
    private static readonly (byte R, byte G, byte B, byte A) BgColor = (0x1B, 0x95, 0xD9, 0xFF); // 品牌蓝
    private static readonly (byte R, byte G, byte B, byte A) AccentColor = (0x15, 0x65, 0xA7, 0xFF); // 深蓝

    /// <summary>创建应用图标文件（32x32 蓝色圆形 + N 字母）</summary>
    public static void EnsureIcon(string filePath)
    {
        if (File.Exists(filePath)) return;

        const int size = 32;
        var pixels = new byte[size * size * 4];

        // 绘制图标像素
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var idx = (y * size + x) * 4;
                var (insideCircle, isAccent) = GetPixelType(x, y, size);

                if (insideCircle)
                {
                    var color = isAccent ? AccentColor : BgColor;
                    pixels[idx] = color.B;     // B
                    pixels[idx + 1] = color.G; // G
                    pixels[idx + 2] = color.R; // R
                    pixels[idx + 3] = color.A; // A
                }
                else
                {
                    pixels[idx + 3] = 0; // 全透明
                }

                // 白色 "N" 字母
                if (IsNLetter(x, y, size))
                {
                    pixels[idx] = 0xFF;
                    pixels[idx + 1] = 0xFF;
                    pixels[idx + 2] = 0xFF;
                    pixels[idx + 3] = 0xFF;
                }
            }
        }

        // 翻转至 BMP 底部优先格式
        var flipped = new byte[pixels.Length];
        for (var y = 0; y < size; y++)
            Buffer.BlockCopy(pixels, y * size * 4, flipped, (size - 1 - y) * size * 4, size * 4);

        // 构造 .ico 文件
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        bw.Write((ushort)0);             // reserved
        bw.Write((ushort)1);             // type: icon
        bw.Write((ushort)1);             // count: 1

        bw.Write((byte)size);            // width
        bw.Write((byte)size);            // height
        bw.Write((byte)0);               // color count
        bw.Write((byte)0);               // reserved
        bw.Write((ushort)1);             // planes
        bw.Write((ushort)32);            // bits per pixel

        var bmpInfoSize = 40;
        var andMaskSize = ((size + 31) / 32) * 4 * size;
        var dataSize = bmpInfoSize + flipped.Length + andMaskSize;

        bw.Write(dataSize);
        bw.Write(6 + 16);                // data offset

        // BITMAPINFOHEADER
        bw.Write(bmpInfoSize);
        bw.Write(size);
        bw.Write(size * 2);             // doubled height (ICO convention)
        bw.Write((ushort)1);             // planes
        bw.Write((ushort)32);            // bpp
        bw.Write(0);                     // compression
        bw.Write(flipped.Length);
        bw.Write(2835);                  // x pixels/m
        bw.Write(2835);                  // y pixels/m
        bw.Write(0);                     // colors used
        bw.Write(0);                     // important colors

        bw.Write(flipped);               // XOR mask

        for (var i = 0; i < andMaskSize; i++) // AND mask (全零)
            bw.Write((byte)0);

        File.WriteAllBytes(filePath, ms.ToArray());
    }

    private static (bool insideCircle, bool isAccent) GetPixelType(int x, int y, int size)
    {
        var cx = size / 2;
        var cy = size / 2;
        var dx = x - cx;
        var dy = y - cy;
        var dist = dx * dx + dy * dy;

        if (dist > 14 * 14) return (false, false);
        if (dist > 11 * 11 && (dx * dy > 0)) return (true, true); // 底部右下角阴影
        return (true, false);
    }

    /// <summary>在 32x32 像素网格中绘制白色 "N" 字母</summary>
    private static bool IsNLetter(int x, int y, int size)
    {
        // N 字母：两条竖线 + 对角线
        var leftX = 10;
        var rightX = 21;
        var topY = 7;
        var bottomY = 24;

        // 左竖线
        if (x >= leftX - 1 && x <= leftX + 1 && y >= topY && y <= bottomY)
            return true;

        // 右竖线
        if (x >= rightX - 1 && x <= rightX + 1 && y >= topY && y <= bottomY)
            return true;

        // 对角线
        var diagY = topY + (int)((double)(x - leftX) / (rightX - leftX) * (bottomY - topY));
        if (y >= diagY - 1 && y <= diagY + 1 && x >= leftX && x <= rightX)
            return true;

        return false;
    }
}
