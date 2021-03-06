﻿#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using TiledSharp;
using prototype.Engine.MonoTinySpace;
#endregion

namespace prototype
{
    using Engine;
    public enum Direction
    {
        Up, Down, Left, Right, Buttz
    };
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Game
    {
        ParticleEngine particleEngine;
        GraphicsDeviceManager graphics;
        Vector3 camera;
        SpriteBatch spriteBatch;
        Player player;
        Region region;
        TCWorld world;
        KeyboardState currentKeyState;
        KeyboardState previousKeyState;
        MouseState currentMouseState;
        MouseState previousMouseState;
        float playerMoveSpeed;
        float dodgeSpeed;
        float projectileSpeed;
        
        Song song;
        public Game1()
            : base()
        {
            graphics = new GraphicsDeviceManager(this);

            // todo fix windows gl full screen
            //var screen = System.Windows.Forms.Screen.AllScreens[0];
            //Window.IsBorderless = true;
            //System.Windows.Forms.Form form = (System.Windows.Forms.Form)System.Windows.Forms.Control.FromHandle(Window.Handle);
            ////form.Location = new System.Drawing.Point(0, 0);
            //graphics.PreferredBackBufferWidth = screen.Bounds.Width;
            //graphics.PreferredBackBufferHeight = screen.Bounds.Height;

            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            player = new Player();
            world = new TCWorld();
            playerMoveSpeed = 80.0f;
            dodgeSpeed = 10 * playerMoveSpeed;
            projectileSpeed = 15.0f;
            
            //region = new Region("Demo", new TmxMap("../../../Content/demo.tmx"), this.Content, new TCWorld());
            region = new Region("Demo", new TmxMap("demo.tmx"), this.Content, new TCWorld());

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            List<Texture2D> particleTextures = new List<Texture2D>();
            particleTextures.Add(Content.Load<Texture2D>("red"));
            particleTextures.Add(Content.Load<Texture2D>("darkorange"));
            particleTextures.Add(Content.Load<Texture2D>("orange"));
            particleEngine = new ParticleEngine(particleTextures, new Vector2((float)((13.5f))*32, (float)((22.5f))*32), region.World);
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            Vector2 playerPos = new Vector2(GraphicsDevice.Viewport.TitleSafeArea.X, GraphicsDevice.Viewport.TitleSafeArea.Y + GraphicsDevice.Viewport.TitleSafeArea.Height / 2);
            //player.Initialize(Content.Load<Texture2D>("mrspy.bmp"), Content.Load<Texture2D>("red"), playerPos, this.Content);
            player.Initialize(Content.Load<Texture2D>("mrspy1"), Content.Load<Texture2D>("red"), 
                region.PlayerSpawn, this.Content, Content.Load<Texture2D>("mrspy2"), region.World);
            region.World.AddRect(player.playerRect);
            region.player = player;

            // audio
            SoundEffect song = Content.Load<SoundEffect>("Boss_GENERIC.wav");
            SoundEffectInstance seInstance = song.CreateInstance();
            seInstance.IsLooped = true;
            seInstance.Play();
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
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || currentKeyState.IsKeyDown(Keys.Escape))
                Exit();
           
            // TODO: Add your update logic here
            previousKeyState = currentKeyState;
            currentKeyState = Keyboard.GetState();
            UpdatePlayer(gameTime);
            
            particleEngine.Update();
            if (currentKeyState.IsKeyDown(Keys.X) && !previousKeyState.IsKeyDown(Keys.X))
            {
                player.Shoot();
            }
            player.Update();
            UpdatePlayer(gameTime);
            UpdateEnemies(gameTime);

            base.Update(gameTime);
        }

