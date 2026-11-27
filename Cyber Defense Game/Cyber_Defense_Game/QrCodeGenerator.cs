using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Cyber_Defense_Game;

public static class QrCodeGenerator
{
    private static readonly int[] DataCodewordsL = { 0, 19, 34, 55, 80, 108, 136 };
    private static readonly int[] EcCodewordsL = { 0, 7, 10, 15, 20, 26, 18 };
    private static readonly int[] BlocksL = { 0, 1, 1, 1, 1, 1, 2 };

    public static WriteableBitmap CreateBitmap(string text, int scale = 8, int quietZone = 3)
    {
        bool[,] modules = Encode(text);
        int size = modules.GetLength(0);
        int total = (size + quietZone * 2) * scale;

        var bmp = new WriteableBitmap(
            new PixelSize(total, total),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque);

        using var fb = bmp.Lock();
        byte[] buffer = new byte[total * fb.RowBytes];

        for (int y = 0; y < total; y++)
        {
            int modY = (y / scale) - quietZone;
            for (int x = 0; x < total; x++)
            {
                int modX = (x / scale) - quietZone;
                bool dark = modX >= 0 && modX < size && modY >= 0 && modY < size && modules[modY, modX];
                byte color = dark ? (byte)0x00 : (byte)0xFF;

                int offset = y * fb.RowBytes + x * 4;
                buffer[offset] = color;
                buffer[offset + 1] = color;
                buffer[offset + 2] = color;
                buffer[offset + 3] = 0xFF;
            }
        }

        Marshal.Copy(buffer, 0, fb.Address, buffer.Length);
        return bmp;
    }

    private static bool[,] Encode(string text)
    {
        byte[] raw = Encoding.UTF8.GetBytes(text);
        int version = 1;
        while (version <= 6 && raw.Length + 2 > DataCodewordsL[version])
            version++;

        if (version > 6)
            throw new ArgumentException("QR text too long.");

        int dataCap = DataCodewordsL[version];
        var bits = new List<int>();
        AppendBits(bits, 0b0100, 4);
        AppendBits(bits, raw.Length, 8);
        foreach (byte b in raw)
            AppendBits(bits, b, 8);

        int terminator = Math.Min(4, dataCap * 8 - bits.Count);
        AppendBits(bits, 0, terminator);
        while (bits.Count % 8 != 0)
            bits.Add(0);

        byte[] dataBytes = new byte[dataCap];
        for (int i = 0; i < bits.Count / 8; i++)
        {
            int val = 0;
            for (int b = 0; b < 8; b++)
                val = (val << 1) | bits[i * 8 + b];
            dataBytes[i] = (byte)val;
        }

        byte[] pad = { 0xEC, 0x11 };
        for (int i = bits.Count / 8, p = 0; i < dataCap; i++, p ^= 1)
            dataBytes[i] = pad[p];

        int numBlocks = BlocksL[version];
        int ecLen = EcCodewordsL[version];
        int blockSize = dataCap / numBlocks;

        var dataBlocks = new byte[numBlocks][];
        var ecBlocks = new byte[numBlocks][];
        for (int b = 0; b < numBlocks; b++)
        {
            dataBlocks[b] = new byte[blockSize];
            Array.Copy(dataBytes, b * blockSize, dataBlocks[b], 0, blockSize);
            ecBlocks[b] = ComputeReedSolomon(dataBlocks[b], ecLen);
        }

        var finalBytes = new List<byte>();
        for (int i = 0; i < blockSize; i++)
            for (int b = 0; b < numBlocks; b++)
                finalBytes.Add(dataBlocks[b][i]);
        for (int i = 0; i < ecLen; i++)
            for (int b = 0; b < numBlocks; b++)
                finalBytes.Add(ecBlocks[b][i]);

        int size = version * 4 + 17;
        bool[,] modules = new bool[size, size];
        bool[,] isFunction = new bool[size, size];

        DrawFinder(modules, isFunction, 0, 0);
        DrawFinder(modules, isFunction, size - 7, 0);
        DrawFinder(modules, isFunction, 0, size - 7);

        if (version >= 2)
        {
            int pos = size - 7;
            DrawAlignment(modules, isFunction, pos, pos);
        }

        for (int i = 0; i < size; i++)
        {
            SetFunc(modules, isFunction, 6, i, i % 2 == 0);
            SetFunc(modules, isFunction, i, 6, i % 2 == 0);
        }
        SetFunc(modules, isFunction, 8, size - 8, true);

        for (int i = 0; i < 9; i++)
        {
            if (i != 6)
            {
                isFunction[8, i] = true;
                isFunction[i, 8] = true;
            }
        }
        for (int i = 0; i < 8; i++)
        {
            isFunction[8, size - 1 - i] = true;
            isFunction[size - 1 - i, 8] = true;
        }

        int bitIdx = 0;
        int totalBits = finalBytes.Count * 8;
        bool upward = true;
        for (int right = size - 1; right >= 1; right -= 2)
        {
            if (right == 6) right = 5;
            for (int vert = 0; vert < size; vert++)
            {
                int y = upward ? size - 1 - vert : vert;
                for (int j = 0; j < 2; j++)
                {
                    int x = right - j;
                    if (!isFunction[y, x])
                    {
                        bool bit = false;
                        if (bitIdx < totalBits)
                        {
                            bit = ((finalBytes[bitIdx >> 3] >> (7 - (bitIdx & 7))) & 1) != 0;
                            bitIdx++;
                        }
                        if ((y + x) % 2 == 0)
                            bit = !bit;
                        modules[y, x] = bit;
                    }
                }
            }
            upward = !upward;
        }

        int fmt = 0x77C4;
        for (int i = 0; i <= 5; i++) modules[8, i] = ((fmt >> i) & 1) != 0;
        modules[8, 7] = ((fmt >> 6) & 1) != 0;
        modules[8, 8] = ((fmt >> 7) & 1) != 0;
        modules[7, 8] = ((fmt >> 8) & 1) != 0;
        for (int i = 9; i < 15; i++) modules[14 - i, 8] = ((fmt >> i) & 1) != 0;

        for (int i = 0; i < 8; i++) modules[size - 1 - i, 8] = ((fmt >> i) & 1) != 0;
        for (int i = 8; i < 15; i++) modules[8, size - 15 + i] = ((fmt >> i) & 1) != 0;

        return modules;
    }

