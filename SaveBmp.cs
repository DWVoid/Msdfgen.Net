using System;
using System.IO;

namespace Msdfgen
{
    public static class Bmp
    {
        private static void WriteBmpHeader(BinaryWriter file, int width, int height, ref int paddedWidth)
        {
            paddedWidth = 3 * width + 3 & ~3;
            const uint bitmapStart = 54;
            var bitmapSize = (uint) (paddedWidth * height);
            var fileSize = bitmapStart + bitmapSize;

            file.Write((ushort) 0x4d42u);
            file.Write(fileSize);
            file.Write((ushort) 0);
            file.Write((ushort) 0);
            file.Write(bitmapStart);

            file.Write((uint) 40);
            file.Write(width);
            file.Write(height);
            file.Write((ushort) 1);
            file.Write((ushort) 24);
            file.Write((uint) 0);
            file.Write(bitmapSize);
            file.Write((uint) 2835);
            file.Write((uint) 2835);
            file.Write((uint) 0);
            file.Write((uint) 0);
        }

        public static void SaveBmp(Bitmap<float> bitmap, string filename)
        {
            using (var stream = new FileStream(filename, FileMode.OpenOrCreate))
            {
                using (var file = new BinaryWriter(stream))
                {
                    var paddedWidth = 0;
                    WriteBmpHeader(file, bitmap.Width, bitmap.Height, ref paddedWidth);

                    var padding = new byte[paddedWidth - 3 * bitmap.Width];
                    
                    for (var y = 0; y < bitmap.Height; ++y)
                    {
                        for (var x = 0; x < bitmap.Width; ++x)
                        {
                            var px = (byte) Math.Clamp(bitmap[x, y] * 0x100, 0, 0xff);
                            file.Write(px);
                            file.Write(px);
                            file.Write(px);
                        }
                        file.Write(padding);
                    }
                }
            }
        }

        public static void SaveBmp(Bitmap<FloatRgb> bitmap, string filename)
        {
            using (var stream = new FileStream(filename, FileMode.OpenOrCreate))
            {
                using (var file = new BinaryWriter(stream))
                {
                    var paddedWidth = 0;
                    WriteBmpHeader(file, bitmap.Width, bitmap.Height, ref paddedWidth);

                    var padding = new byte[paddedWidth - 3 * bitmap.Width];
                    
                    for (var y = 0; y < bitmap.Height; ++y)
                    {
                        for (var x = 0; x < bitmap.Width; ++x)
                        {
                            var bgr = new[]
                            {
                                (byte) Math.Clamp(bitmap[x, y].B * 0x100, 0, 0xff),
                                (byte) Math.Clamp(bitmap[x, y].G * 0x100, 0, 0xff),
                                (byte) Math.Clamp(bitmap[x, y].R * 0x100, 0, 0xff)
                            };
                            file.Write(bgr);
                        }
                        file.Write(padding);
                    }
                }
            }
        }
    }
}