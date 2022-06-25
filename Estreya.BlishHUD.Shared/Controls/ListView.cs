namespace Estreya.BlishHUD.Shared.Controls;

using Blish_HUD;
using Blish_HUD.Controls;
using Estreya.BlishHUD.Shared.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System.Linq;

public class ListView<T> : FlowPanel
{
    public ListView()
    {
        this.FlowDirection = ControlFlowDirection.SingleTopToBottom;
        this.HeightSizingMode = SizingMode.Fill;
        this.WidthSizingMode = SizingMode.Fill;
        this.CanScroll = true;

        GameService.Input.Mouse.LeftMouseButtonReleased += this.Mouse_LeftMouseButtonReleased;
    }

    private void Mouse_LeftMouseButtonReleased(object sender, Blish_HUD.Input.MouseEventArgs e)
    {
        /*Task.Run(async () =>
        {
            // Run in 1 Seconds.
            await Task.Delay(1000);

            this.Children.Where(child =>
            {
                return child is ListEntry entry && entry.Dragging;
            }).ToList().ForEach(child =>
            {
                if (child is ListEntry entry)
                {
                    entry.Dragging = false;
                }
            });
        });
        */
    }

    protected override void OnChildAdded(ChildChangedEventArgs e)
    {
        if (e.ChangedChild is not ListEntry<T>)
        {
            e.Cancel = true;
            return;
        }

        e.ChangedChild.LeftMouseButtonPressed += this.ChangedChild_LeftMouseButtonPressed;
        e.ChangedChild.LeftMouseButtonReleased += this.ChangedChild_LeftMouseButtonReleased;

        base.OnChildAdded(e);
    }

    protected override void OnChildRemoved(ChildChangedEventArgs e)
    {
        e.ChangedChild.LeftMouseButtonPressed -= this.ChangedChild_LeftMouseButtonPressed;
        e.ChangedChild.LeftMouseButtonReleased -= this.ChangedChild_LeftMouseButtonReleased;

        base.OnChildRemoved(e);
    }

    private void ChangedChild_LeftMouseButtonReleased(object sender, Blish_HUD.Input.MouseEventArgs e)
    {
        System.Collections.Generic.List<Control> draggingEntries = this.Children.Where(child =>
        {
            return child is ListEntry<T> entry && entry.Dragging;
        }).ToList();

        if (sender is ListEntry<T> draggedOnEntry)
        {
            int newIndex = this.GetDragIndex(draggedOnEntry);
            if (newIndex > this.Children.Count)
            {
                newIndex = this.Children.Count - 1;
            }

            draggingEntries.ForEach(draggingEntry =>
            {
                int oldIndex = this.Children.ToList().IndexOf(draggingEntry);
                if (this.Children.Remove(draggingEntry))
                {
                    if (newIndex > oldIndex)
                    {
                        newIndex--;
                    }
                }

                this.Children.Insert(newIndex, draggingEntry);
            });

            this.Invalidate();
        }

        draggingEntries.ForEach(child =>
        {
            if (child is ListEntry<T> entry)
            {
                entry.Dragging = false;
            }
        });
    }

    private void ChangedChild_LeftMouseButtonPressed(object sender, Blish_HUD.Input.MouseEventArgs e)
    {
        if (sender is not ListEntry<T> entry)
        {
            return;
        }

        if (entry.DragDrop)
        {
            entry.Dragging = true;
        }
    }

    private int GetDragIndex(ListEntry<T> entry)
    {
        bool upperHalf = this.RelativeMousePosition.Y + this.VerticalScrollOffset < entry.Location.Y + (entry.Size.Y / 2);

        int draggedOnIndex = this.Children.ToList().IndexOf(entry);

        if (draggedOnIndex == -1)
        {
            return this.Children.Count - 1;
        }

        if (draggedOnIndex == 0 || upperHalf)
        {
            return draggedOnIndex;
        }

        return draggedOnIndex + 1;
    }

    private int GetCurrentDragOverIndex()
    {
        System.Collections.Generic.List<Control> currentHoveredEntries = this.Children.Where(child =>
        {
            bool hovered = true;

            hovered &= this.RelativeMousePosition.X + this.HorizontalScrollOffset >= child.Left;
            hovered &= this.RelativeMousePosition.X + this.HorizontalScrollOffset < child.Right;

            hovered &= this.RelativeMousePosition.Y + this.VerticalScrollOffset >= child.Top;
            hovered &= this.RelativeMousePosition.Y + this.VerticalScrollOffset < child.Bottom;

            return hovered;
        }).ToList();

        if (currentHoveredEntries.Count == 0)
        {
            return -1;
        }

        ListEntry<T> entry = currentHoveredEntries.First() as ListEntry<T>;

        return this.GetDragIndex(entry);
    }

    public override void PaintAfterChildren(SpriteBatch spriteBatch, Rectangle bounds)
    {
        System.Collections.Generic.List<Control> draggingEntries = this.Children.Where(child =>
        {
            return child is ListEntry<T> entry && entry.Dragging;
        }).ToList();

        bool anyDragging = draggingEntries.Count > 0;

        if (!anyDragging)
        {
            return;
        }

        ListEntry<T> entry = draggingEntries.First() as ListEntry<T>;

        RectangleF nameRectangle = new RectangleF(this.RelativeMousePosition.X, this.RelativeMousePosition.Y - 20, entry.Width, entry.Height);

        spriteBatch.DrawStringOnCtrl(this, entry.Text, entry.Font, nameRectangle, entry.TextColor);

        int draggedOnIndex = this.GetCurrentDragOverIndex();
        if (draggedOnIndex == -1)
        {
            return;
        }

        bool addedLast = draggedOnIndex == this.Children.Count;

        ListEntry<T> draggedOnEntry = this.Children[addedLast ?  draggedOnIndex - 1 : draggedOnIndex] as ListEntry<T>;

        RectangleF lineRectangle = new RectangleF(draggedOnEntry.Left, (addedLast ? draggedOnEntry.Bottom : draggedOnEntry.Top) - this.VerticalScrollOffset, draggedOnEntry.Width, 2);

        spriteBatch.DrawLineOnCtrl(this, ContentService.Textures.Pixel, lineRectangle, Color.White);
    }
}
