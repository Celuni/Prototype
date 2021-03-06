﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TiledSharp;
using prototype.Engine.MonoTinySpace;
using prototype.Engine.AI;

namespace prototype.Engine
{
    class Region
    {
        public string Name;    
        public Vector2 PlayerSpawn;
        public List<Enemy> EnemyList;
        public List<RegionPortal> RegionPortals;
        private Astar A;
        private TmxMap TileMap;
        private List<Atlas> Atlases;
        private List<Dictionary<Vector2, Vector3>> AtlasLookUp;
        private List<Rectangle> collsionTiles;
        private ContentManager ContentMgr;
        private Dictionary<Vector2, bool> NonWalkableList;
        public Player player; // todo fix when i do entitiez, ideally should be in entity list
        private int PlayerAnimationCount = 0;
        public TCWorld World;
        public int WalkableTiles
        {
            get
            {
                return TileMap.Height * TileMap.Width - NonWalkableList.Count;
            }
        }

        public Region(string name, TmxMap map, ContentManager content, TCWorld world)
        {
            Name = name;
            TileMap = map;
            ContentMgr = content;
            World = world;
            collsionTiles = new List<Rectangle>();
            NonWalkableList = new Dictionary<Vector2, bool>();
            Atlases = processAtlases(map, content);
            AtlasLookUp = processMap(map, Atlases);
            //World = new TCWorld();
        }

