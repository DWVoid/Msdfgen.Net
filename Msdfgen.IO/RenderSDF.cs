using System;

namespace Msdfgen
{
    public static class Render
    {
        private static FloatRgb Mix(FloatRgb a, FloatRgb b, double weight)
        {
            var output = new FloatRgb
            {
                R = Arithmetic.Mix(a.R, b.R, weight),
                G = Arithmetic.Mix(a.G, b.G, weight),
                B = Arithmetic.Mix(a.B, b.B, weight)
            };
            return output;
        }

        private static FloatRgb Sample(Bitmap<FloatRgb> bitmap, Vector2 pos)
        {
            int w = bitmap.Width, h = bitmap.Height;
            var x = pos.X * w - .5;
            var y = pos.Y * h - .5;
            var l = (int) Math.Floor(x);
            var b = (int) Math.Floor(y);
            var r = l + 1;
            var t = b + 1;
            var lr = x - l;
            var bt = y - b;
            l = Math.Clamp(l, 0, w - 1);
            r = Math.Clamp(r, 0, w - 1);
            b = Math.Clamp(b, 0, h - 1);
            t = Math.Clamp(t, 0, h - 1);
            return Mix(Mix(bitmap[l, b], bitmap[r, b], lr), Mix(bitmap[l, t], bitmap[r, t], lr), bt);
        }

        private static float Sample(Bitmap<float> bitmap, Vector2 pos)
        {
            int w = bitmap.Width, h = bitmap.Height;
            var x = pos.X * w - .5;
            var y = pos.Y * h - .5;
            var l = (int) Math.Floor(x);
            var b = (int) Math.Floor(y);
            var r = l + 1;
            var t = b + 1;
            var lr = x - l;
            var bt = y - b;
            l = Math.Clamp(l, 0, w - 1);
            r = Math.Clamp(r, 0, w - 1);
            b = Math.Clamp(b, 0, h - 1);
            t = Math.Clamp(t, 0, h - 1);
            return Arithmetic.Mix(Arithmetic.Mix(bitmap[l, b], bitmap[r, b], lr),
                Arithmetic.Mix(bitmap[l, t], bitmap[r, t], lr), bt);
        }

        private static float DistVal(float dist, double pxRange)
        {
            if (pxRange == 0)
                return dist > .5 ? 1 : 0;
            return (float) Math.Clamp((dist - .5) * pxRange + .5, 0, 1);
        }

        public static void RenderSdf(Bitmap<float> output, Bitmap<float> sdf, double pxRange)
        {
            int w = output.Width, h = output.Height;
            pxRange *= (double) (w + h) / (sdf.Width + sdf.Height);
            for (var y = 0; y < h; ++y)
            for (var x = 0; x < w; ++x)
            {
                var s = Sample(sdf, new Vector2((x + .5) / w, (y + .5) / h));
                output[x, y] = DistVal(s, pxRange);
            }
        }

        public static void RenderSdf(Bitmap<FloatRgb> output, Bitmap<float> sdf, double pxRange)
        {
            int w = output.Width, h = output.Height;
            pxRange *= (double) (w + h) / (sdf.Width + sdf.Height);
            for (var y = 0; y < h; ++y)
            for (var x = 0; x < w; ++x)
            {
                var s = Sample(sdf, new Vector2((x + .5) / w, (y + .5) / h));
                var v = DistVal(s, pxRange);
                output[x, y].R = v;
                output[x, y].G = v;
                output[x, y].B = v;
            }
        }

        public static void RenderSdf(Bitmap<float> output, Bitmap<FloatRgb> sdf, double pxRange)
        {
            int w = output.Width, h = output.Height;
            pxRange *= (double) (w + h) / (sdf.Width + sdf.Height);
            for (var y = 0; y < h; ++y)
            for (var x = 0; x < w; ++x)
            {
                var s = Sample(sdf, new Vector2((x + .5) / w, (y + .5) / h));
                output[x, y] = DistVal(Arithmetic.Median(s.R, s.G, s.B), pxRange);
            }
        }

        public static void RenderSdf(Bitmap<FloatRgb> output, Bitmap<FloatRgb> sdf, double pxRange)
        {
            int w = output.Width, h = output.Height;
            pxRange *= (double) (w + h) / (sdf.Width + sdf.Height);
            for (var y = 0; y < h; ++y)
            for (var x = 0; x < w; ++x)
            {
                var s = Sample(sdf, new Vector2((x + .5) / w, (y + .5) / h));
                output[x, y].R = DistVal(s.R, pxRange);
                output[x, y].G = DistVal(s.G, pxRange);
                output[x, y].B = DistVal(s.B, pxRange);
            }
        }

        public static void Simulate8Bit(Bitmap<float> bitmap)
        {
            int w = bitmap.Width, h = bitmap.Height;
            for (var y = 0; y < h; ++y)
            for (var x = 0; x < w; ++x)
            {
                var v = (byte) Math.Clamp(bitmap[x, y] * 0x100, 0, 0xff);
                bitmap[x, y] = v / 255.0f;
            }
        }

        public static void Simulate8Bit(Bitmap<FloatRgb> bitmap)
        {
            int w = bitmap.Width, h = bitmap.Height;
            for (var y = 0; y < h; ++y)
            for (var x = 0; x < w; ++x)
            {
                var r = (byte) Math.Clamp(bitmap[x, y].R * 0x100, 0, 0xff);
                var g = (byte) Math.Clamp(bitmap[x, y].G * 0x100, 0, 0xff);
                var b = (byte) Math.Clamp(bitmap[x, y].B * 0x100, 0, 0xff);
                bitmap[x, y].R = r / 255.0f;
                bitmap[x, y].G = g / 255.0f;
                bitmap[x, y].B = b / 255.0f;
            }
        }
    }
}