﻿namespace Estreya.BlishHUD.Shared.Utils;

using Blish_HUD;
using Blish_HUD.Controls;
using Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MathHelper = Helpers.MathHelper;

public static class SpriteBatchExtensions
{
    private static readonly Logger Logger = Logger.GetLogger(typeof(SpriteBatchExtensions));

    #region Draw

    public static void Draw(this SpriteBatch spriteBatch, Texture2D texture, RectangleF destinationRectangle)
    {
        Draw(spriteBatch, texture, destinationRectangle, Color.White, 0f);
    }

    public static void Draw(this SpriteBatch spriteBatch, Texture2D texture, RectangleF destinationRectangle, Color tint)
    {
        Draw(spriteBatch, texture, destinationRectangle, tint, 0f);
    }

    public static void Draw(this SpriteBatch spriteBatch, Texture2D texture, RectangleF destinationRectangle, Color tint, float angle)
    {
        DrawOnCtrl(spriteBatch, null, texture, destinationRectangle, tint, angle);
    }

    public static void DrawRectangle(this SpriteBatch spriteBatch, Texture2D baseTexture, RectangleF coords, Color color)
    {
        DrawRectangleOnCtrl(spriteBatch, null, baseTexture, coords, color);
    }

    public static void DrawRectangle(this SpriteBatch spriteBatch, Texture2D baseTexture, RectangleF coords, Color color, int borderSize, Color borderColor)
    {
        DrawRectangleOnCtrl(spriteBatch, null, baseTexture, coords, color);

        if (borderSize > 0 && borderColor != Color.Transparent)
        {
            DrawRectangleOnCtrl(spriteBatch, null, baseTexture, new RectangleF(coords.Left, coords.Top, coords.Width - borderSize, borderSize), borderColor);
            DrawRectangleOnCtrl(spriteBatch, null, baseTexture, new RectangleF(coords.Right - borderSize, coords.Top, borderSize, coords.Height), borderColor);
            DrawRectangleOnCtrl(spriteBatch, null, baseTexture, new RectangleF(coords.Left, coords.Bottom - borderSize, coords.Width, borderSize), borderColor);
            DrawRectangleOnCtrl(spriteBatch, null, baseTexture, new RectangleF(coords.Left, coords.Top, borderSize, coords.Height), borderColor);
        }
    }

    public static void DrawString(this SpriteBatch spriteBatch, string text, SpriteFont font, RectangleF destinationRectangle, Color color, bool wrap = false, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left, VerticalAlignment verticalAlignment = VerticalAlignment.Middle)
    {
        DrawString(spriteBatch, text, font, destinationRectangle, color, wrap, false, 1, horizontalAlignment, verticalAlignment);
    }

    public static void DrawString(this SpriteBatch spriteBatch, string text, SpriteFont font, RectangleF destinationRectangle, Color color, bool wrap, bool stroke, int strokeDistance = 1, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left, VerticalAlignment verticalAlignment = VerticalAlignment.Middle)
    {
        DrawStringOnCtrl(spriteBatch, null, text, font, destinationRectangle, color, wrap, stroke, strokeDistance, horizontalAlignment, verticalAlignment);
    }

    public static void DrawString(this SpriteBatch spriteBatch, string text, SpriteFont font, RectangleF destinationRectangle, Color color, bool wrap, bool stroke, int strokeDistance, Color strokeColor, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left, VerticalAlignment verticalAlignment = VerticalAlignment.Middle)
    {
        DrawStringOnCtrl(spriteBatch, null, text, font, destinationRectangle, color, wrap, stroke, strokeDistance, strokeColor, horizontalAlignment, verticalAlignment);
    }

    public static void DrawString(this SpriteBatch spriteBatch, string text, BitmapFont font, RectangleF destinationRectangle, Color color, bool wrap = false, float scale = 1f, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left, VerticalAlignment verticalAlignment = VerticalAlignment.Middle)
    {
        DrawString(spriteBatch, text, font, destinationRectangle, color, wrap, false, 1,scale, horizontalAlignment, verticalAlignment);
    }

    public static void DrawString(this SpriteBatch spriteBatch, string text, BitmapFont font, RectangleF destinationRectangle, Color color, bool wrap, bool stroke, int strokeDistance = 1, float scale = 1f, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left, VerticalAlignment verticalAlignment = VerticalAlignment.Middle)
    {
        DrawStringOnCtrl(spriteBatch, null, text, font, destinationRectangle, color, wrap, stroke, strokeDistance, scale, horizontalAlignment, verticalAlignment);
    }

