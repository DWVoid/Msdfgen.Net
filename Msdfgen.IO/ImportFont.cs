using System;
using SharpFont;

namespace Msdfgen.IO
{
    public static class ImportFont
    {
        public static Library InitializeFreetype()
        {
            return new Library();
        }

        public static void DeinitializeFreetype(Library library)
        {
            library.Dispose();
        }

        public static Face LoadFont(Library library, string filename)
        {
            return library.NewFace(filename, 0);
        }

        public static void DestroyFont(Face font)
        {
            font.Dispose();
        }

        public static double GetFontScale(Face font)
        {
            return font.UnitsPerEM / 64.0;
        }

        public static void GetFontWhitespaceWidth(ref double spaceAdvance, ref double tabAdvance, Face font)
        {
            font.LoadChar(' ', LoadFlags.NoScale, LoadTarget.Normal);
            spaceAdvance = font.Glyph.Advance.X.Value / 64.0;
            font.LoadChar('\t', LoadFlags.NoScale, LoadTarget.Normal);
            tabAdvance = font.Glyph.Advance.X.Value / 64.0;
        }

        public static void LoadGlyph(Shape output, Face font, uint unicode, ref double advance)
        {
            font.LoadChar(unicode, LoadFlags.NoScale, LoadTarget.Normal);
            output.Contours.Clear();
            output.InverseYAxis = false;
            advance = font.Glyph.Advance.X.Value / 64.0;
            var context = new FtContext(output);
            var ftFunctions = new OutlineFuncs
            {
                MoveFunction = context.FtMoveTo,
                LineFunction = context.FtLineTo,
                ConicFunction = context.FtConicTo,
                CubicFunction = context.FtCubicTo,
                Shift = 0
            };
            font.Glyph.Outline.Decompose(ftFunctions, IntPtr.Zero);
        }

        public static double GetKerning(Face font, uint unicode1, uint unicode2)
        {
            var kerning = font.GetKerning(font.GetCharIndex(unicode1), font.GetCharIndex(unicode2),
                KerningMode.Unscaled);
            return kerning.X.Value / 64.0;
        }

        private class FtContext
        {
            private readonly Shape _shape;
            private Contour _contour;
            private Vector2 _position;

            public FtContext(Shape output)
            {
                _shape = output;
            }

            private static Vector2 FtPoint2(ref FTVector vector)
            {
                return new Vector2(vector.X.Value / 64.0, vector.Y.Value / 64.0);
            }

            internal int FtMoveTo(ref FTVector to, IntPtr context)
            {
                _contour = new Contour();
                _shape.AddContour(_contour);
                _position = FtPoint2(ref to);
                return 0;
            }

            internal int FtLineTo(ref FTVector to, IntPtr context)
            {
                _contour.AddEdge(new EdgeHolder(_position, FtPoint2(ref to)));
                _position = FtPoint2(ref to);
                return 0;
            }

            internal int FtConicTo(ref FTVector control, ref FTVector to, IntPtr context)
            {
                _contour.AddEdge(new EdgeHolder(_position, FtPoint2(ref control), FtPoint2(ref to)));
                _position = FtPoint2(ref to);
                return 0;
            }

            internal int FtCubicTo(ref FTVector control1, ref FTVector control2, ref FTVector to, IntPtr context)
            {
                _contour.AddEdge(new EdgeHolder(_position, FtPoint2(ref control1), FtPoint2(ref control2),
                    FtPoint2(ref to)));
                _position = FtPoint2(ref to);
                return 0;
            }
        }
    }
}