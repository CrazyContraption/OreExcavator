using Microsoft.Xna.Framework;
using MonoMod.Cil;                   // IL
using Newtonsoft.Json;
using OreExcavator.Enumerations;     // Enums

using System;                        // IndexOf
using System.Collections.Concurrent; // ConcurrentDictionary
using System.Collections.Generic;    // Lists & Dictionaries
using System.IO;                     // Binary Reader, Path
using System.Reflection;
using System.Threading;              // Threading
using System.Threading.Tasks;        // Threading

using Terraria;                      // Terraria
using Terraria.DataStructures;       // Point16
using Terraria.GameContent.UI.Chat;
using Terraria.ID;                   // Networking
using Terraria.ModLoader;            // Modloader
using Terraria.ModLoader.Config;
using Terraria.ObjectData;
using Terraria.UI.Chat;

namespace OreExcavator /// The Excavator of ores
{
    public class OreExcavator : Mod
    {
        internal static ModKeybind ExcavateHotkey;
        internal static ModKeybind WhitelistHotkey;
        internal static ModKeybind BlacklistHotkey;

        internal static OreExcavatorConfig_Client ClientConfig = ModContent.GetInstance<OreExcavatorConfig_Client>();
        internal static OreExcavatorConfig_Server ServerConfig = ModContent.GetInstance<OreExcavatorConfig_Server>();
        internal static readonly string ModConfigPath = Path.Combine(Main.SavePath, "ModConfigs");

        internal static ConcurrentDictionary<Point16, bool> masterTiles = new();
        public static OreExcavator myMod = ModContent.GetInstance<OreExcavator>();


        /////////////////////////////////////
        ///                               ///
        ///     !!!THE DANGER ZONE!!!     ///
        ///                               ///
        internal static bool devmode = false;
        ///                               ///
        /////////////////////////////////////

        /// <summary>
        /// Per-thread boolean that signifies if an excavation-related actions are taking place on that thread.
        /// </summary>
        [ThreadStatic] public static bool killCalled = false;
        [ThreadStatic] public static bool excavating = false;

        /// <summary>
        /// Per-thread boolean that signifies if an excavation puppeting is taking place on that thread.
        /// </summary>
        [ThreadStatic] public static bool puppeting = false;

        internal static Tile lookingAtTile;
        internal static ushort lookingCoordX;
        internal static ushort lookingCoordY;
        internal static bool[] playerHalted = new bool[Main.maxPlayers+2];
        internal static bool excavationToggled = false;
        internal static int msgReps = 1;

        /// <summary>
        /// Called by tML when the mod is asked to load.
        /// This binds various important aspects of the mod.
        /// </summary>
        public override void Load()
        {
            ExcavateHotkey  = KeybindLoader.RegisterKeybind(this, "Excavate (while mining)", "OemTilde");
            WhitelistHotkey = KeybindLoader.RegisterKeybind(this, "Whitelist hovered", "Insert");
            BlacklistHotkey = KeybindLoader.RegisterKeybind(this, "Un-whitelist hovered", "Delete");

            // Detours
            On.Terraria.WorldGen.SpawnFallingBlockProjectile += Detour_SpawnFallingBlockProjectile;

            // IL edits
            /*
            if (ServerConfig.aggressiveCompatibility)
            {
                IL.Terraria.WorldGen.KillTile_PlaySounds += ReturnIfCalled;
                IL.Terraria.WorldGen.KillWall_PlaySounds += ReturnIfCalled;
            }
            */

            // TODO: Unit testing?
            /*
            if (devmode)
            {

            }
            */
        }

        private bool Detour_SpawnFallingBlockProjectile(
            On.Terraria.WorldGen.orig_SpawnFallingBlockProjectile orig,
            int x,
            int y,
            Tile tileCache,
            Tile tileTopCache,
            Tile tileBottomCache,
            int type)
        {
            if (puppeting || killCalled)
                return false;

            if (ClientConfig.toggleExcavations ? excavationToggled : OreExcavatorKeybinds.excavatorHeld)
                if (CheckIfAllowed(tileCache.TileType, ActionType.TileKilled))
                    return false;

            return orig(x, y, tileCache, tileTopCache, tileBottomCache, type);
        }


        /// <summary>
        /// IL-ready injection that returns any injected method immediately if the thread calling it is doing an excavation
        /// </summary>
        /// 
        /// <param name="il"></param>
        /*private static void ReturnIfCalled(ILContext il)
        {
            var cursor = new ILCursor(il); // The current "instruction"
            var label = il.DefineLabel();

            cursor.EmitDelegate<Func<bool>>(() => killCalled); // If this thread manually called a kill
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Brfalse_S, label); // If kill was not manually called, goto label
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_0); // Add false to the stack for the return
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ret); // If kill was called, return immediately, don't continue
            cursor.MarkLabel(label); // Label
        }*/


        /// <summary>
        /// Executes once most -if not all- modded content is loaded by tML.
        /// Looks for items from other mods that could be classified as ores, gems, chunks, etc.
        /// May change from time to time as mods change their preferences.
        /// </summary>
        public override void PostSetupContent()
        {
            Log("Looking for modded ores and gems to whitelist...", default, LogType.Info);
            for (int id = TileID.Count; id < TileLoader.TileCount; id++)
            {
                string name = "";
                ModTile tile = TileLoader.GetTile(id);
                if (tile is null)
                    name = TileID.Search.GetName(id);
                else
                    name = tile.Name;
                
                // Strip Tile and T from modded ores (common naming convention)
                // This is done to use EndsWith in next if statement (faster than IndexOf). 
                if (name.EndsWith("Tile"))
                    name = name.Substring(0, name.Length - 4);
                else if (name.EndsWith("T"))
                    name = name.Substring(0, name.Length - 1);

                string newName = name;

                // If modded tile is ore, gem, or chunk, replace with upper case (formatting).
                // This if is used for auto-whitelist adding modded tiles later.
                if (name.EndsWith("Ore"))
                    newName = newName.Replace("Ore", "'ORE'");
                else if (name.EndsWith("Gem"))
                    newName = newName.Replace("Ore", "'GEM'");
                else if (name.EndsWith("Chunk"))
                    newName = newName.Replace("Ore", "'CHUNK'");
                else // Final else checks if the item dropped contains a matching check (to catch stragglers).
                { 
                    if (tile is not null)
                    {
                        ModItem item = ItemLoader.GetItem(tile.ItemDrop);
                        if (item is not null)
                            name = item.Name;
                        else
                            name = new Item(tile.ItemDrop).Name;
                        newName = name;
                        if ((name ?? "") != "")
                        {
                            if (name.EndsWith("Ore"))
                                newName = newName.Replace("Ore", "'ORE'");
                            else if (name.EndsWith("Gem"))
                                newName = newName.Replace("Ore", "'GEM'");
                            else if (name.EndsWith("Chunk"))
                                newName = newName.Replace("Ore", "'CHUNK'");
                        }
                    }
                }
                if (name != newName)
                {
                    name = GetFullNameById(id, ActionType.TileWhiteListed);
                    if (!ClientConfig.tileWhitelist.Contains(name))
                    {
                        Log($"Found vein TileID.{newName}, adding as {name}", default, LogType.Info);
                        ClientConfig.tileWhitelist.Add(name);
                    }
                    else
                        Log($"Found vein TileID.{newName}, but was already whitelisted as {name}", default, LogType.Info);
                }
            }
            SaveConfig(ClientConfig);
            Log("Done whitelisting modded content.", default, LogType.Info);
        }

        public override void Unload()
        {
            base.Unload();
        }

        /// <summary>
        /// Handles incoming packets from clients, or the server.
        /// Should never execute during singleplayer scenarios
        /// </summary>
        /// 
        /// <param name="reader">Stream of data contained by the packet, first byte is always the type</param>
        /// <param name="whoAmI">Player index of who sent the packet</param>
        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;
            
            ModPacket packet = GetPacket();

            byte origin = (byte)whoAmI;
            if (Main.netMode == NetmodeID.MultiplayerClient)
                origin = reader.ReadByte();
            else
                packet.Write((byte)whoAmI);

            ActionType msgType = (ActionType)reader.ReadByte();

            Log($"Packet from: '{(Main.netMode == NetmodeID.MultiplayerClient ? $"SERVER', origin: 'Player.{Main.player[origin].name}" : "Player." + Main.player[origin].name) + $" ({origin})"}', type: 'ActionType.{msgType}'");

