namespace Msdfgen
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var ft = ImportFont.InitializeFreetype();
            var font = ImportFont.LoadFont(ft, "test.otf");
            var shape = new Shape();
            double advance = 0;
            ImportFont.LoadGlyph(shape, font, '曦', ref advance);
            shape.Normalize();
            //                      max. angle
            Coloring.EdgeColoringSimple(shape, 3.1415);
            var msdf = new Bitmap<FloatRgb>(64, 64);
            Generate.Msdf(msdf, shape, 0.5, new Vector2(4.0), new Vector2(2, 2));
            Bmp.SaveBmp(msdf, "output.bmp");
            {
                // MDSF Text
                var rast = new Bitmap<float>(64, 64);
                Render.RenderSdf(rast, msdf, 0.5);
                Bmp.SaveBmp(rast,"rasterized.bmp");
            }
            ImportFont.DestroyFont(font);
            ImportFont.DeinitializeFreetype(ft);
        }
    }
}