using System;

namespace Msdfgen
{
    /// Edge color specifies which color channels an edge belongs to.
    [Flags]
    public enum EdgeColor
    {
        Black = 0,
        Red = 1,
        Green = 2,
        Yellow = 3,
        Blue = 4,
        Magenta = 5,
        Cyan = 6,
        White = 7
    }
}