    public static void DrawString(this SpriteBatch spriteBatch, string text, BitmapFont font, RectangleF destinationRectangle, Color color, bool wrap, bool stroke, int strokeDistance, Color strokeColor, float scale = 1f, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left, VerticalAlignment verticalAlignment = VerticalAlignment.Middle)
    {
        DrawStringOnCtrl(spriteBatch, null, text, font, destinationRectangle, color, wrap, stroke, strokeDistance, strokeColor, scale, horizontalAlignment, verticalAlignment);
    }

    public static void DrawLine(this SpriteBatch spriteBatch, Texture2D baseTexture, Rectangle coords, Color color)
    {
        DrawLineOnCtrl(spriteBatch, null, baseTexture, coords, color);
    }

    public static void DrawLine(this SpriteBatch spriteBatch, Texture2D baseTexture, RectangleF coords, Color color)
    {
        DrawLineOnCtrl(spriteBatch, null, baseTexture, coords, color);
    }

    public static void DrawCrossOut(this SpriteBatch spriteBatch, Texture2D baseTexture, RectangleF coords, Color color)
    {
        DrawCrossOutOnCtrl(spriteBatch, null, baseTexture, coords, color);
    }

    public static void DrawAngledLine(this SpriteBatch spriteBatch, Texture2D baseTexture, Point2 start, Point2 end, Color color, float thickness = 1)
    {
        DrawAngledLineOnCtrl(spriteBatch, null, baseTexture, start, end, color, thickness);
    }
    #endregion

    #region Draw on Control

    public static void DrawOnCtrl(this SpriteBatch spriteBatch, Control control, Texture2D texture, RectangleF destinationRectangle)
    {
        DrawOnCtrl(spriteBatch, control, texture, destinationRectangle, Color.White * control.AbsoluteOpacity());
    }

    public static void DrawOnCtrl(this SpriteBatch spriteBatch, Control control, Texture2D texture, RectangleF destinationRectangle, Color tint)
    {
        DrawOnCtrl(spriteBatch, control, texture, destinationRectangle, tint, 0f);
    }

    public static void DrawOnCtrl(this SpriteBatch spriteBatch, Control control, Texture2D texture, RectangleF destinationRectangle, Color tint, float angle)
    {
        RectangleF rectangle = control != null ? destinationRectangle.ToBounds(control.AbsoluteBounds) : destinationRectangle;
        // Hacky trick to let us use RectangleF with spritebatch.
        Vector2 scale = new Vector2(rectangle.Width / texture.Width, rectangle.Height / texture.Height);

        spriteBatch.Draw(texture, rectangle.Center - (rectangle.Size / 2f), null, tint, angle, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    public static void DrawRectangleOnCtrl(this SpriteBatch spriteBatch, Control control, Texture2D baseTexture, RectangleF coords, Color color)
    {
        if (control != null)
        {
            spriteBatch.DrawOnCtrl(control, baseTexture, coords, color);
        }
        else
        {
            spriteBatch.Draw(baseTexture, coords, color);
        }
    }

    public static void DrawRectangleOnCtrl(this SpriteBatch spriteBatch, Control control, Texture2D baseTexture, RectangleF coords, Color color, int borderSize, Color borderColor)
    {
        DrawRectangleOnCtrl(spriteBatch, control, baseTexture, coords, color);

        if (borderSize > 0 && borderColor != Color.Transparent)
        {
            DrawRectangleOnCtrl(spriteBatch, control, baseTexture, new RectangleF(coords.Left, coords.Top, coords.Width - borderSize, borderSize), borderColor);
            DrawRectangleOnCtrl(spriteBatch, control, baseTexture, new RectangleF(coords.Right - borderSize, coords.Top, borderSize, coords.Height), borderColor);
            DrawRectangleOnCtrl(spriteBatch, control, baseTexture, new RectangleF(coords.Left, coords.Bottom - borderSize, coords.Width, borderSize), borderColor);
            DrawRectangleOnCtrl(spriteBatch, control, baseTexture, new RectangleF(coords.Left, coords.Top, borderSize, coords.Height), borderColor);
        }
    }

    public static void DrawStringOnCtrl(this SpriteBatch spriteBatch, Control ctrl, string text, SpriteFont font, RectangleF destinationRectangle, Color color, bool wrap = false, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left, VerticalAlignment verticalAlignment = VerticalAlignment.Middle)
    {
        DrawStringOnCtrl(spriteBatch, ctrl, text, font, destinationRectangle, color, wrap, false, 1, horizontalAlignment, verticalAlignment);
    }

    public static void DrawStringOnCtrl(this SpriteBatch spriteBatch, Control ctrl, string text, SpriteFont font, RectangleF destinationRectangle, Color color, bool wrap, bool stroke, int strokeDistance = 1, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left, VerticalAlignment verticalAlignment = VerticalAlignment.Middle)
    {
        DrawStringOnCtrl(spriteBatch, ctrl, text, font, destinationRectangle, color, wrap, stroke, strokeDistance, Color.Black, horizontalAlignment, verticalAlignment);
    }

    private static string WrapTextSegment(SpriteFont spriteFont, string text, float maxLineWidth)
    {
        string[] array = text.Split(' ');
        StringBuilder stringBuilder = new StringBuilder();
        float num = 0f;
        float width = spriteFont.MeasureString(" ").X;
        string[] array2 = array;
        foreach (string text2 in array2)
        {
            Vector2 vector = spriteFont.MeasureString(text2);
            if (num + vector.X < maxLineWidth)
            {
                stringBuilder.Append(text2 + " ");
                num += vector.X + width;
            }
            else
            {
                stringBuilder.Append("\n" + text2 + " ");
                num = vector.X + width;
            }
        }

        return stringBuilder.ToString();
    }

    private static string WrapText(SpriteFont spriteFont, string text, float maxLineWidth)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }

        return string.Join("\n", from s in text.Split('\n')
                                 select WrapTextSegment(spriteFont, s, maxLineWidth));
    }

