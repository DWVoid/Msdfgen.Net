using System.CodeDom.Compiler;
using Msdfgen.IO;

namespace Msdfgen.ManualTest
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            double advance = 0;
            var ft = ImportFont.InitializeFreetype();
            var font = ImportFont.LoadFont(ft, "test.otf");
            var shape = ImportFont.LoadGlyph(font, '曦', ref advance);
            var msdf = new Bitmap<FloatRgb>(32, 32);
            var generator = Generate.Msdf();
            generator.Output = msdf;
            generator.Range = 0.5;
            generator.Scale = new Vector2(2.0);
            generator.Translate = new Vector2(2, 2);

            for (int i = 0; i < 5; ++i)
            {
                shape.Normalize();
                Coloring.EdgeColoringSimple(shape, 3.0);
                generator.Shape = shape;
                generator.Compute();
                if (i % 100 == 0)
                    System.Console.WriteLine(i);
            }

            Bmp.SaveBmp(msdf, "output.bmp");
            {
                // MDSF Text
                var rast = new Bitmap<float>(1024, 1024);
                Render.RenderSdf(rast, msdf, 0.5);
                Bmp.SaveBmp(rast,"rasterized.bmp");
            }
            ImportFont.DestroyFont(font);
            ImportFont.DeinitializeFreetype(ft);
        }
    }
}