            switch (msgType)
            {
                case ActionType.HaltExcavations:
                    {
                        if (Main.netMode == NetmodeID.Server)
                        {
                            packet.Write((byte)ActionType.HaltExcavations);
                            packet.Send(-1, origin);
                        }
                        playerHalted[origin] = true;
                    }
                    break;

                case ActionType.ResetExcavations:
                    {
                        if (Main.netMode == NetmodeID.Server)
                        {
                            packet.Write((byte)ActionType.ResetExcavations);
                            packet.Send(-1, origin);
                        }
                        playerHalted[origin] = false;
                    }
                    break;

                case ActionType.TileKilled:
                    {
                        playerHalted[origin] = false;
                        ushort x = reader.ReadUInt16(), y = reader.ReadUInt16(), limit = reader.ReadUInt16(), delay = reader.ReadUInt16();
                        bool doDiagonals = reader.ReadBoolean();
                        ushort targetType = reader.ReadUInt16(), itemToTeleport = reader.ReadUInt16();
                        ModifySpooler(msgType, x, y, limit, delay, doDiagonals, targetType, origin, true);
                    }
                    break;

                case ActionType.WallKilled:
                    {
                        playerHalted[origin] = false;
                        ushort x = reader.ReadUInt16(), y = reader.ReadUInt16(), limit = reader.ReadUInt16(), delay = reader.ReadUInt16();
                        bool doDiagonals = reader.ReadBoolean();
                        ushort targetType = reader.ReadUInt16();
                        ModifySpooler(msgType, x, y, limit, delay, doDiagonals, targetType, origin, true);
                    }
                    break;

                case ActionType.TileReplaced:
                    {
                        playerHalted[origin] = false;
                        ushort x = reader.ReadUInt16(), y = reader.ReadUInt16(), limit = reader.ReadUInt16(), delay = reader.ReadUInt16();
                        bool doDiagonals = reader.ReadBoolean();
                        ushort targetType = reader.ReadUInt16();
                        int targetSubtype = reader.ReadInt16(), createItem = reader.ReadInt16(), itemSubtype = reader.ReadInt16();
                        ModifySpooler(msgType, x, y, limit, delay, doDiagonals, targetType, origin, true, targetSubtype, createItem, itemSubtype);
                    }
                    break;

                case ActionType.WallReplaced:
                    {
                        playerHalted[origin] = false;
                        ushort x = reader.ReadUInt16(), y = reader.ReadUInt16(), limit = reader.ReadUInt16(), delay = reader.ReadUInt16();
                        bool doDiagonals = reader.ReadBoolean();
                        ushort targetType = reader.ReadUInt16();
                        int targetSubtype = reader.ReadInt16(), createItem = reader.ReadInt16(), itemSubtype = reader.ReadInt16();
                        ModifySpooler(msgType, x, y, limit, delay, doDiagonals, targetType, origin, true, targetSubtype, createItem, itemSubtype);
                    }
                    break;

                case ActionType.TurfPlanted:
                    {
                        playerHalted[origin] = false;
                        ushort x = reader.ReadUInt16(), y = reader.ReadUInt16(), limit = reader.ReadUInt16(), delay = reader.ReadUInt16();
                        bool doDiagonals = reader.ReadBoolean();
                        ushort targetType = reader.ReadUInt16();
                        int targetSubtype = reader.ReadInt16(), createItem = reader.ReadInt16(), itemSubtype = reader.ReadInt16();
                        ModifySpooler(msgType, x, y, limit, delay, doDiagonals, targetType, origin, true, targetSubtype, createItem, itemSubtype);
                    }
                    break;

                case ActionType.TurfHarvested:
                    {
                        playerHalted[origin] = false;
                        ushort x = reader.ReadUInt16(), y = reader.ReadUInt16(), limit = reader.ReadUInt16(), delay = reader.ReadUInt16();
                        bool doDiagonals = reader.ReadBoolean();
                        ushort targetType = reader.ReadUInt16();
                        int newDirt = reader.ReadInt16();
                        ModifySpooler(msgType, x, y, limit, delay, doDiagonals, targetType, origin, true, -1, newDirt, -1);
                    }
                    break;

                case ActionType.TilePainted:
                    {
                        playerHalted[origin] = false;
                        ushort x = reader.ReadUInt16(), y = reader.ReadUInt16(), limit = reader.ReadUInt16(), delay = reader.ReadUInt16();
                        bool doDiagonals = reader.ReadBoolean();
                        ushort targetType = reader.ReadUInt16();
                        int newPaint = reader.ReadInt16();
                        ModifySpooler(msgType, x, y, limit, delay, doDiagonals, targetType, origin, true, -1, newPaint, -1);
                    }
                    break;

                case ActionType.TileWhiteListed:
                case ActionType.WallWhiteListed:
                case ActionType.ItemWhiteListed:
                case ActionType.TileBlackListed:
                case ActionType.WallBlackListed:
                case ActionType.ItemBlackListed:
                    {
                        ushort id = (ushort)reader.ReadInt32();

                        if (Main.netMode == NetmodeID.MultiplayerClient)
                            switch (msgType)
                            {
                                case ActionType.TileWhiteListed:
                                    Log($"{Main.player[origin].name} added 'Tile.{GetFullNameById(id, msgType)} ({id})' to their personal tile whitelist.",
                                        Color.Green, LogType.Info);
                                    break;

                                case ActionType.WallWhiteListed:
                                    Log($"{Main.player[origin].name} added 'Wall.{GetFullNameById(id, msgType)} ({id})' to their personal wall whitelist.",
                                        Color.Orange, LogType.Info);
                                    break;

                                case ActionType.ItemWhiteListed:
                                    Log($"{Main.player[origin].name} added 'Item.{GetFullNameById(id, msgType)} ({id})' to their personal swap whitelist.",
                                        Color.Green, LogType.Info);
                                    break;

                                case ActionType.TileBlackListed:
                                    Log($"{Main.player[origin].name} removed 'Tile.{GetFullNameById(id, msgType)} ({id})' from their personal tile whitelist.",
                                        Color.Orange, LogType.Info);
                                    break;

                                case ActionType.WallBlackListed:
                                    Log($"{Main.player[origin].name} removed 'Wall.{GetFullNameById(id, msgType)} ({id})' from their personal wall whitelist.",
                                        Color.Green, LogType.Info);
                                    break;

                                case ActionType.ItemBlackListed:
                                    Log($"{Main.player[origin].name} removed 'Item.{GetFullNameById(id, msgType)} ({id})' from their personal swap whitelist.",
                                        Color.Orange, LogType.Info);
                                    break;
                            }
                        else
                        {
                            packet.Write((byte)msgType);
                            packet.Write(id);
                            packet.Send(-1, origin);
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// This "spools up" a threaded task that will define and monitor a new excavation. Clients call this locally, and servers that recieve valid packets call this with pupetting properties.
        /// </summary>
        /// 
        /// <param name="actionType"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="limit"></param>
        /// <param name="delay"></param>
        /// <param name="doDiagonals"></param>
        /// <param name="targetType"></param>
        /// <param name="playerId"></param>
        /// <param name="puppeting"></param>
        /// <param name="targetSubtype"></param>
        /// <param name="replacementType"></param>
        /// <param name="itemSubtype"></param>
        internal static void ModifySpooler(ActionType actionType, int x, int y, int limit, int delay, bool doDiagonals, int targetType, byte playerId, bool puppeting = false, int targetSubtype = -1, int replacementType = -1, int itemSubtype = -1)
        {
            var checkPoint = new Point16(x, y);

            if (masterTiles.ContainsKey(checkPoint))
                masterTiles.Remove(checkPoint, out _);

            if (actionType == ActionType.TileReplaced && replacementType < 0)
                return;
            if (actionType == ActionType.WallReplaced && replacementType <= 0)
                return;

            if (Main.netMode == NetmodeID.Server || (Main.netMode == NetmodeID.MultiplayerClient && !puppeting))
            {
                Log(playerId + " SENDING " + actionType);
                ModPacket packet = myMod.GetPacket();
                if (Main.netMode == NetmodeID.Server)
                    packet.Write(playerId);
                packet.Write((byte)actionType);
                packet.Write((ushort)x);
                packet.Write((ushort)y);
                packet.Write((ushort)limit);
                packet.Write((ushort)delay);
                packet.Write(doDiagonals);
                packet.Write((ushort)targetType);
                if (actionType == ActionType.TileReplaced || actionType == ActionType.WallReplaced || actionType == ActionType.TurfPlanted || actionType == ActionType.TilePainted)
                {
                    packet.Write((int)targetSubtype);
                    packet.Write((int)replacementType);
                    packet.Write((int)itemSubtype);
                }
                else if (actionType == ActionType.TurfHarvested)
                    packet.Write((ushort)replacementType);
                packet.Send(-1, playerId);
            }
            new Task(delegate
            {
                OreExcavator.puppeting = puppeting;
                ModifyAdjacentFloodfill(actionType, x, y, limit, delay, doDiagonals, targetType, playerId, targetSubtype, replacementType, itemSubtype);
            }).Start();
        }

        /// <summary>
        /// Searches around the given origin for matching types, and modifies them based on refined clauses.
        /// This also recursively schedules new nodes to search from, but only serially, never concurrently.
        /// tML hates concurrency in large amounts, its got enough to worry about already.
        /// </summary>
        /// 
        /// <param name="actionType">Type of action being performed</param>
        /// <param name="originX">X coordinate of the entity to start searching from</param>
        /// <param name="originY">Y coordinate  of the entity to start searching from (top of world is y = 0)</param>
        /// <param name="limit">Number of tiles allowed to be modified per execution</param>
        /// <param name="delay"></param>
        /// <param name="doDiagonals"></param>
        /// <param name="targetType">ID of the entity that is being searched for, and modified</param>
        /// <param name="playerID">The player index to teleport items to, if needed</param>
        /// <param name="targetSubtype">Frame Y of the targeted type to destroy</param>
        /// <param name="replaceItemType">If the action is replacing, what are we replacing the entity with</param>
        /// <param name="itemSubtype">ID of the item style that is being replaced with, if needed</param>
        internal static void ModifyAdjacentFloodfill(ActionType actionType, int originX, int originY, int limit, int delay, bool doDiagonals, int targetType, byte playerID, int targetSubtype = -1, int replaceItemType = -1, int itemSubtype = -1)
        {
            bool isWall = false;
            if (actionType == ActionType.WallKilled || actionType == ActionType.WallReplaced || actionType == ActionType.WallPainted || actionType == ActionType.ClearedPaint)
                isWall = true;

            bool isReplacing = actionType == ActionType.TileReplaced || actionType == ActionType.WallReplaced || actionType == ActionType.TurfPlanted ? true : false;
            bool falling = (!isWall && TileID.Sets.Falling[targetType] || (ModContent.TileType<ModTile>() > 0 && TileID.Sets.Falling[ModContent.TileType<ModTile>()])) && !isReplacing;

            sbyte[] ch_x;
            sbyte[] ch_y;
            ch_x = new sbyte[] { 0, 1, 0, -1, 1, 1, -1, -1 };
            ch_y = new sbyte[] { -1, 0, 1, 0, -1, 1, 1, -1 };

            // Untested
            int hardlimit = Math.Min(ClientConfig.recursionLimit, ServerConfig.recursionLimit);
            if (limit > 0)
                if (Main.netMode == NetmodeID.MultiplayerClient) // Dupe check, ensures that if player count of item is less than world limit, set local limit to item count.
                    if (isReplacing && hardlimit > Main.player[playerID].CountItem(replaceItemType))
                        limit = Main.player[playerID].CountItem(replaceItemType);
                    else
                        limit = hardlimit;

            if (actionType == ActionType.TurfHarvested || actionType == ActionType.TilePainted)
                isReplacing = true;

            Log($"(ID:{playerID} - P:{puppeting}) {actionType}, OX:{originX}, OY:{originY}, L:{limit}, D:{delay}, DD:{doDiagonals}, TT:{targetType}:{targetSubtype}, RT:{replaceItemType}:{itemSubtype} F:{falling}", Color.Yellow);

            Queue<Point16> queue = new();

            queue.Enqueue(new Point16(originX, originY));
            for (int tileCount = 0; queue.Count > 0 && tileCount < limit && !Main.gameMenu; tileCount++)
            {
                Point16 currentPoint = queue.Dequeue();

                if (masterTiles.TryAdd(new Point16(currentPoint.X, currentPoint.Y), false))
                {
                    if (tileCount > 0 || originX != currentPoint.X || originY != currentPoint.Y) // Ensure we're not spawning dirt for the initial break
                    {
                        killCalled = true;
                        bool success = AlterHandler(actionType, currentPoint.X, currentPoint.Y, playerID, (isReplacing ? replaceItemType : -1), (isReplacing ? itemSubtype : -1));
                        _ = masterTiles.TryRemove(new Point16(currentPoint.X, currentPoint.Y), out _);
                        killCalled = false;
                        if (!success)
                            break;
                    }
                    else
                        _ = masterTiles.TryRemove(new Point16(currentPoint.X, currentPoint.Y), out _);
                }
                else
                    continue;

                if (delay > 0)
                    Thread.Sleep(delay);

                for (byte index = 0; index < (doDiagonals ? (falling ? 8 : 8) : (falling ? 4 : 4)); index++) // Looks fancy, but really just adds 2 or 4 if diagonals should be included
                {
                    Point16 nextPoint = new(currentPoint.X + ch_x[index], currentPoint.Y + ch_y[index]);

                    ushort x = (ushort)nextPoint.X;
                    ushort y = (ushort)nextPoint.Y;

                    if (WorldGen.InWorld(x, y) && !queue.Contains(nextPoint) && !masterTiles.ContainsKey(nextPoint))
                    {
                        if (!isWall && WorldGen.CanKillTile(x, y) && Framing.GetTileSafely(nextPoint) is Tile checkTile && checkTile.HasTile && checkTile.TileType == targetType)
                        {
                            switch (actionType)
                            {
                                case ActionType.TileKilled:
                                case ActionType.TileReplaced:
                                case ActionType.TilePainted:
                                case ActionType.ClearedPaint:
                                {
                                        if (targetSubtype < 0)
                                            goto default;
                                        if (targetSubtype == TileObjectData.GetTileStyle(checkTile))
                                            queue.Enqueue(nextPoint);
                                    }
                                    continue;
                                case ActionType.TurfPlanted:
                                    if (!Main.tile[x, y - 1].HasTile || !Main.tile[x, y + 1].HasTile || !Main.tile[x - 1, y].HasTile || !Main.tile[x + 1, y].HasTile)
                                        goto default;
                                    continue;
                                default:
                                    if (checkTile.TileType != Main.tile[originX, originY].TileType || !Main.tile[originX, originY].HasTile || actionType == ActionType.TilePainted)
                                        queue.Enqueue(nextPoint);
                                    continue;
                            }
                        }
                        else if (isWall && (!Main.tile[x, y].HasTile || (Main.tile[x, y].HasTile && Main.tile[x, y].Slope != SlopeType.Solid)) && Main.tile[x, y].WallType == targetType)
                            queue.Enqueue(nextPoint);
                        else if (actionType == ActionType.ClearedPaint)
                            queue.Enqueue(nextPoint);
                    }
                }
                if ((playerHalted[playerID] && puppeting) || (Main.netMode != NetmodeID.Server && playerID == Main.myPlayer && ClientConfig.releaseCancelsExcavations && !(ClientConfig.toggleExcavations ? excavationToggled : OreExcavatorKeybinds.excavatorHeld)))
                    break;
            }

            if (Main.gameMenu)
            {
                masterTiles.Clear();
                queue.Clear();
                puppeting = false;
                return;
            }

            if ((playerHalted[playerID] && puppeting) || (Main.netMode != NetmodeID.Server && playerID == Main.myPlayer && ClientConfig.releaseCancelsExcavations && !(ClientConfig.toggleExcavations ? excavationToggled : OreExcavatorKeybinds.excavatorHeld)))
            {
                Log(playerID + " HALTED", default, LogType.Debug);
                if (Main.netMode == NetmodeID.MultiplayerClient && playerID == Main.myPlayer)
                {
                    ModPacket packet = myMod.GetPacket();
                    packet.Write((byte)ActionType.HaltExcavations);
                    packet.Send();
                    Log(playerID + " SENDING " + ActionType.HaltExcavations);
                }

                Thread.Sleep(delay + (Math.Max(delay, 2) / 2));

                while (queue.Count > 0)
                {
                    WorldGen.SquareTileFrame(queue.Peek().X, queue.Peek().Y);
                    _ = masterTiles.TryRemove(queue.Dequeue(), out _);
                }

                if (Main.netMode == NetmodeID.MultiplayerClient && playerID == Main.myPlayer)
                {
                    ModPacket packet = myMod.GetPacket();
                    packet.Write((byte)ActionType.ResetExcavations);
                    packet.Send();
                    Log(playerID + " SENDING " + ActionType.ResetExcavations);
                }
                playerHalted[playerID] = false;
            }

            queue.Clear(); // Clear the queue

            puppeting = false;

            Log(playerID + " EXCAVATION COMPLETED");
        }

        /* AlterHandler
         * 
         * You can tell this method is old, it's comment syntax just hits different, don't it?
         * 
         * int x - X coordinate of the block/wall to be killed
         * int y - Y coordinate of the block/wall to be killed (top of world is y = 0)
         * bool isWall - True if the tile in question is actually a wall
         * 
         * Called when we want to kill a block/wall.
         * Handles the checks, multiplayer replication, and most of the code deduplication for the breaking process
         */
        public static bool AlterHandler(ActionType actionType, int x, int y, byte playerID, int consumesItemType = -1, int itemSubtype = -1)
        {
            if (devmode)
                Log($"AlterHandler ({x},{y}) - {actionType} = {consumesItemType}:{itemSubtype}");

            if (x < 0 || y < 0)
                return false;

            switch (actionType)
            {
                case ActionType.WallKilled:
                    {
                        if (!Main.tile[x, y].HasTile)
                            WorldGen.KillWall(x, y, false);
                    }
                    break;

                case ActionType.TileKilled:
                    {
                        if (actionType == ActionType.TileKilled)
                            if (TileObjectData.GetTileData(Main.tile[x, y - 1]) == null)
                                WorldGen.KillTile(x, y, false, false, ServerConfig.creativeMode);
                    }
                    break;

                case ActionType.WallReplaced:
                    {
                        if (Main.tile[x, y].HasTile)
                            return false;
                    }
                    goto case ActionType.TileReplaced;

                case ActionType.TileReplaced:
                    {
                        // TODO: Move this outside of modify loop?

                        if (consumesItemType <= 0) // Invalid item type?
                            return false; //goto case ActionType.TileKilled;
                        int placeType;
                        Item item = new Item(consumesItemType);
                        if (actionType == ActionType.TileReplaced)
                            placeType = item.createTile;
                        else
                            placeType = item.createWall;
                        if (placeType < 0) // Places invalid thing?
                            return false;


                        if (!ServerConfig.creativeMode && !puppeting)
                        {
                            bool consumed = Main.player[playerID].ConsumeItem(consumesItemType);
                            if (!consumed) // Does the player have items to place?
                                return false;
                        }
                        if (actionType == ActionType.TileReplaced)
                            WorldGen.ReplaceTile(x, y, (ushort)placeType, itemSubtype <= 0 ? (int)Main.tile[x, y].Slope : itemSubtype);
                        else
                            WorldGen.ReplaceWall(x, y, (ushort)placeType);
                    }
                    break;


                case ActionType.TurfPlanted:
                    {
                        // TODO: Move this outside of modify loop?
                        if (consumesItemType <= 0) // Invalid item type?
                            return false; //goto case ActionType.TileKilled;
                        int placeType = new Item(consumesItemType).createTile;
                        if (placeType < 0) // Places invalid thing?
                            return false;


                        if (!ServerConfig.creativeMode && !puppeting)
                        {
                            bool consumed = Main.player[playerID].ConsumeItem(consumesItemType);
                            if (!consumed) // Does the player have items to place?
                                return false;
                        }
                        // TODO: Retain paint?
                        WorldGen.KillTile(x, y, true, false, true);
                        WorldGen.SpreadGrass(x, y, Main.tile[x, y].TileType, placeType, false, 0);
                        //WorldGen.PlaceTile(x, y, (ushort)consumesItemType, true, true, playerID, itemSubtype <= 0 ? (int)Main.tile[x, y].Slope : itemSubtype);
                    }
                    break;

                case ActionType.TurfHarvested:
                    {
                        if (consumesItemType < 0) // Invalid grass type?
                            return false;
                        // TODO: Retain paint?
                        WorldGen.KillTile(x, y, false, false, true);
                        WorldGen.PlaceTile(x, y, (ushort)consumesItemType, true, true, playerID, itemSubtype <= 0 ? (int)Main.tile[x, y].Slope : itemSubtype);
                    }
                    break;

                case ActionType.TilePainted:
                    {
                        WorldGen.paintTile(x, y, (byte)consumesItemType, true);
                    }
                    break;

                case ActionType.WallPainted:
                    {
                        WorldGen.paintWall(x, y, (byte)consumesItemType, true);
                    }
                    break;

                case ActionType.ClearedPaint:
                    {
                        WorldGen.paintTile(x, y, PaintID.None, true);
                        WorldGen.paintWall(x, y, PaintID.None, true);
                    }
                    break;

                default:
                    return false;
            }
            return true;
        }

        /// My sad attempt at reflection
        /*internal static void reflect(string assembly, object?[] parameters)
        {
            var paths = assembly.Split('.');
            if (paths.Length <= 0)
                return;
            typeof(ModLoader).Assembly.GetType(assembly).GetMethod(paths[paths.Length - 1], BindingFlags.Public | BindingFlags.Static).Invoke(null, parameters);
        }*/

        /// <summary>
        /// Checks if an id is valid for a given action against the whitelists and blacklists
        /// Respecting their enabled status and contents.
        /// </summary>
        /// 
        /// <param name="id">ID to translate into a string</param>
        /// <param name="type">Whitelist type of the ID being passed, black/white treated the same</param>
        /// <param name="subtype">Whitelist frameY of the ID being passed, if any</param>
        /// <returns>Translated string</returns>
        internal static bool CheckIfAllowed(int id, ActionType type, int subtype = -1)
        {
            string fullName = GetFullNameById(id, type, subtype);

            if (fullName.Length < 3)
                return false;

            switch (type)
            {
                case ActionType.TileKilled:
                    if (ServerConfig.allowPickaxing)
                        goto case ActionType.TileWhiteListed;
                    goto default;
                case ActionType.TileWhiteListed:
                case ActionType.TileBlackListed:
                    return
                        (!ClientConfig.tileWhitelistToggled || ClientConfig.tileWhitelist.Contains(fullName)) &&
                        (!ServerConfig.tileBlacklistToggled || !ServerConfig.tileBlacklist.Contains(fullName));

                case ActionType.WallKilled:
                    if (ServerConfig.allowHammering)
                        goto case ActionType.WallWhiteListed;
                    goto default;
                case ActionType.WallWhiteListed:
                case ActionType.WallBlackListed:
                    return
                        (!ClientConfig.wallWhitelistToggled || ClientConfig.wallWhitelist.Contains(fullName)) &&
                        (!ServerConfig.wallBlacklistToggled || !ServerConfig.wallBlacklist.Contains(fullName));

                case ActionType.TileReplaced:
                case ActionType.WallReplaced:
                    if (ServerConfig.allowReplace)
                        goto case ActionType.ItemWhiteListed;
                    goto default;

                case ActionType.TurfPlanted:
                    if (ServerConfig.chainSeeding)
                        goto case ActionType.ItemWhiteListed;
                    goto default;

                case ActionType.ItemWhiteListed:
                case ActionType.ItemBlackListed:
                    return
                        (!ClientConfig.itemWhitelistToggled || ClientConfig.itemWhitelist.Contains(fullName)) &&
                        (!ServerConfig.itemBlacklistToggled || !ServerConfig.itemBlacklist.Contains(fullName));

                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets the fully named string of a tile/wall/item with mod prefix.
        /// Used for writing to whitelists, as this standardizes our prefixes.
        /// </summary>
        /// 
        /// <param name="id">ID to translate into a string</param>
        /// <param name="type">Whitelist type of the ID being passed, black/white treated the same</param>
        /// <param name="subtype">Whitelist frameY of the ID being passed, if any is avaiable to be parsed</param>
        /// <returns>Translated string</returns>
        internal static string GetFullNameById(int id, ActionType type, int subtype = -1)
        {
            //Log($"GetFullNameById({id}, {type}, {subtype})");
            switch (type)
            {
                case ActionType.TileWhiteListed:
                case ActionType.TileBlackListed:
                case ActionType.TileKilled:
                    var modTile = TileLoader.GetTile(id);
                    if (modTile is not null)
                        return $"{modTile.Mod}:{modTile.Name}" + (subtype >= 0 ? $":{subtype}" : "");
                    else
                        return "Terraria:" + TileID.Search.GetName(id) + (subtype >= 0 ? $":{subtype}" : "");

                case ActionType.WallWhiteListed:
                case ActionType.WallBlackListed:
                case ActionType.WallKilled:
                    var modWall = WallLoader.GetWall(id);
                    if (modWall is not null)
                        return $"{modWall.Mod}:{modWall.Name}";
                    else
                        return "Terraria:" + WallID.Search.GetName(id);

                case ActionType.ItemWhiteListed:
                case ActionType.ItemBlackListed:
                case ActionType.TileReplaced:
                case ActionType.WallReplaced:
                case ActionType.TurfPlanted:
                    var modItem = ItemLoader.GetItem(id);
                    if (modItem is not null)
                        return $"{modItem.Mod}:{modItem.Name}";
                    else
                        return "Terraria:" + ItemID.Search.GetName(id);

                default:
                    return "";
            }
        }

        /// <summary>
        /// Gets the ID of a tile/wall/item with respect to tML loader designations.
        /// Used for reading from whitelists.
        /// </summary>
        /// 
        /// <param name="fullName">Full name to translate into an ID</param>
        /// <param name="type">Whitelist type of the ID being passed, black/white treated the same</param>
        /// <param name="subtype">Whitelist frameY of the ID being passed, if any is avaiable to be parsed</param>
        /// <returns>Translated ID. Returns negative for invalid types</returns>
        internal static int GetIdByFullName(string fullName, ActionType type, out int subtype)
        {
            subtype = -1;
            if (fullName.IndexOf(':') < 0)
                return -1;

            var names = fullName.Split(':');

            if (names[0] == "Terraria")
                names[0] = "";

            if (names.Length == 3)
                try
                {
                    subtype = Int32.Parse(names[2]);
                }
                catch (FormatException e)
                {
                    names[2] = "";
                    Log(e.Message, default, LogType.Error);
                }

            switch (type)
            {
                case ActionType.TileWhiteListed:
                case ActionType.TileBlackListed:
                case ActionType.TileKilled:
                case ActionType.TilePainted:
                case ActionType.TurfHarvested:
                    return TileID.Search.GetId(names[1]);

                case ActionType.WallWhiteListed:
                case ActionType.WallBlackListed:
                case ActionType.WallKilled:
                case ActionType.WallPainted:
                    return WallID.Search.GetId(names[1]);

                case ActionType.ItemWhiteListed:
                case ActionType.ItemBlackListed:
                case ActionType.TileReplaced:
                case ActionType.WallReplaced:
                case ActionType.TurfPlanted:
                    return ItemID.Search.GetId(names[1]);

                default:
                    return -1;
            }
        }

        /// <summary>
        /// For whatever reason, ModConfig has no native way of saving runtime changes to a config.
        /// So here I am, writing a file system to manually save changes, usually just whitelist changes.
        /// </summary>
        /// 
        /// <param name="config">The configuration object to write to storage from memory.</param>
        internal static void SaveConfig(ModConfig config)
        {
            Log($"Saving config '{config.Name}' changes...");
            string filename = config.Mod.Name + "_" + config.Name + ".json";
            string path = Path.Combine(ModConfigPath, filename);
            string json = JsonConvert.SerializeObject(config/*, serializerSettings*/);
            try
            {
                Directory.CreateDirectory(ModConfigPath);
                File.WriteAllText(path, json);
                Log($"Config '{config.Name}' saved, and updated!");
            }catch (Exception e)
            {
                throw new Exception($"A file could not be created, or updated at:"
                    + "\n'{path}'"
                    + "\n\n If you ARE using Onedrive, please reinstall tModloader in a different location."
                    + "\n If you ARE NOT using Onedrive, please disable Windows Real-Time protection.");
            }
        }

        /// <summary>
        /// Logs text to chat and/or client/server loggers.
        /// </summary>
        /// 
        /// <param name="msg">Text to write to chat and/or loggers</param>
        /// <param name="color">Color of the text in chat, color default will not output to chat</param>
        /// <param name="type">Type of message to write, type None will not output to logger files</param>
        internal static void Log(string msg, Color color = default, LogType type = LogType.Debug)
        {
            if (type != LogType.None)
                switch (type)
                {
                    case LogType.Info:
                        myMod.Logger.Info(msg);
                        break;

                    case LogType.Debug:
                        if (ClientConfig.doDebugStuff || devmode)
                            myMod.Logger.Debug(msg);
                        break;

                    case LogType.Warn:
                        myMod.Logger.Warn(msg);
                        break;

                    case LogType.Error:
                        myMod.Logger.Error(msg);
                        break;

                    case LogType.Fatal:
                        myMod.Logger.Fatal(msg);
                        break;
                }
            if (type != LogType.Debug || ClientConfig.doDebugStuff || devmode)
                if (color != default)
                {
                    // This stuff combines duplicated messages
                    try {
                        List<ChatMessageContainer> messages = (List<ChatMessageContainer>)typeof(RemadeChatMonitor).GetField("_messages", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Main.chatMonitor);
                        if (messages != null && messages.Count > 0)
                        {
                            List<TextSnippet[]> parsedText = (List<TextSnippet[]>)typeof(ChatMessageContainer).GetField("_parsedText", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(messages[0]);
                            if (parsedText[^1][0].TextOriginal.Equals(msg))
                                parsedText[^1][0].Text = parsedText[^1][0].TextOriginal + $" [{++msgReps}x]";
                            else
                            {
                                msgReps = 1;
                                Main.NewText(msg, color);
                            }
                            //Main.chatMonitor.DrawChat(true);
                        }
                        else
                            Main.NewText(msg, color);
                    } catch(Exception e) {
                        myMod.Logger.Error(e);
                        Main.NewText(msg, color);
                    }
                }
        }
    }

    internal class ExcavatorTile : GlobalTile
    {
        /// <summary>
        /// Called when the player hits a block.
        /// This handles the tile's death, and subsequently will kill other tiles under the right conditions.
        /// </summary>
        /// 
        /// <param name="x">X coordinate of the tile that was struck</param>
        /// <param name="y">Y coordinate of the tile that was struck (top of world is y = 0)</param>
        /// <param name="type">Tile ID of the tile that was struck</param>
        /// <param name="fail">Reference of if the tile that was struck was killed or not. Death strike = fail is false</param>
        /// <param name="effectOnly">Reference of if the tile was actually struck with a poweful enough pickaxe to hurt it</param>
        /// <param name="noItem">Reference of if the tile should drop not item(s)</param>
        public override void KillTile(int x, int y, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            if (WorldGen.gen || !Main.tile[x, y].HasTile || type < 0 || OreExcavator.killCalled || Main.netMode == NetmodeID.Server || Main.gameMenu)
                return;

            if (OreExcavator.ExcavateHotkey.GetAssignedKeys().Count <= 0 || !OreExcavator.ServerConfig.allowPickaxing)
                return;

            if (x != Player.tileTargetX || y != Player.tileTargetY)
                return;

            // Not essential, but helps reset the player during inactive phases
            if (OreExcavator.ClientConfig.toggleExcavations ? !OreExcavator.excavationToggled : !OreExcavatorKeybinds.excavatorHeld)
            {
                if (OreExcavator.ClientConfig.releaseCancelsExcavations)
                {
                    OreExcavator.masterTiles.Clear();
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        ModPacket packet = OreExcavator.myMod.GetPacket();
                        packet.Write((byte)ActionType.ResetExcavations);
                        packet.Send();
                    }
                    OreExcavator.playerHalted[Main.myPlayer] = false;
                }
                return;
            }

            Tile localTile = OreExcavator.lookingAtTile;

            _ = OreExcavator.masterTiles.TryRemove(new(x, y), out _);
            
            if (!OreExcavator.CheckIfAllowed(type, ActionType.TileKilled, TileObjectData.GetTileStyle(localTile)))
                return;

            /// USES DIRECT TILE - REVERT TO COPY?
            if (OreExcavator.ServerConfig.creativeMode)
                noItem = true;

            if (fail)
            {
                _ = Task.Run(async delegate // Create a new thread
                {
                    Thread.Sleep(10);
                    ushort newType = Main.tile[x, y].TileType;
                    if (type != newType)
                        OreExcavator.ModifySpooler(
                            ActionType.TurfHarvested, x, y,
                            OreExcavator.ClientConfig.recursionLimit,
                            OreExcavator.ClientConfig.recursionDelay,
                            true,
                            type,
                            (byte)Main.myPlayer,
                            false,
                            -1,
                            newType
                        );
                    OreExcavator.lookingAtTile.CopyFrom(Main.tile[x, y]);
                });
                return;
            }
            
            OreExcavator.ModifySpooler(
                ActionType.TileKilled, x, y,
                OreExcavator.ClientConfig.recursionLimit,
                OreExcavator.ClientConfig.recursionDelay,
                OreExcavator.ClientConfig.doDiagonals,
                type,
                (byte)Main.myPlayer,
                false,
                TileObjectData.GetTileStyle(localTile)
            );
            OreExcavator.lookingAtTile.CopyFrom(Main.tile[x, y]);
        }
    }

    internal class ExcavatorWall : GlobalWall
    {

        /// <summary>
        /// Called when the player hits a wall.
        /// This handles the wall's death, and subsequently will kill other walls under the right conditions.
        /// </summary>
        /// 
        /// <param name="x">X coordinate of the wall that was struck</param>
        /// <param name="y">Y coordinate of the wall that was struck (top of world is y = 0)</param>
        /// <param name="type">Tile ID of the wall that was struck</param>
        /// <param name="fail">Reference of if the wall that was struck was killed or not. Death strike = fail is false</param>
        public override void KillWall(int x, int y, int type, ref bool fail)
        {
            if (WorldGen.gen || fail || Main.tile[x, y].HasTile || type <= 0 || OreExcavator.killCalled || Main.netMode == NetmodeID.Server || Main.gameMenu)
                return;

            if (OreExcavator.ExcavateHotkey.GetAssignedKeys().Count <= 0 || !OreExcavator.ServerConfig.allowHammering)
            {
                OreExcavator.Log("Excavation Halted: No key bound to excavations.", Color.Red);
                return;
            }

            if (x != Player.tileTargetX || y != Player.tileTargetY)
                return;

            // Not essential, but helps reset the player during inactive phases
            if (OreExcavator.ClientConfig.toggleExcavations ? !OreExcavator.excavationToggled : !OreExcavatorKeybinds.excavatorHeld)
            {
                if (OreExcavator.ClientConfig.releaseCancelsExcavations)
                {
                    OreExcavator.masterTiles.Clear();
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        ModPacket packet = OreExcavator.myMod.GetPacket();
                        packet.Write((byte)ActionType.ResetExcavations);
                        packet.Send();
                    }
                    OreExcavator.playerHalted[Main.myPlayer] = false;
                }
                return;
            }

            _ = OreExcavator.masterTiles.TryRemove(new(x, y), out _);

            if (!OreExcavator.CheckIfAllowed(type, ActionType.WallKilled))
            {
                OreExcavator.Log("Excavation Halted: Whitelist checks failed.", Color.Red);
                return;
            }

            OreExcavator.ModifySpooler(
                ActionType.WallKilled, x, y,
                OreExcavator.ClientConfig.recursionLimit,
                OreExcavator.ClientConfig.recursionDelay,
                OreExcavator.ClientConfig.doDiagonals,
                type,
                (byte)Main.myPlayer,
                false
            );
            OreExcavator.lookingAtTile.CopyFrom(Main.tile[x, y]);
        }
    }

    internal class ExcavatorItem : GlobalItem
    {
        /// <summary>
        /// Called whenever a player uses an item.
        /// We use this as a hook for block swap excavations.
        /// </summary>
        /// 
        /// <param name="item">The item in question that is being used</param>
        /// <param name="player">The player using the item in question</param>
        /// <returns>True if the item did something, false if not, null for default (for use timers)</returns>
        public override bool? UseItem(Item item, Player player)
        {
            if (WorldGen.gen || Main.netMode == NetmodeID.Server || Main.gameMenu || item.pick + item.axe + item.hammer != 0)
                return null;

            if (OreExcavator.ExcavateHotkey.GetAssignedKeys().Count <= 0)// || item.Name.ToLower().Contains("seed") || item.Name.ToLower().Contains("paint"))
            {
                OreExcavator.Log("Excavation Halted: No key bound to excavations.", Color.Red);
                return null;
            }

            if (OreExcavator.ClientConfig.toggleExcavations ? !OreExcavator.excavationToggled : !OreExcavatorKeybinds.excavatorHeld)
            {
                if (OreExcavator.ClientConfig.releaseCancelsExcavations)
                {
                    OreExcavator.masterTiles.Clear();
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        ModPacket packet = OreExcavator.myMod.GetPacket();
                        packet.Write((byte)ActionType.ResetExcavations);
                        packet.Send();
                    }
                    OreExcavator.playerHalted[Main.myPlayer] = false;
                }
                return null;
            }

            int x = Player.tileTargetX, y = Player.tileTargetY;

            if (x != OreExcavator.lookingCoordX || y != OreExcavator.lookingCoordY)
            {
                OreExcavator.Log("Excavation Halted: Tile position mismatch - player is moving cursor too fast.", Color.Red);
                return null;
            }

            ActionType actionType = ActionType.None;
            short createType = -1;
            Tile localTile = OreExcavator.lookingAtTile;

            if (item.createTile >= TileID.Dirt && item.createTile >= item.createWall) // Replacing and planting
            {
                if (localTile.TileType < TileID.Dirt || !OreExcavator.ClientConfig.doSpecials)
                    return null;

                if (item.Name.ToLower().Contains("seed"))
                {
                    if (!OreExcavator.ServerConfig.chainSeeding)
                    {
                        OreExcavator.Log("Excavation Halted: Server has disabled Chain-Planting.", Color.Red);
                        return null;
                    }

                    actionType = ActionType.TurfPlanted;
                    createType = (short)item.createTile;
                }
                else
                {
                    if (!OreExcavator.ServerConfig.allowReplace)
                    {
                        OreExcavator.Log("Excavation Halted: Server has disabled Chain-Swapping.", Color.Red);
                        return null;
                    }
                    if (!localTile.HasTile || !Main.tile[x, y].HasTile)
                    {
                        OreExcavator.Log("Excavation Halted: Tile does not exist.", Color.Red);
                        return null;
                    }
                    actionType = ActionType.TileReplaced;
                    createType = (short)item.createTile;
                }
            }
            else if (item.createWall > TileID.Dirt && item.createTile < item.createWall)
            {
                if (localTile.WallType <= TileID.Dirt || !OreExcavator.ServerConfig.allowReplace || !OreExcavator.ClientConfig.doSpecials)
                {
                    OreExcavator.Log("Excavation Halted: Wall is invalid, or the server has disabled Chain-Swapping, or the client has alternative features disabled.", Color.Red);
                    return null;
                }
                actionType = ActionType.WallReplaced;
                createType = (short)item.createWall;
            }
            else if (item.Name.ToLower().Contains("paint"))
            {
                if (!OreExcavator.ServerConfig.chainPainting)
                {
                    OreExcavator.Log("Excavation Halted: Server has disabled Chain-Painting.", Color.Red);
                    return null;
                }
                _ = Task.Run(async delegate // Create a new thread
                {
                    Thread.Sleep(10);
                    if (item.Name.ToLower().Contains("brush"))
                    {
                        if (localTile.TileColor == Main.tile[x, y].TileColor)
                        {
                            OreExcavator.Log("Excavation Halted: Tile was not altered by item usage.", Color.Red);
                            return;
                        }
                        actionType = ActionType.TilePainted;
                        if (Main.tile[x, y].HasTile)
                            OreExcavator.ModifySpooler(actionType, x, y,
                                OreExcavator.ClientConfig.recursionLimit,
                                OreExcavator.ClientConfig.recursionDelay,
                                OreExcavator.ClientConfig.doDiagonals,
                                localTile.TileType,
                                (byte)Main.myPlayer,
                                false,
                                TileObjectData.GetTileStyle(localTile),
                                Main.tile[x, y].TileColor
                            );
                    }
                    else if (item.Name.ToLower().Contains("roller"))
                    {
                        if (localTile.WallColor == Main.tile[x, y].WallColor)
                        {
                            OreExcavator.Log("Excavation Halted: Tile was not altered by item usage.", Color.Red);
                            return;
                        }
                        actionType = ActionType.WallPainted;
                        if (Main.tile[x, y].WallType > 0)
                            OreExcavator.Log($"PAINT: Wall.{Main.tile[x, y].WallColor}", Color.Green);
                    }
                    else if (item.Name.ToLower().Contains("scraper"))
                    {
                        if (localTile.TileColor == Main.tile[x, y].TileColor)
                        {
                            OreExcavator.Log("Excavation Halted: Tile was not altered by item usage.", Color.Red);
                            return;
                        }
                        actionType = ActionType.ClearedPaint;
                        if (Main.tile[x, y].HasTile || Main.tile[x, y].WallType > 0)
                            OreExcavator.Log($"PAINT: All.Clear", Color.Green);
                    }
                    else
                        OreExcavator.Log("Excavation Halted: Invalid paint item detected.", Color.Red);
                });
                return null;
            }
            else
            {
                OreExcavator.Log("Excavation Halted: Invalid operation attempted.", Color.Red);
                return null;
            }

            if (createType < 0 || (actionType == ActionType.TileReplaced && createType == localTile.TileType && localTile.TileFrameY == Main.tile[x , y].TileFrameY) // is it a valid type, and is what we're looking to replace valid?
                || (actionType == ActionType.WallReplaced && createType == localTile.WallType)
                || (actionType == ActionType.TurfPlanted && createType == localTile.TileType))
                return null;

            if (OreExcavator.ExcavateHotkey.GetAssignedKeys()[0] == "Mouse1")
                if (OreExcavator.ServerConfig.allowReplace && OreExcavator.ClientConfig.doSpecials)
                {
                    OreExcavator.Log($"\nHey! {OreExcavator.myMod.Name} here!" +
                         "\nWe've detected that you're using Left Mouse for excavations. We don't recommend this, but to protect your world, we've disabled non-veinmining features! (chain-swap, etc.)" +
                        $"\nAs an alternative, we recommend using {"Right Click"} for excavations! Go ahead and try it out!" +
                         "\n\nYou can turn these features back on in the client configurations at any time after you switch your keybind off of Left Mouse.", Color.Orange, LogType.Warn);
                    OreExcavator.ClientConfig.doSpecials = false;
                    OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                    return null;
                }
            if (actionType != ActionType.WallReplaced)
                if (Main.tile[x, y] is Tile testTile && testTile.TileType == localTile.TileType && localTile.TileFrameY == testTile.TileFrameY)
                {
                    OreExcavator.Log("Excavation Halted: Tile was not altered by item usage.", Color.Red);
                    return null;
                }
                else if (Main.tile[x + 1, y].HasTile == false && Main.tile[x, y + 1].HasTile == false && Main.tile[x - 1, y].HasTile == false && Main.tile[x, y - 1].HasTile == false)
                {
                    OreExcavator.Log("Excavation Halted: Tile is unanchored, nowhere to flood to.", Color.Red);
                    return null;
                }

            string itemName = OreExcavator.GetFullNameById(item.type, actionType);

            if (OreExcavator.ClientConfig.itemWhitelistToggled && !OreExcavator.ClientConfig.itemWhitelist.Contains(itemName))
            {
                OreExcavator.Log($"Rejected chain-swapping 'ItemID.{item.Name} ({item.netID})' because it isn't whitelisted by you", Color.Orange, LogType.Warn);
                return null;
            }
            if (OreExcavator.ServerConfig.itemBlacklistToggled && OreExcavator.ServerConfig.itemBlacklist.Contains(itemName))
            {
                OreExcavator.Log($"Rejected chain-swapping 'ItemID.{item.Name} ({item.netID})' because it's blacklisted by the server", Color.Orange, LogType.Warn);
                return null;
            }

            OreExcavator.ModifySpooler(
                actionType, x, y,
                OreExcavator.ClientConfig.recursionLimit,
                OreExcavator.ClientConfig.recursionDelay,
                (actionType == ActionType.TurfPlanted ? true : OreExcavator.ClientConfig.doDiagonals),
                (actionType == ActionType.WallReplaced ? localTile.WallType : localTile.TileType),
                (byte)Main.myPlayer,
                false,
                TileObjectData.GetTileStyle(localTile),
                item.type,
                item.placeStyle
            );
            OreExcavator.lookingAtTile.CopyFrom(Main.tile[x, y]);

            return true;
        }

        public override bool? CanBurnInLava(Item item)
        {
            if (item.noWet && OreExcavator.ServerConfig.safeItems)
                return false;
            return base.CanBurnInLava(item);
        }

        /// <summary>
        /// Called when an item is created
        /// Used to update items created by excavations
        /// </summary>
        /// 
        /// <param name="item">The item in question being spawned</param>
        /// <param name="source">Source of the spawned item</param>
        public override void OnSpawn(Item item, IEntitySource source)
        {
            if (WorldGen.gen)
                return;
            try
            {
                //OreExcavator.Log($"OnSpawn('{item.Name}', '{(source != null ? source.Context : "")}') Places: {item.createTile}, {item.createWall}", Color.Red);
                if (item != null && item.active && (source == null || source.Context == null || source.Context == "") && (item.createTile != -1 || item.createWall != -1))
                {
                    if (OreExcavator.ServerConfig.teleportLoot) // Add check for initial drop from excavations
                        if (OreExcavator.killCalled || OreExcavator.puppeting)
                        {
                            item.noGrabDelay = 0;
                            float closestDistance = -1f;
                            byte closestPlayer = 0;
                            for (short index = 0; index < Main.player.Length; index++)
                            {
                                if (OreExcavator.playerHalted[index])
                                    continue;
                                if (!Main.player[index].active)
                                    continue;

                                float distance = item.position.Distance(Main.player[index].position);
                                if (distance < closestDistance || closestDistance < 0f)
                                {
                                    closestDistance = distance;
                                    closestPlayer = (byte)index;
                                }
                            }
                            if (closestDistance >= 0f && closestDistance < 900)
                            {
                                item.position = Main.player[closestPlayer].position;
                                //item.position.Y += 21f;
                            }
                            if (OreExcavator.ServerConfig.safeItems)
                                item.noWet = true;

                            if (OreExcavator.ServerConfig.creativeMode)
                                item.active = false;
                        }
                }
            }
            catch { } // Because I don't trust my code as far as it can throw 
            base.OnSpawn(item, source);
        }

        /// <summary>
        /// Called on initial load
        /// Used to add control tips to items that are considered for excavation
        /// </summary>
        /// 
        /// <param name="item">The item in question being modified</param>
        /// <param name="tooltips">Tooltips list of the provided item to append to or alter</param>
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if ((item.pick > 0 && OreExcavator.ServerConfig.allowPickaxing)
                || (item.hammer > 0 && OreExcavator.ServerConfig.allowHammering)
                || (OreExcavator.ServerConfig.allowReplace && OreExcavator.ClientConfig.doSpecials))
            {
                string name = "";
                if (item.createTile >= 0.0 || item.createWall > 0.0)
                    name = OreExcavator.GetFullNameById(item.type, ActionType.ItemWhiteListed);

                if (name == "" && item.pick == 0 && item.hammer == 0)
                    return;

                if (OreExcavator.ExcavateHotkey.GetAssignedKeys().Count > 0 && OreExcavator.ClientConfig.showItemTooltips)
                {
                    string keybind = OreExcavator.ExcavateHotkey.GetAssignedKeys()[0];
                    if (keybind.StartsWith("Oem"))
                        keybind = keybind.Substring(3);
                    else if (keybind == "Mouse2")
                        keybind = "Right Click";
                    else if (keybind == "Mouse1")
                        keybind = "Left Click";

                    if ((item.pick != 0 && OreExcavator.ServerConfig.allowPickaxing) && (item.hammer != 0 && OreExcavator.ServerConfig.allowHammering))
                        tooltips.Add(new TooltipLine(OreExcavator.myMod, "HowToUse", $"Hold '{keybind}' to Excavate blocks or walls!"));
                    else if ((item.pick != 0 && OreExcavator.ServerConfig.allowPickaxing))
                        tooltips.Add(new TooltipLine(OreExcavator.myMod, "HowToUse", $"Hold '{keybind}' to Excavate blocks!"));
                    else if ((item.hammer != 0 && OreExcavator.ServerConfig.allowHammering))
                        tooltips.Add(new TooltipLine(OreExcavator.myMod, "HowToUse", $"Hold '{keybind}' to Excavate walls!"));
                    else if (name != "")
                    {
                        if (OreExcavator.ServerConfig.itemBlacklistToggled && OreExcavator.ServerConfig.itemBlacklist.Contains(OreExcavator.GetFullNameById(item.type, ActionType.ItemBlackListed)))
                            tooltips.Add(new TooltipLine(OreExcavator.myMod, "HowToUse", $"Server host has blacklisted this item for chain-{(item.Name.ToLower().Contains("seed") ? "planting" : "swapping")}"));
                        else if (OreExcavator.ClientConfig.itemWhitelist.Contains(OreExcavator.GetFullNameById(item.type, ActionType.ItemWhiteListed)))
                            tooltips.Add(new TooltipLine(OreExcavator.myMod, "HowToUse", $"Hold '{keybind}' while placing to chain-{(item.Name.ToLower().Contains("seed") ? "plant" : "swap")}!"));
                        else if (OreExcavator.WhitelistHotkey.GetAssignedKeys().Count > 0)
                        {
                            string whitelistKey = OreExcavator.WhitelistHotkey.GetAssignedKeys()[0];
                            if (keybind.StartsWith("Oem"))
                                whitelistKey = whitelistKey.Substring(3);
                            else if (whitelistKey == "Mouse2")
                                whitelistKey = "Right Click";
                            else if (whitelistKey == "Mouse1")
                                whitelistKey = "Left Click";
                            tooltips.Add(new TooltipLine(OreExcavator.myMod, "HowToUse", $"Hover and press '{whitelistKey}' to whitelist for chain-{(item.Name.ToLower().Contains("seed") ? "planting" : "swapping")}!"));
                        }
                    }
                        
                }
                else if (OreExcavator.ExcavateHotkey.GetAssignedKeys().Count <= 0)
                    if ((item.pick > 0 && OreExcavator.ServerConfig.allowPickaxing) || (item.hammer > 0 && OreExcavator.ServerConfig.allowHammering))
                        tooltips.Add(new TooltipLine(OreExcavator.myMod, "HowToUse", "No keybind set, please set one in your control settings to start Excavating!"));
                    else if (name != "")
                        tooltips.Add(new TooltipLine(OreExcavator.myMod, "HowToUse", $"No keybind set, please set one in your control settings to start chain-{(item.Name.ToLower().Contains("seed") ? "planting" : "swapping")}!"));
            }
        }

        public override bool CanUseItem(Item item, Player player)
        {
            int x = Player.tileTargetX, y = Player.tileTargetY;

            if (Main.netMode == NetmodeID.Server || !OreExcavator.ServerConfig.allowReplace)
                return true;

            if (OreExcavator.lookingCoordX != x || OreExcavator.lookingCoordY != y)
                OreExcavator.lookingAtTile.CopyFrom(Main.tile[x, y]);

            if (OreExcavator.lookingCoordX != x)
                OreExcavator.lookingCoordX = (ushort)x;
            if (OreExcavator.lookingCoordY != y)
                OreExcavator.lookingCoordY = (ushort)y;

            return true;
        }

        /// <summary>
        /// Called when the game wants to know if we can right-click with a given item.
        /// Used to call our async handler so we can manually add auto-swing to picks without caring about if they can be used.
        /// </summary>
        /// 
        /// <param name="item">The item in question that is trying to be used</param>
        /// <param name="player">The player using the item in question</param>
        /// <returns></returns>
        public override bool AltFunctionUse(Item item, Player player)
        {
            if (Main.netMode != NetmodeID.Server &&
                (OreExcavatorKeybinds.excavatorHeld || OreExcavator.excavationToggled) &&
                OreExcavator.ExcavateHotkey.GetAssignedKeys().Count > 0 &&
                OreExcavator.ExcavateHotkey.GetAssignedKeys()[0] == "Mouse2")
                RepeatHandler(item, player);
            return base.AltFunctionUse(item, player); // Restore the original use(s) (recovers things like summon staffs)
        }

        /// <summary>
        /// Called manually to determine if we should be continually swinging.
        /// This is here so we can use async tasks easily.
        /// This checks if the correct inputs are being held and whitelisting is approved,
        /// and issues the actions to take if so, it also sets a followup task after the item's defined cooldown timer expires.
        /// </summary>
        /// 
        /// <param name="item">The item in question</param>
        /// <param name="player">The player using the item in question</param>
        internal void RepeatHandler(Item item, Player player)
        {
            // Get the player's traget tile
            int x = Player.tileTargetX;
            int y = Player.tileTargetY;
            
            _ = Task.Run(async delegate // Create a new thread
            {
                /// I can see the hordes of angry steam comments from here. Ready the battlements
                if ((OreExcavator.ServerConfig.allowPickaxing && item.pick > 0.0) ||
                    (OreExcavator.ServerConfig.allowHammering && item.hammer > 0.0) ||
                    (OreExcavator.ServerConfig.allowReplace && item.createTile >= 0.0) ||
                    (OreExcavator.ServerConfig.allowReplace && item.createWall > 0.0))
                    if (OreExcavatorKeybinds.excavatorHeld && TileObjectData.GetTileData(Main.tile[x, y - 1]) == null)
                        if ((item.pick > 0.0 && (!OreExcavator.ClientConfig.inititalChecks || (OreExcavator.ClientConfig.tileWhitelist.Contains(OreExcavator.GetFullNameById(Main.tile[x, y].TileType, ActionType.TileWhiteListed)) && Main.tile[x, y].HasTile))) ||
                            (item.hammer > 0.0 && (!OreExcavator.ClientConfig.inititalChecks || OreExcavator.ClientConfig.wallWhitelist.Contains(OreExcavator.GetFullNameById(Main.tile[x, y].WallType, ActionType.WallWhiteListed)))) ||
                           ((item.createTile >= 0.0 || item.createWall >= 0.0) && (!OreExcavator.ClientConfig.inititalChecks || OreExcavator.ClientConfig.itemWhitelist.Contains(OreExcavator.GetFullNameById(item.type, ActionType.ItemWhiteListed)))))
                        {
                            player.controlUseItem = true; // Yes, we're using the item normally, even though we're really not
                            player.ItemCheck(player.selectedItem); // Run the item's normal actions on whatever we're holding
                            player.controlUseItem = false; // Yes, we're using the item normally, even though we're really not
                            //await Task.Delay(item.useTime * 13);
                            Thread.Sleep(item.useTime * 10);
                            RepeatHandler(item, player); // Start the next check
                        }
            });
        }
    }

    internal class ExcavatorPlayer : ModPlayer
    {
        public override void OnEnterWorld(Player player) // Startup message
        {
            if (OreExcavator.ExcavateHotkey.GetAssignedKeys().Count <= 0)
            {
                new Task(delegate
                {
                    Thread.Sleep(2000);
                    OreExcavator.Log($"[{OreExcavator.myMod.DisplayName}] - v{OreExcavator.myMod.Version}" +
                                     "\n\t  We noticed you don't have a keybind set for the mod! " +
                                     "\n\t  The mod won't work without one, so be sure it's bound before reporting bugs." +
                                     "\n\t  You can find bindings for mods @ Settings > Controls > Mod Controls (at the bottom) > OreExcavator: Excavate", Color.Red, LogType.Warn);
                }).Start();
            }
            else if (OreExcavator.ClientConfig.showWelcome070)
            {
                new Task(delegate
                {
                    Thread.Sleep(2000);
                    OreExcavator.Log($"[{OreExcavator.myMod.DisplayName}] - v{OreExcavator.myMod.Version}" +
                                     "\n\t  We now have a discord for bug reporting:" +
                                     "\n\t  https://discord.gg/FtrsRtPe6h" +
                                     "\n\n\t  We released an update to combat some of the major dupes." +
                                     "\n\t  We've hit version 0.7.0, which means we're 70% of the way to being fully released." +
                                     "\n\t  We've also finally released chain painting, planting, and harvesting!" +
                                     "\n\t  Thanks for supporting us and being patient with us!" +
                                     "\n\n\t  Oh yeah, you can also disable this popup in your Client configs.", Color.SteelBlue, LogType.Info);
                }).Start();
            }
            else
            {
                // Add new update available logger?
            }
        }
    }
}