    public static void DrawStringOnCtrl(this SpriteBatch spriteBatch, Control ctrl, string text, SpriteFont font, RectangleF destinationRectangle, Color color, bool wrap, bool stroke, int strokeDistance, Color strokeColor, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left, VerticalAlignment verticalAlignment = VerticalAlignment.Middle)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        text = wrap ? WrapText(font, text, destinationRectangle.Width) : text;
        if (horizontalAlignment != 0 && (wrap || text.Contains("\n")))
        {
            using StringReader stringReader = new StringReader(text);
            for (int i = 0; destinationRectangle.Height - i > 0; i += font.LineSpacing)
            {
                string text2;
                if ((text2 = stringReader.ReadLine()) == null)
                {
                    break;
                }

                spriteBatch.DrawStringOnCtrl(ctrl, text2, font, destinationRectangle.Add(0, i, 0, 0), color, wrap, stroke, strokeDistance, horizontalAlignment, verticalAlignment);
            }

            return;
        }

        Vector2 vector = font.MeasureString(text);
        destinationRectangle = ctrl != null ? destinationRectangle.ToBounds(ctrl.AbsoluteBounds) : destinationRectangle;
        float num = destinationRectangle.X;
        float num2 = destinationRectangle.Y;
        switch (horizontalAlignment)
        {
            case HorizontalAlignment.Center:
                num += (destinationRectangle.Width / 2) - (vector.X / 2);
                break;
            case HorizontalAlignment.Right:
                num += destinationRectangle.Width - vector.X;
                break;
        }

        switch (verticalAlignment)
        {
            case VerticalAlignment.Middle:
                num2 += (destinationRectangle.Height / 2) - (vector.Y / 2);
                break;
            case VerticalAlignment.Bottom:
                num2 += destinationRectangle.Height - vector.Y;
                break;
        }

