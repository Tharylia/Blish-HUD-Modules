namespace Estreya.BlishHUD.Shared.Extensions
{
    using MonoGame.Extended;

    public static class RectangleExtension
    {
        public static RectangleF Add(this RectangleF u1, float x, float y, float width, float height)
        {
            return new RectangleF(u1.X + x, u1.Y + y, u1.Width + width, u1.Height + height);
        }
        public static RectangleF ToBounds(this RectangleF r1, RectangleF bounds)
        {
            Point2 point = new Point2(r1.X + bounds.X, r1.Y + bounds.Y);
            return new RectangleF(point, r1.Size);
        }
    }
}
