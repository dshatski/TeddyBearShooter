using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace OptionalProject
{
    /// <summary>
    /// A class for a teddy bear
    /// </summary>
    public class TeddyBear
    {
        #region Fields

        bool active = true;

        // drawing support
        Texture2D sprite;
        Rectangle drawRectangle;

        // velocity information
        Vector2 velocity = new Vector2(0, 0);

        // shooting support
        int elapsedShotTime = 0;
        int firingDelay;

        #endregion

        #region Constructors

        /// <summary>
        ///  Constructs a teddy bear centered on the given x and y with the
        ///  given velocity
        /// </summary>
        /// <param name="contentManager">the content manager for loading content</param>
        /// <param name="spriteName">the name of the sprite for the teddy bear</param>
        /// <param name="x">the x location of the center of the teddy bear</param>
        /// <param name="y">the y location of the center of the teddy bear</param>
        public TeddyBear(ContentManager contentManager, string spriteName, int x, int y,
            Vector2 velocity)
        {
            LoadContent(contentManager,spriteName, x, y);
            this.velocity = velocity;
            firingDelay = GetRandomFiringDelay();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets and sets whether or not the teddy bear is active
        /// </summary>
        public bool IsActive
        {
            get { return active; }
            set { active = value; }
        }

        /// <summary>
        /// Gets the location of the teddy bear
        /// </summary>
        public Point Location
        {
            get { return drawRectangle.Center; }
        }

        /// <summary>
        /// Gets the collision rectangle for the teddy bear
        /// </summary>
        public Rectangle CollisionRectangle
        {
            get { return drawRectangle; }
        }

        /// <summary>
        /// Gets and sets the velocity of the teddy bear
        /// </summary>
        public Vector2 Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }

        /// <summary>
        /// Gets and sets the draw rectangle for the teddy bear
        /// </summary>
        public Rectangle DrawRectangle
        {
            get { return drawRectangle; }
            set { drawRectangle = value; }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Updates the teddy bear's location, bouncing if necessary. Also has
        /// the teddy bear fire a projectile when it's time to
        /// </summary>
        /// <param name="gameTime">game time</param>
        /// <param name="soundBank">the sound bank</param>
        public void Update(GameTime gameTime, SoundBank soundBank)
        {
            // move the teddy bear
            drawRectangle.X += (int)(gameTime.ElapsedGameTime.Milliseconds*velocity.X);
            drawRectangle.Y += (int)(gameTime.ElapsedGameTime.Milliseconds*velocity.Y);

            // bounce as necessary
            BounceTopBottom(soundBank);
            BounceLeftRight(soundBank);

            // fire projectile as appropriate
            elapsedShotTime += gameTime.ElapsedGameTime.Milliseconds;

            Projectile projectile;

            if (elapsedShotTime > firingDelay) // time for the teddy bear to shoot
            {
                firingDelay = GetRandomFiringDelay();
                elapsedShotTime = 0;

                projectile = new Projectile(ProjectileType.TeddyBear, Game1.GetProjectileSprite(ProjectileType.TeddyBear),
                    drawRectangle.X, drawRectangle.Bottom + GameConstants.TEDDY_BEAR_PROJECTILE_OFFSET, GetProjectileYVelocity());
                soundBank.PlayCue("TeddyShot");
                Game1.AddProjectile(projectile);
            }

        }

        /// <summary>
        /// Draws the teddy bear
        /// </summary>
        /// <param name="spriteBatch">the sprite batch to use</param>
        public void Draw(SpriteBatch spriteBatch)
        {
            // have the teddy bear draw itself
            spriteBatch.Draw(sprite, drawRectangle, Color.White);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Loads the content for the teddy bear
        /// </summary>
        /// <param name="contentManager">the content manager to use</param>
        /// <param name="spriteName">the name of the sprite for the teddy bear</param>
        /// <param name="x">the x location of the center of the teddy bear</param>
        /// <param name="y">the y location of the center of the teddy bear</param>
        private void LoadContent(ContentManager contentManager, string spriteName, 
            int x, int y)
        {
            // load content and set remainder of draw rectangle
            sprite = contentManager.Load<Texture2D>(spriteName);
            drawRectangle = new Rectangle(x - sprite.Width / 2, 
                y - sprite.Height / 2, sprite.Width,
                sprite.Height);
        }

        /// <summary>
        /// Bounces the teddy bear off the top and bottom window borders if necessary
        /// </summary>
        /// <param name="soundBank">the sound bank</param>
        private void BounceTopBottom(SoundBank soundBank)
        {
            if (drawRectangle.Y < 0)
            {
                // bounce off top
                soundBank.PlayCue("TeddyBounce");
                drawRectangle.Y = 0;
                velocity.Y *= -1;
            }
            else if ((drawRectangle.Y + drawRectangle.Height) > GameConstants.WINDOW_HEIGHT)
            {
                // bounce off bottom
                soundBank.PlayCue("TeddyBounce");
                drawRectangle.Y = GameConstants.WINDOW_HEIGHT - drawRectangle.Height;
                velocity.Y *= -1;
            }
        }
        /// <summary>
        /// Bounces the teddy bear off the left and right window borders if necessary
        /// </summary>
        /// <param name="soundBank">the sound bank</param>
        private void BounceLeftRight(SoundBank soundBank)
        {
            if (drawRectangle.X < 0)
            {
                // bounce off left
                soundBank.PlayCue("TeddyBounce");
                drawRectangle.X = 0;
                velocity.X *= -1;
            }
            else if ((drawRectangle.X + drawRectangle.Width) > GameConstants.WINDOW_WIDTH)
            {
                // bounce off right
                soundBank.PlayCue("TeddyBounce");
                drawRectangle.X = GameConstants.WINDOW_WIDTH - drawRectangle.Width;
                velocity.X *= -1;
            }
        }

        /// <summary>
        /// Gets a random firing delay between MIN_FIRING_DELAY and
        /// MIN_FIRING_DELY + FIRING_RATE_RANGE
        /// </summary>
        /// <returns>the random firing delay</returns>
        private int GetRandomFiringDelay()
        {
            return GameConstants.BEAR_MIN_FIRING_DELAY + 
                RandomNumberGenerator.Next(GameConstants.BEAR_FIRING_RATE_RANGE);
        }

        /// <summary>
        /// Gets the y velocity for the projectile being fired
        /// </summary>
        /// <returns>the projectile y velocity</returns>
        private float GetProjectileYVelocity()
        {
            if (velocity.Y > 0)
            {
                return velocity.Y + GameConstants.TEDDY_BEAR_PROJECTILE_SPEED;
            }
            else
            {
                return GameConstants.TEDDY_BEAR_PROJECTILE_SPEED;
            }
        }

        #endregion
    }
}
