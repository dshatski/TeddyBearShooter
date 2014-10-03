using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using Collisions;

namespace OptionalProject
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        // game objects. Using inheritance would make this
        // easier, but inheritance isn't a GDD 1200 topic
        Burger burger;
        List<TeddyBear> bears = new List<TeddyBear>();
        static List<Projectile> projectiles = new List<Projectile>();
        List<Explosion> explosions = new List<Explosion>();

        // projectile and explosion sprites. Saved so they don't have to
        // be loaded every time projectiles or explosions are created
        static Texture2D frenchFriesSprite;
        static Texture2D teddyBearProjectileSprite;
        static Texture2D explosionSpriteStrip;

        // spawn location support
        const int SPAWN_BORDER_SIZE = 100;

        // scoring support
        int score = 0;
        string scoreString = GameConstants.SCORE_PREFIX + 0;
        SpriteFont font;

        // health support
        string healthString = GameConstants.HEALTH_PREFIX + 
            GameConstants.BURGER_INITIAL_HEALTH;
        bool burgerDead = false;

        // audio components
        AudioEngine audioEngine;
        WaveBank waveBank;
        SoundBank soundBank;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            graphics.PreferredBackBufferWidth = GameConstants.WINDOW_WIDTH;
            graphics.PreferredBackBufferHeight = GameConstants.WINDOW_HEIGHT;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            RandomNumberGenerator.Initialize();

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // load audio content
            audioEngine = new AudioEngine(@"Content\GameAudio.xgs");
            waveBank = new WaveBank(audioEngine, @"Content\Wave Bank.xwb");
            soundBank = new SoundBank(audioEngine, @"Content\Sound Bank.xsb");

            // load sprite font
            font = Content.Load<SpriteFont>("Arial20");

            // load projectile and explosion sprites
            teddyBearProjectileSprite = Content.Load<Texture2D>("teddybearprojectile");
            frenchFriesSprite = Content.Load<Texture2D>("frenchfries");
            explosionSpriteStrip = Content.Load<Texture2D>("explosion");
            
            // add initial game objects
            // assign the burger field to a newly-constructed Burger object
            burger = new Burger(Content, "burger", GameConstants.WINDOW_WIDTH/2, GameConstants.WINDOW_HEIGHT-10);

            // spawn multiple teddy bears
            for(int i=0;i<GameConstants.MAX_BEARS;i++)
                SpawnBear();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            healthString = GameConstants.HEALTH_PREFIX + burger.Health;

            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                this.Exit();

            // update burger
            burger.Update(gameTime, Keyboard.GetState(PlayerIndex.One), soundBank);

            // update other game objects
            foreach (TeddyBear bear in bears)
            {
                bear.Update(gameTime, soundBank);
            }
            foreach (Projectile projectile in projectiles)
            {
                projectile.Update(gameTime);
            }
            foreach (Explosion explosion in explosions)
            {
                explosion.Update(gameTime);
            }

            // check and resolve collisions between teddy bears
            foreach(TeddyBear first in bears)
                foreach(TeddyBear second in bears)
                    if (first != second)
                    {
                        CollisionResolutionInfo collisionResult = CollisionUtils.CheckCollision(1000,
                            GameConstants.WINDOW_WIDTH, GameConstants.WINDOW_HEIGHT, first.Velocity, first.DrawRectangle,
                            second.Velocity, second.DrawRectangle);
                        if (collisionResult != null) // there's a collision to be resolved
                        {
                            soundBank.PlayCue("TeddyBounce");
                            if (collisionResult.FirstOutOfBounds) // the first teddy bear ended up at least partially outside the game window
                                first.IsActive = false;
                            else
                            {
                                first.Velocity = collisionResult.FirstVelocity;
                                first.DrawRectangle = collisionResult.FirstDrawRectangle;
                            }
                            if (collisionResult.SecondOutOfBounds) // the second teddy bear ended up at least partially outside the game window
                                second.IsActive = false;
                            else
                            {
                                second.Velocity = collisionResult.SecondVelocity;
                                second.DrawRectangle = collisionResult.SecondDrawRectangle;
                            }
                        }
                    }

            // check and resolve collisions between burger and teddy bears
            foreach (TeddyBear bear in bears)
                if(bear.CollisionRectangle.Intersects(burger.CollisionRectangle))
                {
                    if (!burgerDead)
                        soundBank.PlayCue("BurgerDamage");
                    burger.Health -= GameConstants.BEAR_DAMAGE;
                    CheckBurgerKill();
                    bear.IsActive = false;
                    explosions.Add(new Explosion(explosionSpriteStrip, bear.Location.X, bear.Location.Y));
                    soundBank.PlayCue("Explosion");
                }

            // check and resolve collisions between burger and projectiles
            foreach (Projectile projectile in projectiles)
                if (projectile.CollisionRectangle.Intersects(burger.CollisionRectangle))
                {
                    if (!burgerDead)
                        soundBank.PlayCue("BurgerDamage");
                    projectile.IsActive = false;
                    if (projectile.Type == ProjectileType.FrenchFries)
                        burger.Health -= GameConstants.FRENCH_FRIES_PROJECTILE_DAMAGE;
                    else if (projectile.Type == ProjectileType.TeddyBear)
                        burger.Health -= GameConstants.TEDDY_BEAR_PROJECTILE_DAMAGE;
                    CheckBurgerKill();
                }

            // check and resolve collisions between teddy bears and projectiles
            // (make sure to check every teddy bear/projectile pairing)
            foreach(TeddyBear bear in bears)
                foreach(Projectile projectile in projectiles)
                    // check to see if the current teddy bear and the current projectile collide
                    if (projectile.Type == ProjectileType.FrenchFries &&
                        bear.DrawRectangle.Intersects(projectile.CollisionRectangle))
                    {
                        score += GameConstants.BEAR_POINTS;
                        scoreString = GameConstants.SCORE_PREFIX + score;
                        bear.IsActive = false;
                        projectile.IsActive = false;
                        // explode teddy bear when hit by french fries
                        explosions.Add(new Explosion(explosionSpriteStrip, bear.Location.X, bear.Location.Y));
                        soundBank.PlayCue("Explosion");
                    }

            // clean out inactive teddy bears and add new ones as necessary
            for (int i = bears.Count-1; i >= 0; i--)
                if (!bears[i].IsActive)
                    bears.RemoveAt(i);
            while (bears.Count < GameConstants.MAX_BEARS)
                SpawnBear();

            // clean out inactive projectiles
            for (int i = projectiles.Count - 1; i >= 0; i--)
                if (!projectiles[i].IsActive)
                    projectiles.RemoveAt(i);

            // clean out finished explosions
            for (int i = explosions.Count - 1; i >= 0; i--)
                if (explosions[i].Finished)
                    explosions.RemoveAt(i);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            // draw game objects
            // draw the burger
            burger.Draw(spriteBatch);
            foreach (TeddyBear bear in bears)
            {
                bear.Draw(spriteBatch);
            }
            foreach (Projectile projectile in projectiles)
            {
                projectile.Draw(spriteBatch);
            }
            foreach (Explosion explosion in explosions)
            {
                explosion.Draw(spriteBatch);
            }

            // draw score and health
            spriteBatch.DrawString(font, scoreString, GameConstants.SCORE_LOCATION, Color.White);
            spriteBatch.DrawString(font, healthString, GameConstants.HEALTH_LOCATION, Color.White);

            spriteBatch.End();

            base.Draw(gameTime);
        }

        #region Public methods

        /// <summary>
        /// Gets the projectile sprite for the given projectile type
        /// </summary>
        /// <param name="type">the projectile type</param>
        /// <returns>the projectile sprite for the type</returns>
        public static Texture2D GetProjectileSprite(ProjectileType type)
        {
            // return correct projectile sprite based on projectile type
            if (type == ProjectileType.TeddyBear)
                return teddyBearProjectileSprite;
            else
                return frenchFriesSprite;
        }

        /// <summary>
        /// Adds the given projectile to the game
        /// </summary>
        /// <param name="projectile">the projectile to add</param>
        public static void AddProjectile(Projectile projectile)
        {
            projectiles.Add(projectile);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Spawns a new teddy bear at a random location
        /// </summary>
        private void SpawnBear()
        {
            // generate random x and y locations for the bear
            int x = GetRandomLocation(0, GameConstants.WINDOW_WIDTH);
            int y = GetRandomLocation(0, GameConstants.WINDOW_HEIGHT - SPAWN_BORDER_SIZE);

            // generate random velocity
            float velAbs = GameConstants.MIN_BEAR_SPEED + RandomNumberGenerator.NextFloat(GameConstants.BEAR_SPEED_RANGE);

            // generate a random angle
            double angle = RandomNumberGenerator.NextDouble() * Math.PI;

            // create a new Vector2 object using the random speed and angles generated and the appropriate trigonometry
            Vector2 vel = new Vector2(velAbs * (float)Math.Cos(angle), velAbs * (float)Math.Sin(angle));

            // create a new teddy bear
            TeddyBear newBear = new TeddyBear(Content, "teddybear", x, y, vel);

            List<Rectangle> existingRectangles = GetCollisionRectangles();

            // Only spawn new teddy bear into a collision-free location
            while (CollisionUtils.IsCollisionFree(newBear.DrawRectangle, existingRectangles) == false)
                newBear = new TeddyBear(Content, "teddybear", GetRandomLocation(0, GameConstants.WINDOW_WIDTH),
                    GetRandomLocation(0, GameConstants.WINDOW_HEIGHT - SPAWN_BORDER_SIZE), vel);

            // add the new bear to the list of bears included in the game
            bears.Add(newBear);
        }

        /// <summary>
        /// Gets a random location using the given min and range
        /// </summary>
        /// <param name="min">the minimum</param>
        /// <param name="range">the range</param>
        /// <returns>the random location</returns>
        private int GetRandomLocation(int min, int range)
        {
            return min + RandomNumberGenerator.Next(range);
        }

        /// <summary>
        /// Gets a list of collision rectangles for all the objects in the game world
        /// </summary>
        /// <returns>the list of collision rectangles</returns>
        private List<Rectangle> GetCollisionRectangles()
        {
            List<Rectangle> collisionRectangles = new List<Rectangle>();
            collisionRectangles.Add(burger.CollisionRectangle);
            foreach (TeddyBear bear in bears)
            {
                collisionRectangles.Add(bear.CollisionRectangle);
            }
            foreach (Projectile projectile in projectiles)
            {
                collisionRectangles.Add(projectile.CollisionRectangle);
            }
            foreach (Explosion explosion in explosions)
            {
                collisionRectangles.Add(explosion.CollisionRectangle);
            }
            return collisionRectangles;
        }

        /// <summary>
        /// Checks to see if the burger has just been killed
        /// </summary>
        private void CheckBurgerKill()
        {
            if (burger.Health <= 0 && !burgerDead)
            {
                explosions.Add(new Explosion(explosionSpriteStrip, burger.CollisionRectangle.Center.X,
                    burger.CollisionRectangle.Center.Y));
                soundBank.PlayCue("BurgerDeath");
                burgerDead = true;
            }
        }

        #endregion
    }
}
