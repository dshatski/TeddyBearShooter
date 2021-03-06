﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace OptionalProject
{
    /// <summary>
    /// A class for a projectile
    /// </summary>
    public class Projectile
    {
        #region Fields

        bool active = true;
        ProjectileType type;

        // drawing support
        Texture2D sprite;
        Rectangle drawRectangle;

        // velocity information
        float yVelocity;

        #endregion

        #region Constructors

        /// <summary>
        ///  Constructs a projectile with the given y velocity
        /// </summary>
        /// <param name="type">the projectile type</param>
        /// <param name="sprite">the sprite for the projectile</param>
        /// <param name="x">the x location of the center of the projectile</param>
        /// <param name="y">the y location of the center of the projectile</param>
        /// <param name="yVelocity">the y velocity for the projectile</param>
        public Projectile(ProjectileType type, Texture2D sprite, int x, int y, 
            float yVelocity)
        {
            this.type = type;
            this.sprite = sprite;
            this.yVelocity = yVelocity;
            drawRectangle = new Rectangle(x - sprite.Width / 2, 
                y - sprite.Height / 2, sprite.Width,
                sprite.Height);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets and sets whether or not the projectile is active
        /// </summary>
        public bool IsActive
        {
            get { return active; }
            set { active = value; }
        }

        /// <summary>
        /// Gets the projectile type
        /// </summary>
        public ProjectileType Type
        {
            get { return type; }
        }

        /// <summary>
        /// Gets the collision rectangle for the projectile
        /// </summary>
        public Rectangle CollisionRectangle
        {
            get { return drawRectangle; }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Updates the projectile's location and makes inactive when it
        /// leaves the game window
        /// </summary>
        public void Update(GameTime gameTime)
        {
            // move the projectile based on its type, velocity and the elapsed game time in milliseconds
            if(type==ProjectileType.FrenchFries)
                drawRectangle.Y -= (int)(gameTime.ElapsedGameTime.Milliseconds * yVelocity);
            else
                drawRectangle.Y += (int)(gameTime.ElapsedGameTime.Milliseconds * yVelocity);

            // check for outside game window (some projectiles go up and other go down)
            if (drawRectangle.Y < 0 || drawRectangle.Bottom >= GameConstants.WINDOW_HEIGHT)
                active = false;
        }

        /// <summary>
        /// Draws the projectile
        /// </summary>
        /// <param name="spriteBatch">the sprite batch to use</param>
        public void Draw(SpriteBatch spriteBatch)
        {
            // have the projectile draw itself
            spriteBatch.Draw(sprite, drawRectangle, Color.White);
        }

        #endregion
    }
}