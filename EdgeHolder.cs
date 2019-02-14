namespace Msdfgen
{
    /// Container for a single edge of dynamic type.
    public class EdgeHolder
    {
        public EdgeHolder()
        {
        }

        public EdgeHolder(EdgeSegment segment)
        {
            Segment = segment;
        }

        public EdgeHolder(Vector2 p0, Vector2 p1, EdgeColor edgeColor = EdgeColor.White)
        {
            Segment = new LinearSegment(p0, p1, edgeColor);
        }

        public EdgeHolder(Vector2 p0, Vector2 p1, Vector2 p2, EdgeColor edgeColor = EdgeColor.White)
        {
            Segment = new QuadraticSegment(p0, p1, p2, edgeColor);
        }

        public EdgeHolder(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, EdgeColor edgeColor = EdgeColor.White)
        {
            Segment = new CubicSegment(p0, p1, p2, p3, edgeColor);
        }

        public EdgeSegment Segment { get; }
    };
}