        /// <summary>
        /// Draws the Region
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch needed to draw</param>
        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var layer in AtlasLookUp)
            {
                foreach (var tile in layer)
                {
                    int atlasIndex = (int)tile.Value.X;
                    int row = (int)tile.Value.Y;
                    int col = (int)tile.Value.Z;

                    Rectangle sourceRect = new Rectangle(row, col, Atlases[atlasIndex].ElementWidth, Atlases[atlasIndex].ElementHeight);
                    Rectangle destRect = new Rectangle((int)tile.Key.X * Atlases[atlasIndex].ElementWidth, (int)tile.Key.Y * Atlases[atlasIndex].ElementHeight, Atlases[atlasIndex].ElementWidth, Atlases[atlasIndex].ElementHeight);
                    spriteBatch.Draw(Atlases[atlasIndex].Texture, destRect, sourceRect, Color.White);
                }
            }
        }
        /// <summary>
        /// Creates a List of Atlas Objects based on the tilesets in the map param.
        /// </summary>
        /// <param name="map">.tmx map, created in Tiled.</param>
        /// <param name="content">Content manager passed from Game.cs needed to load textures</param>
        /// <returns></returns>
        private List<Atlas> processAtlases(TmxMap map, ContentManager content)
        {
            List<Atlas> tilesets = new List<Atlas>();

            foreach (var tileset in map.Tilesets)
            {
                Texture2D atlas = content.Load<Texture2D>(tileset.Name);
                tilesets.Add(new Atlas(tileset.FirstGid,
                                        tileset.TileWidth,
                                        tileset.TileHeight,
                                        (int)tileset.Image.Height / tileset.TileHeight,
                                        (int)tileset.Image.Width / tileset.TileWidth,
                                        atlas));
            }

            return tilesets;

        }

        /// <summary>
        /// Creates a list of hashtables aka Dictionaries that are used to look up asset positions for drawing the map
        /// </summary>
        /// <param name="map">.tmx map, created in Tiled</param>
        /// <param name="tilesets">List of Atlas objects generated from processAtlases()</param>
        /// <returns></returns>
        private List<Dictionary<Vector2, Vector3>> processMap(TmxMap map, List<Atlas> tilesets)
        {
            List<Dictionary<Vector2, Vector3>> lookup = new List<Dictionary<Vector2, Vector3>>();
            EnemyList = new List<Enemy>();
            Texture2D enemyTexture = ContentMgr.Load<Texture2D>("Soviet Soldier 2");
            // LOOOOOL CUBIC
            foreach (var layer in map.Layers)
            {
                Dictionary<Vector2, Vector3> locs = new Dictionary<Vector2, Vector3>();
                foreach (var tile in layer.Tiles)
                {
                    for (int i = 0; i < tilesets.Count; i++)
                    {
                        if (tile.Gid >= tilesets[i].FirstGid && tile.Gid < (tilesets[i].FirstGid + (tilesets[i].Rows * tilesets[i].Columns)))
                        {
                            if (layer.Name.Equals("meta"))
                            {
                                if (tile.Gid == 2)
                                {
                                    PlayerSpawn = new Vector2(tile.X * tilesets[i].ElementWidth, tile.Y * tilesets[i].ElementHeight - tilesets[i].ElementHeight);
                                }
                                if (tile.Gid == 1)
                                {
                                    // collision tile
                                    World.AddRect(new TCRectangle(
                                        new Vector2((int)tile.X * tilesets[i].ElementWidth, (int)tile.Y * tilesets[i].ElementHeight),
                                        new Vector2(0, 0),
                                        tilesets[i].ElementWidth,
                                        tilesets[i].ElementHeight,
                                        1));

                                    // keep track of collision tiles so when we construct our path finding grid we set walkable = false
                                    NonWalkableList.Add(new Vector2(tile.X, tile.Y), false);
                                }
                                if(tile.Gid == 3)
                                {
                                    Enemy enemy;
                                    Vector2 EnemyPos = new Vector2(tile.X * tilesets[i].ElementWidth, tile.Y * tilesets[i].ElementHeight - tilesets[i].ElementHeight);
                                    enemy = new Enemy(enemyTexture, EnemyPos, ContentMgr, World);
                                    World.AddRect(enemy.EnemyRect);
                                    EnemyList.Add(enemy);
                                    
                                }
                            }
                            else
                            {
                                // crazy CB3 math
                                locs.Add(
                                    new Vector2(tile.X, tile.Y),
                                    new Vector3(i,
                                        (((tile.Gid - tilesets[i].FirstGid) * tilesets[i].ElementWidth) % tilesets[i].Texture.Width),
                                        (((int)(((tile.Gid - tilesets[i].FirstGid) * tilesets[i].ElementWidth) / tilesets[i].Texture.Width)) * tilesets[i].ElementHeight))
                                    );
                            }



                        }
                    }
                }
                lookup.Add(locs);
            }
            return lookup;
        }        
        

        private List<List<Node>> ConstructGrid()
        {
            List<List<Node>> temp = new List<List<Node>>();
            bool walkable = false;
            float tempX = 0;
            float tempY = 0;

            for(int i = 0; i < TileMap.Width; i++)
            {
                temp.Add( new List<Node>());
                for(int j = 0; j < TileMap.Height; j++)
                {
                    if(NonWalkableList.TryGetValue(new Vector2(i, j), out walkable))
                    {
                        walkable = false;
                    }
                    else
                    {
                        walkable = true;
                    }
                    temp[i].Add(new Node(new Vector2(i,j), walkable));
                    tempX += 32;
                }
                tempX = 0;
                tempY += 32;
            }
            return temp;
        }
        public void MovePlayer(Player p, Vector2 vel)
        {
            World.MoveObject(p, vel);
            player = p;
            
            // HOLy SHIT BUDGE - todo fix
            if (PlayerAnimationCount % p.PlayerAnimation.FrameCtr == 0)
            {
                player.UpdateAnimation();
            }
            PlayerAnimationCount++;
        }

        public void MoveBullets(Particle p)
        {
            World.MoveObject(p, p.velocity);
        }

        public void Reconstruction(Enemy e)
        {
            //A = new Astar(ConstructGrid());
            e.Path = A.FindPath(e.Position, player.Position);
            e.EnemyState = State.Active;
            MoveEnemy(e);
            //World.MoveObjectAlongPath(e, ref e.Path);
            //e.EnemyState = State.Idle;

        }

        // NOTE: i'm going to regret this
        public void MoveEnemy(Enemy e)
        {
            if (e.EnemyState == State.Idle)
            {

                if (e.stepsTraveled < 500)
                {
                    switch (e.DirectionFacing)
                    {
                        case Direction.Left:
                            World.MoveObject(e, new Vector2(-e.EnemyMoveSpeed, 0));
                            break;
                        case Direction.Right:
                            World.MoveObject(e, new Vector2(e.EnemyMoveSpeed, 0));
                            break;
                    }
                }
                else
                {
                    e.stepsTraveled = 0;
                    if (e.DirectionFacing == Direction.Left)
                    {
                        e.DirectionFacing = Direction.Right;
                    }
                    else
                    {
                        e.DirectionFacing = Direction.Left;
                    }
                }
            }
            else if(e.EnemyState == State.Active)
            {
                if(e.Path == null)
                {
                    A = new Astar(ConstructGrid());
                    e.SearchState = EnemySearchState.Searching;
                    e.Path = A.FindPath(e.Position, player.Position);
                    if(e.Path == null)
                    {
                        e.SearchState = EnemySearchState.Unreachable;
                    }
                }
                else if(e.SearchState == EnemySearchState.Alerted)
                {
                    e.SearchState = EnemySearchState.Searching;
                    e.Path = A.FindPath(e.Position, player.Position);
                }

                if(e.SearchState != EnemySearchState.Found && e.Path != null)
                    World.MoveObjectAlongPath(e, ref e.Path);
            }
        }
    }


}