    private static void SetFunc(bool[,] mod, bool[,] func, int x, int y, bool val)
    {
        mod[y, x] = val;
        func[y, x] = true;
    }

    private static void DrawFinder(bool[,] mod, bool[,] func, int x, int y)
    {
        for (int dy = -1; dy <= 7; dy++)
        {
            for (int dx = -1; dx <= 7; dx++)
            {
                int xx = x + dx, yy = y + dy;
                if (xx < 0 || yy < 0 || xx >= mod.GetLength(0) || yy >= mod.GetLength(0))
                    continue;
                int dist = Math.Max(Math.Abs(dx - 3), Math.Abs(dy - 3));
                SetFunc(mod, func, xx, yy, dist != 2 && dist != 4);
            }
        }
    }

    private static void DrawAlignment(bool[,] mod, bool[,] func, int cx, int cy)
    {
        for (int dy = -2; dy <= 2; dy++)
            for (int dx = -2; dx <= 2; dx++)
                SetFunc(mod, func, cx + dx, cy + dy, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1);
    }

    private static void AppendBits(List<int> bits, int val, int len)
    {
        for (int i = len - 1; i >= 0; i--)
            bits.Add((val >> i) & 1);
    }

    private static byte[] ComputeReedSolomon(byte[] data, int ecLen)
    {
        byte[] gen = new byte[ecLen];
        gen[ecLen - 1] = 1;
        int root = 1;
        for (int i = 0; i < ecLen; i++)
        {
            for (int j = 0; j < ecLen; j++)
            {
                gen[j] = MultiplyGf(gen[j], (byte)root);
                if (j + 1 < ecLen)
                    gen[j] ^= gen[j + 1];
            }
            root = MultiplyGf((byte)root, 0x02);
        }

        byte[] rem = new byte[ecLen];
        foreach (byte b in data)
        {
            byte factor = (byte)(b ^ rem[0]);
            Array.Copy(rem, 1, rem, 0, ecLen - 1);
            rem[ecLen - 1] = 0;
            for (int i = 0; i < ecLen; i++)
                rem[i] ^= MultiplyGf(gen[i], factor);
        }
        return rem;
    }

    private static byte MultiplyGf(byte x, byte y)
    {
        int z = 0;
        for (int i = 7; i >= 0; i--)
        {
            z = (z << 1) ^ ((z >> 7) * 0x11D);
            z ^= ((y >> i) & 1) * x;
        }
        return (byte)z;
    }
}