        Vector2 vector2 = new Vector2(num, num2);
        float scale = ctrl != null ? ctrl.AbsoluteOpacity() : 1;
        if (stroke)
        {
            spriteBatch.DrawString(font, text, vector2.OffsetBy(0f, -strokeDistance), strokeColor * scale);
            spriteBatch.DrawString(font, text, vector2.OffsetBy(strokeDistance, -strokeDistance), strokeColor * scale);
            spriteBatch.DrawString(font, text, vector2.OffsetBy(strokeDistance, 0f), strokeColor * scale);
            spriteBatch.DrawString(font, text, vector2.OffsetBy(strokeDistance, strokeDistance), strokeColor * scale);
            spriteBatch.DrawString(font, text, vector2.OffsetBy(0f, strokeDistance), strokeColor * scale);
            spriteBatch.DrawString(font, text, vector2.OffsetBy(-strokeDistance, strokeDistance), strokeColor * scale);
            spriteBatch.DrawString(font, text, vector2.OffsetBy(-strokeDistance, 0f), strokeColor * scale);
            spriteBatch.DrawString(font, text, vector2.OffsetBy(-strokeDistance, -strokeDistance), strokeColor * scale);
        }

        spriteBatch.DrawString(font, text, vector2, color * scale);
    }

    public static void DrawStringOnCtrl(this SpriteBatch spriteBatch, Control ctrl, string text, BitmapFont font, RectangleF destinationRectangle, Color color, bool wrap = false, float scale = 1f, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left, VerticalAlignment verticalAlignment = VerticalAlignment.Middle)
    {
        DrawStringOnCtrl(spriteBatch, ctrl, text, font, destinationRectangle, color, wrap, false, 1, scale, horizontalAlignment, verticalAlignment);
    }

    public static void DrawStringOnCtrl(this SpriteBatch spriteBatch, Control ctrl, string text, BitmapFont font, RectangleF destinationRectangle, Color color, bool wrap, bool stroke, int strokeDistance = 1, float scale = 1f, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left, VerticalAlignment verticalAlignment = VerticalAlignment.Middle)
    {
        DrawStringOnCtrl(spriteBatch, ctrl, text, font, destinationRectangle, color, wrap, stroke, strokeDistance, Color.Black, scale, horizontalAlignment, verticalAlignment);
    }

    private static string WrapTextSegment(BitmapFont spriteFont, string text, float maxLineWidth)
    {
        string[] array = text.Split(' ');
        StringBuilder stringBuilder = new StringBuilder();
        float num = 0f;
        float width = spriteFont.MeasureString(" ").Width;
        string[] array2 = array;
        foreach (string text2 in array2)
        {
            Vector2 vector = spriteFont.MeasureString(text2);
            if (num + vector.X < maxLineWidth)
            {
                stringBuilder.Append(text2 + " ");
                num += vector.X + width;
            }
            else
            {
                stringBuilder.Append("\n" + text2 + " ");
                num = vector.X + width;
            }
        }

        return stringBuilder.ToString();
    }

    private static string WrapText(BitmapFont spriteFont, string text, float maxLineWidth)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }

        return string.Join("\n", from s in text.Split('\n')
                                 select WrapTextSegment(spriteFont, s, maxLineWidth));
    }

    public static void DrawStringOnCtrl(this SpriteBatch spriteBatch, Control ctrl, string text, BitmapFont font, RectangleF destinationRectangle, Color color, bool wrap, bool stroke, int strokeDistance, Color strokeColor, float scale = 1f, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left, VerticalAlignment verticalAlignment = VerticalAlignment.Middle)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        text = wrap ? WrapText(font, text, destinationRectangle.Width) : text;
        if (horizontalAlignment != 0 && (wrap || text.Contains("\n")))
        {
            using StringReader stringReader = new StringReader(text);
            for (int i = 0; destinationRectangle.Height - i > 0; i += font.LineHeight)
            {
                string text2;
                if ((text2 = stringReader.ReadLine()) == null)
                {
                    break;
                }

                spriteBatch.DrawStringOnCtrl(ctrl, text2, font, destinationRectangle.Add(0, i, 0, 0), color, wrap, stroke, strokeDistance, scale, horizontalAlignment, verticalAlignment);
            }

            return;
        }

        Vector2 vector = font.MeasureString(text);
        destinationRectangle = ctrl != null ? destinationRectangle.ToBounds(ctrl.AbsoluteBounds) : destinationRectangle;
        float num = destinationRectangle.X;
        float num2 = destinationRectangle.Y;
        switch (horizontalAlignment)
        {
            case HorizontalAlignment.Center:
                num += (destinationRectangle.Width / 2) - (vector.X / 2);
                break;
            case HorizontalAlignment.Right:
                num += destinationRectangle.Width - vector.X;
                break;
        }

        switch (verticalAlignment)
        {
            case VerticalAlignment.Middle:
                num2 += (destinationRectangle.Height / 2) - (vector.Y / 2);
                break;
            case VerticalAlignment.Bottom:
                num2 += destinationRectangle.Height - vector.Y;
                break;
        }

        Vector2 vector2 = new Vector2(num, num2);
        float opacity = ctrl != null ? ctrl.AbsoluteOpacity() : 1;
        float rotation = 0f;
        Vector2 origin = Vector2.Zero;

        if (stroke)
        {
            spriteBatch.DrawString(font, text, vector2.OffsetBy(0f, -strokeDistance), strokeColor * opacity, rotation, origin, scale, SpriteEffects.None, 1f);
            spriteBatch.DrawString(font, text, vector2.OffsetBy(strokeDistance, -strokeDistance), strokeColor * opacity, rotation, origin,scale, SpriteEffects.None, 1f);
            spriteBatch.DrawString(font, text, vector2.OffsetBy(strokeDistance, 0f), strokeColor * opacity, rotation, origin, scale, SpriteEffects.None, 1f);
            spriteBatch.DrawString(font, text, vector2.OffsetBy(strokeDistance, strokeDistance), strokeColor * opacity, rotation, origin, scale, SpriteEffects.None, 1f);
            spriteBatch.DrawString(font, text, vector2.OffsetBy(0f, strokeDistance), strokeColor * opacity, rotation, origin, scale, SpriteEffects.None, 1f);
            spriteBatch.DrawString(font, text, vector2.OffsetBy(-strokeDistance, strokeDistance), strokeColor * opacity, rotation, origin, scale, SpriteEffects.None, 1f);
            spriteBatch.DrawString(font, text, vector2.OffsetBy(-strokeDistance, 0f), strokeColor * opacity, rotation, origin, scale, SpriteEffects.None, 1f);
            spriteBatch.DrawString(font, text, vector2.OffsetBy(-strokeDistance, -strokeDistance), strokeColor * opacity, rotation, origin, scale, SpriteEffects.None, 1f);
        }

        spriteBatch.DrawString(font, text, vector2, color * opacity, rotation, origin, scale, SpriteEffects.None, 1f);
    }

    public static void DrawLineOnCtrl(this SpriteBatch spriteBatch, Control control, Texture2D baseTexture, Rectangle coords, Color color)
    {
        if (control != null)
        {
            DrawOnCtrl(spriteBatch, control, baseTexture, coords, color);
        }
        else
        {
            Draw(spriteBatch, baseTexture, coords, color);
        }
    }

    public static void DrawLineOnCtrl(this SpriteBatch spriteBatch, Control control, Texture2D baseTexture, RectangleF coords, Color color)
    {
        if (control != null)
        {
            DrawOnCtrl(spriteBatch, control, baseTexture, coords, color);
        }
        else
        {
            Draw(spriteBatch, baseTexture, coords, color);
        }
    }

    public static void DrawCrossOutOnCtrl(this SpriteBatch spriteBatch, Control control, Texture2D baseTexture, RectangleF coords, Color color)
    {
        Point2 topLeft = new Point2(coords.Left, coords.Top);
        Point2 topRight = new Point2(coords.Right, coords.Top);
        Point2 bottomLeft = new Point2(coords.Left, coords.Bottom - 1.5f);
        Point2 bottomRight = new Point2(coords.Right, coords.Bottom - 1.5f);

        DrawAngledLineOnCtrl(spriteBatch, control, baseTexture, topLeft, bottomRight, color);
        DrawAngledLineOnCtrl(spriteBatch, control, baseTexture, bottomLeft, topRight, color);
    }

    public static void DrawAngledLineOnCtrl(this SpriteBatch spriteBatch, Control control, Texture2D baseTexture, Point2 start, Point2 end, Color color, float thickness = 1)
    {
        float length = MathHelper.CalculateDistance(start, end);
        RectangleF lineRectangle = new RectangleF(start.X, start.Y, length, thickness);
        float angle = MathHelper.CalculateAngle(start, end);

        if (control != null)
        {
            spriteBatch.DrawOnCtrl(control, baseTexture, lineRectangle, color, angle);
        }
        else
        {
            spriteBatch.Draw(baseTexture, lineRectangle, color, angle);
        }
    }

    #endregion
}