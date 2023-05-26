namespace Estreya.BlishHUD.Shared.Models;

using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System;

public interface IRenderable : IDisposable
{
    /// <summary>
    ///     Renders the "control" on the <paramref name="spriteBatch" /> with the specified <paramref name="bounds" />.
    /// </summary>
    /// <param name="spriteBatch">The spritebatch to render onto.</param>
    /// <param name="bounds">The position and size the render should use.</param>
    /// <returns>The actual rendered bounds.</returns>
    RectangleF Render(SpriteBatch spriteBatch, RectangleF bounds);
}