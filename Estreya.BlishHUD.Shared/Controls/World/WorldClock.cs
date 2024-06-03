namespace Estreya.BlishHUD.Shared.Controls.World
{
    using Blish_HUD.Entities;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using System;
    using Extensions;
    using System.Collections.Generic;
    using System.Text;
    using Blish_HUD;
    using Gw2Sharp.WebApi.V2.Models;

    public class WorldClock : WorldEntity
    {
        private readonly float _radius;
        private readonly Func<DateTime> _getNow;
        private WorldCircle _circlePolygone;
        private WorldPolygone _hourPolygone;
        private WorldPolygone _minutePolygone;
        private WorldPolygone _secondPolygone;

        private List<WorldPolygone> _minuteMarkPolygones;

        public WorldClock(Vector3 position, float radius, Func<DateTime> getNow) : base(position, 1f)
        {
            this._radius = radius;
            this._getNow = getNow;

            this.CreatePolygones();
        }

        private void CreatePolygones()
        {
            this._circlePolygone = new WorldCircle(this.Position, this._radius, 50);
            this._hourPolygone = new WorldPolygone(this.Position, new Vector3[] { Vector3.Zero, Vector3.Zero });
            this._minutePolygone = new WorldPolygone(this.Position, new Vector3[] { Vector3.Zero, Vector3.Zero });
            this._secondPolygone = new WorldPolygone(this.Position, new Vector3[] { Vector3.Zero, Vector3.Zero });

            this._minuteMarkPolygones = this.CreateMinuteMarkPolygones();
        }

        private List<WorldPolygone> CreateMinuteMarkPolygones()
        {
            var polygones = new List<WorldPolygone>();

            var marks = 12;
            for (int i = 0; i < marks; i++)
            {
                var degreeMinuteMark = i.Remap(0, marks, 0, 360);
                var angleMinuteMark = Math.PI * degreeMinuteMark / 180.0;
                var angleMinuteMarkOuterX = this._radius * (float)Math.Cos(angleMinuteMark);
                var angleMinuteMarkOuterY = this._radius * (float)Math.Sin(angleMinuteMark);
                var angleMinuteMarkInnerX = this._radius * 0.95f * (float)Math.Cos(angleMinuteMark);
                var angleMinuteMarkInnerY = this._radius * 0.95f * (float)Math.Sin(angleMinuteMark);

                polygones.Add(new WorldPolygone(this.Position, new Vector3[]
                {
                    new Vector3(angleMinuteMarkOuterX, angleMinuteMarkOuterY, 0),
                    new Vector3(angleMinuteMarkInnerX, angleMinuteMarkInnerY, 0),
                })
                {
                    ScaleX = this.ScaleX,
                    ScaleY = this.ScaleY,
                    ScaleZ = this.ScaleZ,
                    RotationX = this.RotationX,
                    RotationY = this.RotationY,
                    RotationZ = this.RotationZ
                });
            }

            return polygones;
        }

        public override bool IsPlayerInside(bool includeZAxis = true) => false;

        protected override void InternalRender(GraphicsDevice graphicsDevice, IWorld world, ICamera camera)
        {
            var now = this._getNow();

            this.ApplyProperties(this._circlePolygone);

            var degreeHour = now.Hour.Remap(0, 60, 0, 360) * -1;
            var angleHour = Math.PI * (degreeHour - 90f) / 180.0;
            var angleHourX = this._radius * 0.5f * (float)Math.Cos(angleHour);
            var angleHourY = this._radius * 0.5f * (float)Math.Sin(angleHour);

            this._hourPolygone.Points = new Vector3[]
            {
                Vector3.Zero,
                new Vector3(angleHourX, angleHourY, 0),
            };
            this.ApplyProperties(this._hourPolygone);

            var degreeMinute = now.Minute.Remap(0, 60, 0, 360) * -1;
            var angleMinute = Math.PI * (degreeMinute - 90f) / 180.0;
            var angleMinuteX = this._radius * 0.75f * (float)Math.Cos(angleMinute);
            var angleMinuteY = this._radius * 0.75f * (float)Math.Sin(angleMinute);

            this._minutePolygone.Points = new Vector3[]
            {
                Vector3.Zero,
                new Vector3(angleMinuteX, angleMinuteY, 0),
            };
            this.ApplyProperties(this._minutePolygone);

            var degreeSecond = now.Second.Remap(0, 60, 0, 360) * -1;
            var angleSecond = Math.PI * (degreeSecond - 90f) / 180.0;
            var angleSecondX = this._radius * (float)Math.Cos(angleSecond);
            var angleSecondY = this._radius * (float)Math.Sin(angleSecond);

            this._secondPolygone.Points = new Vector3[]
            {
                Vector3.Zero,
                new Vector3(angleSecondX, angleSecondY, 0),
            };
            this.ApplyProperties(this._secondPolygone);

            this._circlePolygone.Render(graphicsDevice, world, camera);
            _hourPolygone.Render(graphicsDevice, world, camera);
            _minutePolygone.Render(graphicsDevice, world, camera);
            _secondPolygone.Render(graphicsDevice, world, camera);

            foreach (var minuteMark in _minuteMarkPolygones)
            {
                this.ApplyProperties(minuteMark);
                minuteMark.Render(graphicsDevice, world, camera);
            }
        }

        private void ApplyProperties(WorldEntity entity)
        {
            entity.ScaleX = this.ScaleX;
            entity.ScaleY = this.ScaleY;
            entity.ScaleZ = this.ScaleZ;
            entity.RotationX = this.RotationX;
            entity.RotationY = this.RotationY;
            entity.RotationZ = this.RotationZ;
        }
    }
}
