namespace Msdfgen
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var ft = ImportFont.InitializeFreetype();
            var font = ImportFont.LoadFont(ft, "test.ttf");
            var shape = new Shape();
            double advance = 0;
            ImportFont.LoadGlyph(shape, font, 'G', ref advance);
            shape.Normalize();
            //                      max. angle
            Coloring.EdgeColoringSimple(shape, 3.0);
            var msdf = new Bitmap<FloatRgb>(32, 32);
            Generate.Msdf(msdf, shape, 4.0, new Vector2(1.0), new Vector2(4.0, 4.0));
            Bmp.SaveBmp(msdf, "output.bmp");
            ImportFont.DestroyFont(font);
            ImportFont.DeinitializeFreetype(ft);
        }
    }
}