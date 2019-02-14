namespace Msdfgen
{
    public struct FloatRgb {
        public float R, G, B;
    };
    
    public class Bitmap<T> where T:struct
    {
        public Bitmap()
        {
        }

        public Bitmap(int width, int height)
        {
            Width = width;
            Height = height;
            _content = new T[width, height];
        }

        public ref T this[int x, int y] => ref _content[x, y];

        public int Width { get; }

        public int Height { get; }

        private readonly T[,] _content;
    }
   
}