        private void UpdateEnemies(GameTime gameTime)
        {
            float x, y;
            // VERY BUDGET STYLE enemy logic
            foreach(Enemy e in region.EnemyList)
            {
                x = Math.Abs(e.Position.X - player.Position.X);
                y = Math.Abs(e.Position.Y - player.Position.Y);

                if(e.EnemyState == State.Idle)
                {
                    region.MoveEnemy(e);
                    e.stepsTraveled++;
                }

                if (e.EnemyState == State.Active && (e.SearchState == EnemySearchState.Alerted || e.SearchState == EnemySearchState.Searching))
                {
                    region.MoveEnemy(e);
                }

                if(x < 100 && y < 100)
                {
                    e.Shoot();
                }

                if(x < 500 && y < 500 && x > 150 && y > 150 && e.EnemyState == State.Idle)
                {
                    e.EnemyState = State.Active;
                    e.SearchState = EnemySearchState.Alerted;
                }

                if(e.SearchState == EnemySearchState.Found)
                {
                    e.EnemyState = State.Idle;
                    e.SearchState = EnemySearchState.Searching;
                }

                if (e.SearchState == EnemySearchState.Unreachable)
                {
                    e.EnemyState = State.Idle;
                }

                e.Update();
            }

        }

        //TODO refactor
        private void UpdatePlayer(GameTime gameTime)
        {
            if (currentKeyState.IsKeyDown(Keys.Left))
            {
                region.MovePlayer(player,new Vector2(-playerMoveSpeed, 0));              
                if (!currentKeyState.IsKeyDown(Keys.Space))
                {
                    player.directionFacing = Direction.Left;
                }
            }

            if (currentKeyState.IsKeyDown(Keys.Right))
            {
                region.MovePlayer(player,new Vector2(playerMoveSpeed, 0));
                if (!currentKeyState.IsKeyDown(Keys.Space))
                {
                    player.directionFacing = Direction.Right;
                }
            }

            if (currentKeyState.IsKeyDown(Keys.Up))
            {
                region.MovePlayer(player, new Vector2(0, -playerMoveSpeed));
                if (!currentKeyState.IsKeyDown(Keys.Space))
                {
                    player.directionFacing = Direction.Up;
                }
            }
            if (currentKeyState.IsKeyDown(Keys.Down))
            {
                region.MovePlayer(player, new Vector2(0, playerMoveSpeed));
                if (!currentKeyState.IsKeyDown(Keys.Space))
                {
                    player.directionFacing = Direction.Down;
                }
            }

            // DODGE mechanic maybe. TODO: tweak
            if (currentKeyState.IsKeyDown(Keys.Z) && !previousKeyState.IsKeyDown(Keys.Z))
            {
                switch(player.directionFacing)
                {
                    case Direction.Left:
                        region.MovePlayer(player, new Vector2(-dodgeSpeed, 0));
                        break;
                    case Direction.Right:
                        region.MovePlayer(player, new Vector2(dodgeSpeed, 0));
                        break;
                    case Direction.Up:
                        region.MovePlayer(player, new Vector2(0, -dodgeSpeed));
                        break;
                    case Direction.Down:
                        region.MovePlayer(player, new Vector2(0, dodgeSpeed));
                        break;
                }
            }

            // TODO: translation function maybe
            camera.X = -player.Position.X + GraphicsDevice.Viewport.Bounds.Width / 2;
            camera.Y = -player.Position.Y + GraphicsDevice.Viewport.Bounds.Height / 2;
            camera.Z = 0;

            if(currentKeyState.IsKeyDown(Keys.Q) && !previousKeyState.IsKeyDown(Keys.Q))
            {
                foreach(Enemy e in region.EnemyList)
                {
                    Console.Write("state = {0}, path = {1}", e.EnemyState, e.Path);
                    if (e.EnemyState == State.Idle)
                    {
                        region.Reconstruction(e);
                        //e.EnemyState = State.Active;
                    }
                }
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            // TODO: Add your drawing code here
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Matrix.CreateTranslation(camera));
            

            region.Draw(spriteBatch);
            player.Draw(spriteBatch);

            foreach (var enemy in region.EnemyList)
            {
                enemy.Draw(spriteBatch);
                enemy.DrawBullets(spriteBatch, camera);
            }

           

            // bulletz
            player.DrawBullets(spriteBatch, camera);

            // fire in the middle of the map
            // particleEngine.Draw(spriteBatch, camera);
            spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
