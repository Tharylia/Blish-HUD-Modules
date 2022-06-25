namespace Estreya.BlishHUD.EventTable.Input
{
    using Blish_HUD.Input;
    using Microsoft.Xna.Framework;

    public class MouseEventArgs
    {
        public Point Position { get; private set; }

        public bool DoubleClick { get; private set; }

        public MouseEventType EventType { get; private set; }

        public MouseEventArgs(Point position, bool doubleClick, MouseEventType type)
        {
            this.Position = position;
            this.DoubleClick = doubleClick;
            this.EventType = type;
        }
    }
}
