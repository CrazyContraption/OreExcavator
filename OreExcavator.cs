using Microsoft.Xna.Framework;
//using MonoMod.Cil;                   // IL
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
using Terraria.Localization;
using Terraria.ModLoader;            // Modloader
using Terraria.ModLoader.Config;
using Terraria.ObjectData;
using Terraria.UI.Chat;

namespace OreExcavator /// The Excavator of ores
{
    public partial class OreExcavator : Mod
    {
        internal static ModKeybind ExcavateHotkey;
        internal static ModKeybind WhitelistHotkey;
        internal static ModKeybind BlacklistHotkey;

        internal static OreExcavatorConfig_Client ClientConfig = ModContent.GetInstance<OreExcavatorConfig_Client>();
        internal static OreExcavatorConfig_Server ServerConfig = ModContent.GetInstance<OreExcavatorConfig_Server>();
        internal static readonly string ModConfigPath = Path.Combine(Main.SavePath, "ModConfigs");

        internal static ConcurrentDictionary<Point16, bool> masterTiles = new();
        internal static ConcurrentDictionary<int?, Task> alterTasks = new();
        internal static ConcurrentQueue<Alteration> alterQueue = new();

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
        internal static bool hostOnly = false;

        /// <summary>
        /// Called by tML when the mod is asked to load.
        /// This binds various important aspects of the mod.
        /// </summary>
        public override void Load()
        {
            ExcavateHotkey  = KeybindLoader.RegisterKeybind(this, Language.GetTextValue("Mods.OreExcavator.KeyBinds.Excavate"), "OemTilde");
            WhitelistHotkey = KeybindLoader.RegisterKeybind(this, Language.GetTextValue("Mods.OreExcavator.KeyBinds.Whitelist"), "Insert");
            BlacklistHotkey = KeybindLoader.RegisterKeybind(this, Language.GetTextValue("Mods.OreExcavator.KeyBinds.UnWhitelist"), "Delete");

            // Detours
            On.Terraria.WorldGen.SpawnFallingBlockProjectile += Detour_SpawnFallingBlockProjectile;
            //On.Terraria.WorldGen.CheckOrb += WorldGen_CheckOrb;
            //On.Terraria.WorldGen.CheckPot += WorldGen_CheckPot;
            On.Terraria.Main.Update += Detour_Update;

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

        private void Detour_Update(On.Terraria.Main.orig_Update orig, Main self, GameTime gameTime)
        {
            while (!alterQueue.IsEmpty)
            {
                alterQueue.TryDequeue(out Alteration alteration);
                if (alteration != null)
                {
                    bool taskIsAlive = TaskOkay(alteration.threadID);
                    if (taskIsAlive)
                    {
                        killCalled = true;
                        bool fatal = Alteration.DoAlteration(alteration);
                        killCalled = false;

                        if (fatal)
                        {
                            Log($"Excavation killed - Fatal response (#{alteration.threadID})", Color.Orange);
                            _ = alterTasks.TryRemove(alteration.threadID, out _);
                        }
                    }
                }
            }
            orig(self, gameTime);
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

            return orig(x, y, tileCache, tileTopCache, tileBottomCache, type);
        }

        private void WorldGen_CheckOrb(On.Terraria.WorldGen.orig_CheckOrb orig, int i, int j, int type)
        {
            if (puppeting || killCalled)
                return;
            orig(i, j, type);
        }

        private void WorldGen_CheckPot(On.Terraria.WorldGen.orig_CheckPot orig, int i, int j, int type)
        {
            if (puppeting || killCalled)
                return;
            orig(i, j, type);
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
            Log(Language.GetTextValue("Mods.OreExcavator.Logging.OreSearch.Start"), default, LogType.Info);
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
                        Log(Language.GetTextValue("Mods.OreExcavator.Logging.OreSearch.Added", newName, name), default, LogType.Info);
                        ClientConfig.tileWhitelist.Add(name);
                    }
                    else
                        Log(Language.GetTextValue("Mods.OreExcavator.Logging.OreSearch.Found", newName, name), default, LogType.Info);
                }
            }
            SaveConfig(ClientConfig);
            Log(Language.GetTextValue("Mods.OreExcavator.Logging.OreSearch.Finish"), default, LogType.Info);
        }

        internal static bool TaskOkay(int? id)
        {
            if (id == null)
            {
                Log("Attempted lookup for a task that doesn't exist!", default, LogType.Warn);
                return false;
            }

            if (alterTasks.TryGetValue(id, out Task task)) // Excavation exists!
            {
                if (task.IsCompleted) // Excavation was halted(task no longer running)
                {
                    //Log($"Excavation was killed - Thread closed (#{id}), allowing one alteration", Color.Orange);
                    _ = alterTasks.TryRemove(id, out _);
                    return true;
                }
                // Excavation is running still, IDs will always match
                //Log($"Excavation is running! (#{id})", Color.Green);
                return true;
            }
            //Log($"Excavation found dead (#{id})", Color.Red, LogType.None);
            return false;
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

                case ActionType.ClearedPaint:
                case ActionType.TileKilled:
                case ActionType.WallKilled:
                    {
                        playerHalted[origin] = false;
                        ushort x = reader.ReadUInt16(), y = reader.ReadUInt16();
                        byte delay = reader.ReadByte();
                        int limit = reader.ReadInt32();
                        bool doDiagonals = reader.ReadBoolean();
                        ushort targetType = reader.ReadUInt16();
                        byte targetColor = reader.ReadByte();
                        int targetSubtype = reader.ReadInt32();
                        ModifySpooler(msgType, x, y, delay, limit, doDiagonals, origin, targetType, targetColor, targetSubtype, -1, -1, true);
                    }
                    break;

                case ActionType.ExtendPlacement:
                case ActionType.TileReplaced:
                case ActionType.WallReplaced:
                case ActionType.TilePlaced:
                case ActionType.WallPlaced:
                    {
                        playerHalted[origin] = false;
                        ushort x = reader.ReadUInt16(), y = reader.ReadUInt16();
                        byte delay = reader.ReadByte();
                        int limit = reader.ReadInt32();
                        bool doDiagonals = reader.ReadBoolean();
                        ushort targetType = reader.ReadUInt16();
                        byte targetColor = reader.ReadByte();
                        int targetSubtype = reader.ReadInt32(), replaceType = reader.ReadInt32(), replaceSubtype = reader.ReadInt32();
                        ModifySpooler(msgType, x, y, delay, limit, doDiagonals, origin, targetType, targetColor, targetSubtype, replaceType, replaceSubtype, true);
                    }
                    break;

                case ActionType.TurfHarvested:
                case ActionType.TurfPlanted:
                case ActionType.TilePainted:
                case ActionType.WallPainted:
                    {
                        playerHalted[origin] = false;
                        ushort x = reader.ReadUInt16(), y = reader.ReadUInt16();
                        byte delay = reader.ReadByte();
                        int limit = reader.ReadInt32();
                        bool doDiagonals = reader.ReadBoolean();
                        ushort targetType = reader.ReadUInt16();
                        byte targetColor = reader.ReadByte();
                        int targetSubtype = reader.ReadInt32(), replaceType = reader.ReadInt32();
                        ModifySpooler(msgType, x, y, delay, limit, doDiagonals, origin, targetType, targetColor, targetSubtype, replaceType, -1, true);
                    }
                    break;

                case ActionType.TileWhiteListed:
                case ActionType.TileBlackListed:
                case ActionType.WallWhiteListed:
                case ActionType.WallBlackListed:
                case ActionType.ItemWhiteListed:
                case ActionType.ItemBlackListed:
                    {
                        ushort id = reader.ReadUInt16();
                        int subtype = reader.ReadInt32();

                        if (Main.netMode == NetmodeID.MultiplayerClient)
                            switch (msgType)
                            {
                                case ActionType.TileWhiteListed:
                                    
                                    Log(Language.GetTextValue("Mods.OreExcavator.Network.Added", Main.player[origin].name, "Tile", $"{GetFullNameById(id, msgType, subtype)} ({id})"),
                                        Color.Green, LogType.Info);
                                    break;

                                case ActionType.WallWhiteListed:
                                    Log(Language.GetTextValue("Mods.OreExcavator.Network.Added", Main.player[origin].name, "Wall", $"{GetFullNameById(id, msgType, subtype)} ({id})"),
                                        Color.Orange, LogType.Info);
                                    break;

                                case ActionType.ItemWhiteListed:
                                    Log(Language.GetTextValue("Mods.OreExcavator.Network.Added", Main.player[origin].name, "Item", $"{GetFullNameById(id, msgType, subtype)} ({id})"),
                                        Color.Green, LogType.Info);
                                    break;

                                case ActionType.TileBlackListed:
                                    Log(Language.GetTextValue("Mods.OreExcavator.Network.Removed", Main.player[origin].name, "Tile", $"{GetFullNameById(id, msgType, subtype)} ({id})"),
                                        Color.Orange, LogType.Info);
                                    break;

                                case ActionType.WallBlackListed:
                                    Log(Language.GetTextValue("Mods.OreExcavator.Network.Removed", Main.player[origin].name, "Wall", $"{GetFullNameById(id, msgType, subtype)} ({id})"),
                                        Color.Green, LogType.Info);
                                    break;

                                case ActionType.ItemBlackListed:
                                    Log(Language.GetTextValue("Mods.OreExcavator.Network.Removed", Main.player[origin].name, "Item", $"{GetFullNameById(id, msgType, subtype)} ({id})"),
                                        Color.Orange, LogType.Info);
                                    break;
                            }
                        else
                        {
                            packet.Write((byte)msgType);
                            packet.Write(id);
                            packet.Write(subtype);
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
        /// <param name="playerId"></param>
        /// <param name="puppeting"></param>
        /// <param name="replacementType"></param>
        /// <param name="replacementSubtype"></param>
        internal static void ModifySpooler(ActionType actionType, ushort x, ushort y, byte delay, int limit, bool doDiagonals, byte playerId, ushort targetType, byte targetColor = 0, int targetSubtype = -1, int replacementType = -1, int replacementSubtype = -1, bool puppeting = false)
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
                //Log(playerId + " SENDING " + actionType);
                ModPacket packet = myMod.GetPacket();
                if (Main.netMode == NetmodeID.Server)
                    packet.Write((byte)playerId);
                packet.Write((byte)actionType);
                packet.Write((ushort)x);
                packet.Write((ushort)y);
                packet.Write((byte)delay);
                packet.Write((int)limit);
                packet.Write((bool)doDiagonals);
                packet.Write((ushort)targetType);
                packet.Write((byte)targetColor);
                packet.Write((int)targetSubtype);
                switch (actionType)
                {
                    case ActionType.ExtendPlacement:
                    case ActionType.TileReplaced:
                    case ActionType.WallReplaced:
                    case ActionType.TilePlaced:
                    case ActionType.WallPlaced:
                        packet.Write((int)replacementType);
                        packet.Write((int)replacementSubtype);
                        break;

                    case ActionType.TurfHarvested:
                    case ActionType.TurfPlanted:
                    case ActionType.TilePainted:
                    case ActionType.WallPainted:
                        packet.Write((int)replacementType);
                        break;
                }
                packet.Send(-1, playerId);
            }

            if (Main.netMode == NetmodeID.SinglePlayer || (Main.netMode == NetmodeID.MultiplayerClient && !puppeting))
                if (x == Player.tileTargetX && y == Player.tileTargetY)
                    lookingAtTile.CopyFrom(Main.tile[x, y]);

            Task task = new Task(delegate
            {
                OreExcavator.puppeting = puppeting;
                ModifyAdjacentFloodfill(actionType, x, y, limit, delay, doDiagonals, playerId, targetType, targetColor, targetSubtype, replacementType, replacementSubtype);
            });
            _ = alterTasks.TryAdd(task.Id, task);
            task.Start();
        }

        internal static void ModifySpooler(ActionType actionType, ushort x, ushort y, byte delay, int limit, bool doDiagonals, byte playerId, Tile targetTile, int replacementType = -1, int replacementSubtype = -1, bool puppeting = false)
        {
            ModifySpooler(
                actionType,
                x,
                y,
                delay,
                limit,
                doDiagonals,
                playerId,
                targetTile.HasTile ? targetTile.TileType : targetTile.WallType,
                targetTile.TileColor > PaintID.None ? targetTile.TileColor : targetTile.WallColor,
                TileObjectData.GetTileStyle(targetTile),
                replacementType,
                replacementSubtype,
                puppeting
            );
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
        /// <param name="itemType">If the action is replacing, what are we replacing the entity with</param>
        /// <param name="itemSubtype">ID of the item style that is being replaced with, if needed</param>
        internal static void ModifyAdjacentFloodfill(ActionType actionType, ushort originX, ushort originY, int limit, byte delay, bool doDiagonals, byte playerID, int targetType, byte targetColor = PaintID.None, int targetSubtype = -1, int itemType = -1, int itemSubtype = -1)
        {
            bool isWallBounded = false;

            switch (actionType)
            {
                case ActionType.WallKilled:
                case ActionType.WallPlaced:
                case ActionType.WallPainted:
                case ActionType.WallReplaced:
                    isWallBounded = true;
                    break;

                case ActionType.ClearedPaint:
                case ActionType.ExtendPlacement:
                    isWallBounded = !Main.tile[originX, originY].HasTile;
                    break;
            }
            
            bool isFallingType = false;
            bool passReplacementTypes = false;

            int hardlimit = Math.Min(ClientConfig.recursionLimit, ServerConfig.recursionLimit);
            if (limit <= 0 || hardlimit <= 0)
                return;

            switch (actionType)
            {
                case ActionType.TurfHarvested:
                    passReplacementTypes = true;
                    break;

                case ActionType.ExtendPlacement:
                case ActionType.TurfPlanted:
                case ActionType.TilePainted:
                case ActionType.WallPainted:
                    passReplacementTypes = true;
                    limit = Math.Min(Main.player[playerID].CountItem(itemType) + (Main.mouseItem.netID == itemType ? Main.mouseItem.stack : 0), hardlimit);
                    break;

                case ActionType.TileReplaced:
                case ActionType.WallReplaced:
                case ActionType.TilePlaced:
                case ActionType.WallPlaced:
                    passReplacementTypes = true;
                    limit = Math.Min(Main.player[playerID].CountItem(itemType) + (Main.mouseItem.netID == itemType ? Main.mouseItem.stack : 0), hardlimit);
                    isFallingType = !isWallBounded && TileID.Sets.Falling[targetType] || (ModContent.TileType<ModTile>() > 0 && TileID.Sets.Falling[ModContent.TileType<ModTile>()]);
                    break;
            }

            bool doOrigin = !passReplacementTypes;
            if (puppeting)
                doOrigin = false;

            bool isRope = false;
            if (actionType == ActionType.ExtendPlacement)
            {
                Item item = new Item(itemType);
                if (item.Name.Contains("Rope")) // TODO: Update for vines
                    isRope = true;
            }

            sbyte[] ch_x = new sbyte[] { -1,  1,  0,  0,  1,  1, -1, -1 };
            sbyte[] ch_y = new sbyte[] {  0,  0, 1,  -1, -1,  1,  1, -1 };

            if (actionType == ActionType.ExtendPlacement && !isWallBounded)
                limit = Math.Min(100, limit); // Ensure that placements don't get out of hand

            Log($"(ID:{playerID} - P:{puppeting}) {actionType}, OX:{originX}, OY:{originY}, L:{limit}, D:{delay}, DD:{doDiagonals}, TT:{targetType}:{targetSubtype}({targetColor})[{(isWallBounded ? "W" : "T" )}], RT:{itemType}:{itemSubtype} F:{isFallingType}", Color.Yellow);

            Queue<Point16> queue = new();
            queue.Enqueue(new Point16(originX, originY));

            for (int tileCount = 0; queue.Count > 0 && tileCount < limit && !Main.gameMenu; tileCount++)
            {
                Point16 currentPoint = queue.Dequeue();

                if (currentPoint.X != originX || currentPoint.Y != originY || doOrigin)
                    if (masterTiles.TryAdd(new Point16(currentPoint.X, currentPoint.Y), false))
                    {
                        if (TaskOkay(Task.CurrentId))
                        {
                            alterQueue.Enqueue(new Alteration(Task.CurrentId, actionType, currentPoint.X, currentPoint.Y, playerID, puppeting, (passReplacementTypes ? itemType : -1), (passReplacementTypes ? itemSubtype : -1)));
                            _ = masterTiles.TryRemove(new Point16(currentPoint.X, currentPoint.Y), out _);
                        }
                        else
                        {
                            _ = masterTiles.TryRemove(new Point16(currentPoint.X, currentPoint.Y), out _);
                            break;
                        }
                    }
                    else
                        continue;

                if (delay > 0)
                    Thread.Sleep(delay);

                for (byte index = (byte)(isRope ? 2 : 0); index < (actionType == ActionType.ExtendPlacement ? (isRope ? 4 : 2) : (doDiagonals ? 8: 4)); index++)
                {
                    Point16 nextPoint = new(currentPoint.X + ch_x[index], currentPoint.Y + ch_y[index]);

                    ushort x = (ushort)nextPoint.X;
                    ushort y = (ushort)nextPoint.Y;

                    if (WorldGen.InWorld(x, y) && !queue.Contains(nextPoint) && !masterTiles.ContainsKey(nextPoint) && Framing.GetTileSafely(nextPoint) is Tile checkTile)
                        if (actionType == ActionType.ExtendPlacement)
                        {
                            if (!checkTile.HasTile)
                            {
                                queue.Enqueue(nextPoint);
                                if (checkTile.TileType != Main.tile[originX, originY].TileType)
                                    Dust.NewDustPerfect(new Vector2(x * 16, y * 16), DustID.Teleporter);
                            }
                        }
                        else if (!isWallBounded && WorldGen.CanKillTile(x, y) && checkTile.HasTile && checkTile.TileType == targetType)
                            switch (actionType)
                            {
                                case ActionType.TileReplaced:
                                case ActionType.TileKilled:
                                case ActionType.TilePainted:
                                case ActionType.ClearedPaint:
                                    {
                                        if (targetSubtype < 0 || targetSubtype == TileObjectData.GetTileStyle(checkTile)) // No subtypes or the subtypes match
                                            goto default;
                                    }
                                    continue;

                                case ActionType.TurfPlanted:
                                    if (!Main.tile[x, y - 1].HasTile || !Main.tile[x, y + 1].HasTile || !Main.tile[x - 1, y].HasTile || !Main.tile[x + 1, y].HasTile)
                                        if (checkTile.TileType != Main.tile[originX, originY].TileType)
                                            goto default;
                                    continue;

                                default:
                                    if (checkTile.TileColor == targetColor)
                                    {
                                        queue.Enqueue(nextPoint);
                                        if (!ClientConfig.reducedEffects)
                                            Dust.NewDustPerfect(new Vector2(x * 16, y * 16), DustID.Teleporter);
                                    }
                                    continue;
                            }
                        else if (isWallBounded && (!Main.tile[x, y].HasTile || Main.tile[x, y].Slope != SlopeType.Solid) && Main.tile[x, y].WallType == targetType)
                            if (checkTile.WallColor == targetColor)
                            {
                                queue.Enqueue(nextPoint);
                                if (!ClientConfig.reducedEffects)
                                    Dust.NewDustPerfect(new Vector2(x * 16, y * 16), DustID.Teleporter);
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
                //Log(playerID + " HALTED");
                if (Main.netMode == NetmodeID.MultiplayerClient && playerID == Main.myPlayer)
                {
                    ModPacket packet = myMod.GetPacket();
                    packet.Write((byte)ActionType.HaltExcavations);
                    packet.Send();
                    //Log(playerID + " SENDING " + ActionType.HaltExcavations);
                }

                Thread.Sleep(delay + (Math.Max(delay, (ushort)2) / 2));

                while (queue.Count > 0)
                {
                    WorldGen.TileFrame(queue.Peek().X, queue.Peek().Y);
                    _ = masterTiles.TryRemove(queue.Dequeue(), out _);
                }

                if (Main.netMode == NetmodeID.MultiplayerClient && playerID == Main.myPlayer)
                {
                    ModPacket packet = myMod.GetPacket();
                    packet.Write((byte)ActionType.ResetExcavations);
                    packet.Send();
                    //Log(playerID + " SENDING " + ActionType.ResetExcavations);
                }
                playerHalted[playerID] = false;
            }

            queue.Clear(); // Clear the queue

            puppeting = false;

            //Log(playerID + " EXCAVATION COMPLETED");
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
        /// <param name="checkClient">Should checks be done against the local whitelist too?</param>
        /// <returns>Translated string</returns>
        internal static bool CheckIfAllowed(int id, ActionType type, int subtype = -1, bool checkClient = true)
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
                    if (subtype >= 0)
                    {
                        bool baseIsAllowed = CheckIfAllowed(id, type, -1, false);
                        if (!baseIsAllowed)
                            return false;
                    }
                    return
                        (!checkClient || !ClientConfig.tileWhitelistToggled || ClientConfig.tileWhitelist.Contains(fullName)) &&
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
        internal static string? GetFullNameById(int id, ActionType type, int subtype = -1)
        {
            switch (type)
            {
                case ActionType.TileWhiteListed:
                case ActionType.TileBlackListed:
                case ActionType.TileKilled:
                    var modTile = TileLoader.GetTile(id);
                    if (modTile is not null)
                        return $"{modTile.Mod}:{modTile.Name}" + (subtype >= 0 ? $":{subtype}" : "");
                    else if (id < TileID.Count)
                        return "Terraria:" + TileID.Search.GetName(id) + (subtype >= 0 ? $":{subtype}" : "");
                    else
                        return null;

                case ActionType.WallWhiteListed:
                case ActionType.WallBlackListed:
                case ActionType.WallKilled:
                    var modWall = WallLoader.GetWall(id);
                    if (modWall is not null)
                        return $"{modWall.Mod}:{modWall.Name}";
                    else if (id < WallID.Count)
                        return "Terraria:" + WallID.Search.GetName(id);
                    else
                        return null;

                case ActionType.ItemWhiteListed:
                case ActionType.ItemBlackListed:
                case ActionType.TileReplaced:
                case ActionType.WallReplaced:
                case ActionType.TurfPlanted:
                    var modItem = ItemLoader.GetItem(id);
                    if (modItem is not null)
                        return $"{modItem.Mod}:{modItem.Name}";
                    else if (id < ItemID.Count)
                        return "Terraria:" + ItemID.Search.GetName(id);
                    else
                        return null;

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
            if (Main.netMode == NetmodeID.Server)
                if (Main.CurrentFrameFlags.ActivePlayersCount > 1)
                    hostOnly = false;

            Log($"Saving config '{config.Name}' changes...");
            string filename = config.Mod.Name + "_" + config.Name + ".json";
            string path = Path.Combine(ModConfigPath, filename);
            string json = JsonConvert.SerializeObject(config/*, serializerSettings*/);
            try
            {
                Directory.CreateDirectory(ModConfigPath);
                File.WriteAllText(path, json);
                Log($"Config '{config.Name}' saved, and updated!");
            }catch (Exception)
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
            if (Main.netMode != NetmodeID.Server && (type != LogType.Debug || ClientConfig.doDebugStuff || devmode))
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
        /// <param name="oldType">Tile ID of the tile that was struck</param>
        /// <param name="fail">Reference of if the tile that was struck was killed or not. Death strike = fail is false</param>
        /// <param name="effectOnly">Reference of if the tile was actually struck with a poweful enough pickaxe to hurt it</param>
        /// <param name="noItem">Reference of if the tile should drop not item(s)</param>
        public override void KillTile(int x, int y, int oldType, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            if (OreExcavator.ServerConfig.creativeMode)
                noItem = true;

            if (WorldGen.gen || !Main.tile[x, y].HasTile || oldType < 0 || OreExcavator.killCalled || Main.netMode == NetmodeID.Server || Main.gameMenu)
                return;

            if (OreExcavator.ExcavateHotkey.GetAssignedKeys().Count <= 0)
            {
                OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.NoKey"), Color.Orange);
                return;
            }

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

            byte oldColor = OreExcavator.lookingAtTile.TileColor;
            int oldSubtype = TileObjectData.GetTileStyle(OreExcavator.lookingAtTile);

            if (!OreExcavator.ServerConfig.allowPickaxing)
            {
                OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Server.DisabledExcavations"), Color.Red);
                return;
            }

            if (x != Player.tileTargetX || y != Player.tileTargetY)
            {
                OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.PositionMismatch"), Color.Orange);
                return;
            }

            if(OreExcavator.masterTiles.ContainsKey(new Point16(x, y)))
            {
                OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.TilePending"), Color.Red);
                fail = true;
                return;
            }
            
            if (!OreExcavator.CheckIfAllowed(oldType, ActionType.TileKilled, oldSubtype))
            {
                OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.WhitelistFailed"), Color.Orange);
                return;
            }

            if (fail)
            {
                _ = Task.Run(async delegate // Create a new thread
                {
                    Thread.Sleep(50);
                    if (x != Player.tileTargetX || y != Player.tileTargetY)
                    {
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.PositionMismatch"), Color.Orange);
                        return;
                    }
                    if (Main.tile[x, y].HasTile && oldType != Main.tile[x, y].TileType)
                        OreExcavator.ModifySpooler(
                            ActionType.TurfHarvested, (ushort)x, (ushort)y,
                            (byte)OreExcavator.ClientConfig.recursionDelay,
                            OreExcavator.ClientConfig.recursionLimit,
                            true,
                            (byte)Main.myPlayer,
                            (ushort)oldType,
                            oldColor,
                            oldSubtype,
                            Main.tile[x, y].TileType
                        );
                });
                return;
            }
            else
                fail = true;

            OreExcavator.ModifySpooler(
                ActionType.TileKilled, (ushort)x, (ushort)y,
                (byte)OreExcavator.ClientConfig.recursionDelay,
                OreExcavator.ClientConfig.recursionLimit,
                OreExcavator.ClientConfig.doDiagonals,
                (byte)Main.myPlayer,
                (ushort)oldType,
                oldColor,
                oldSubtype
            );
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
        /// <param name="oldType">Tile ID of the wall that was struck</param>
        /// <param name="fail">Reference of if the wall that was struck was killed or not. Death strike = fail is false</param>
        public override void KillWall(int x, int y, int oldType, ref bool fail)
        {
            if (WorldGen.gen || fail || OreExcavator.killCalled || Main.netMode == NetmodeID.Server || Main.gameMenu)
                return;

            if (OreExcavator.ExcavateHotkey.GetAssignedKeys().Count <= 0)
            {
                OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.NoKey"), Color.Orange);
                return;
            }

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

            byte oldColor = OreExcavator.lookingAtTile.TileColor;
            //int oldSubtype = TileObjectData.GetTileStyle(OreExcavator.lookingAtTile);

            if (oldType <= WallID.None)
            {
                OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.InvalidType"), Color.Orange);
                return;
            }

            if (OreExcavator.lookingAtTile.HasTile)
            {
                OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Blocked"), Color.Orange);
                return;
            }

            if (!OreExcavator.ServerConfig.allowHammering)
            {
                OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Server.DisabledAlternatives"), Color.Red);
                return;
            }

            if (x != Player.tileTargetX || y != Player.tileTargetY)
            {
                OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.PositionMismatch"), Color.Orange);
                return;
            }

            if (OreExcavator.masterTiles.ContainsKey(new Point16(x, y)))
            {
                OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.TilePending"), Color.Red);
                fail = true;
                return;
            }

            if (!OreExcavator.CheckIfAllowed(oldType, ActionType.WallKilled))
            {
                OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.WhitelistFailed"), Color.Orange);
                return;
            }

            fail = true;

            OreExcavator.ModifySpooler(
                ActionType.WallKilled, (ushort)x, (ushort)y,
                (byte)OreExcavator.ClientConfig.recursionDelay,
                OreExcavator.ClientConfig.recursionLimit,
                OreExcavator.ClientConfig.doDiagonals,
                (byte)Main.myPlayer,
                (ushort)oldType,
                oldColor
            );
        }
    }

    internal class ExcavatorItem : GlobalItem
    {
        internal Direction GetExtendsDirection(int tileID)
        {
            return tileID switch
            {
                TileID.Platforms or TileID.MinecartTrack or TileID.PlanterBox => Direction.Horizontal,
                TileID.Rope or TileID.VineRope or TileID.WebRope => Direction.Vertical,
                _ => Direction.None,
            };
        }

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
            if (Main.mouseItem != null && Main.mouseItem.Name != "")
                item = Main.mouseItem;
            if (WorldGen.gen || Main.netMode == NetmodeID.Server || item.pick + item.axe + item.hammer != 0 || Main.gameMenu)
                return null;

            if (OreExcavator.ExcavateHotkey.GetAssignedKeys().Count <= 0) // || item.Name.ToLower().Contains("seed") || item.Name.ToLower().Contains("paint"))
            {
                OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.NoKey"), Color.Red);
                return null;
            }

            if (OreExcavator.ExcavateHotkey.GetAssignedKeys()[0] == "Mouse1")
                if (OreExcavator.ServerConfig.allowReplace && OreExcavator.ClientConfig.doSpecials)
                {
                    OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.KeyBinds.MainActionWarning",
                        OreExcavator.myMod.Name, (Terraria.GameInput.PlayerInput.CurrentInputMode < Terraria.GameInput.InputMode.XBoxGamepad ? "Click" : "Trigger")),
                        Color.Orange, LogType.Warn);
                    OreExcavator.ClientConfig.doSpecials = false;
                    OreExcavator.ClientConfig.showCursorTooltips = false;
                    OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                    return true;
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
                if (item.createTile >= TileID.Dirt || item.createWall > WallID.None)
                {
                    if (!OreExcavator.ServerConfig.allowReplace)
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Server.DisabledSwap"), Color.Red);
                    else if (!OreExcavator.ClientConfig.tileWhitelistToggled)
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Client.DisabledAlternatives"), Color.Red);
                    else if (!OreExcavator.ClientConfig.doSpecials)
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Client.DisabledAlternatives"), Color.Red);
                }
                else
                    OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.PositionMismatch"), Color.Red);
                return null;
            }

            ActionType actionType = ActionType.None;
            short createType = -1;

            bool oldHasTile = OreExcavator.lookingAtTile.HasTile;
            ushort oldType = oldHasTile ? OreExcavator.lookingAtTile.TileType : OreExcavator.lookingAtTile.WallType;
            byte oldColor = oldHasTile ? OreExcavator.lookingAtTile.TileColor : OreExcavator.lookingAtTile.WallColor;
            int oldSubtype = TileObjectData.GetTileStyle(OreExcavator.lookingAtTile);

            bool newHasTile = Main.tile[x, y].TileType > TileID.Dirt;
            ushort newType = newHasTile ? Main.tile[x, y].TileType : Main.tile[x, y].WallType;
            byte newColor = newHasTile ? Main.tile[x, y].TileColor : Main.tile[x, y].WallColor;
            int newSubtype = TileObjectData.GetTileStyle(Main.tile[x, y]);

            if (item.createTile >= TileID.Dirt && item.createTile >= item.createWall - 1) // Replacing and planting
            {
                if (oldType < TileID.Dirt)
                {
                    OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.TypeInvalid"), Color.Red);
                    return null;
                }

                if (!OreExcavator.ClientConfig.doSpecials)
                {
                    OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Client.DisabledAlternatives"), Color.Red);
                    return null;
                }

                if (oldType == newType && oldSubtype == newSubtype)
                {
                    OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Unaltered"), Color.Red);
                    return null;
                }

                if (item.Name.Contains("Seed"))
                {
                    if (!OreExcavator.ServerConfig.chainSeeding)
                    {
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Server.DisabledPlanting"), Color.Red);
                        return null;
                    }

                    actionType = ActionType.TurfPlanted;
                    createType = (short)item.createTile;
                }
                else
                {
                    createType = (short)item.createTile;
                    if (!OreExcavator.ServerConfig.allowReplace)
                    {
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Server.DisabledSwap"), Color.Red);
                        return null;
                    }

                    if (!oldHasTile)
                        if (OreExcavator.ClientConfig.ChainPlacing)
                            switch (item.createTile)
                            {
                                case TileID.Platforms:
                                case TileID.MinecartTrack:
                                case TileID.Rope:
                                case TileID.VineRope:
                                case TileID.WebRope:
                                case TileID.PlanterBox:
                                    actionType = ActionType.ExtendPlacement;
                                    OreExcavator.ModifySpooler(actionType, (ushort)x, (ushort)y,
                                        (byte)OreExcavator.ClientConfig.recursionDelay,
                                        OreExcavator.ClientConfig.recursionLimit,
                                        OreExcavator.ClientConfig.doDiagonals,
                                        (byte)Main.myPlayer,
                                        0, 0, 0,
                                        item.netID,
                                        item.placeStyle
                                    );
                                    return true;

                                default:
                                    return null;
                            }
                        else
                        {
                            OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Client.DisabledPlacing"), Color.Red);
                            return null;
                        }
                    else if (!newHasTile)
                    {
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.DoesNotExist"), Color.Red);
                        return null;
                    }
                    else
                        actionType = ActionType.TileReplaced;
                }
            }
            else if (item.createWall > TileID.Dirt && item.createTile < item.createWall)
            {
                if (oldType == newType)
                {
                    OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Unaltered"), Color.Red);
                    return null;
                }

                if (oldType <= WallID.None)
                {
                    OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.TypeInvalid"), Color.Red);
                    return null;
                }

                if (!OreExcavator.ServerConfig.allowReplace)
                {
                    OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Server.DisabledSwap"), Color.Red);
                    return null;
                }

                if (!OreExcavator.ClientConfig.doSpecials)
                {
                    OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Client.DisabledAlternatives"), Color.Red);
                    return null;
                }
                actionType = ActionType.WallReplaced;
                createType = (short)item.createWall;
            }
            else if (item.Name.Contains("Paint"))
            {
                if (!OreExcavator.ServerConfig.chainPainting)
                {
                    OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Server.DisabledPainting"), Color.Red);
                    return null;
                }

                if (item.Name.ToLower().Contains("brush"))
                {
                    if (oldColor == newColor)
                    {
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Unaltered"), Color.Red);
                        return null;
                    }
                    actionType = ActionType.TilePainted;
                    for (ushort index = 0; index < player.inventory.Length; index++)
                    {
                        if (newHasTile)
                            if (player.inventory[index].paint > PaintID.None)
                            {
                                OreExcavator.ModifySpooler(actionType, (ushort)x, (ushort)y,
                                    (byte)OreExcavator.ClientConfig.recursionDelay,
                                    OreExcavator.ClientConfig.recursionLimit,
                                    OreExcavator.ClientConfig.doDiagonals,
                                    (byte)Main.myPlayer,
                                    oldType,
                                    oldColor,
                                    oldSubtype,
                                    player.inventory[index].netID,
                                    player.inventory[index].paint
                                );
                                return true;
                            }
                    }
                    OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.NoPaint"), Color.Orange);
                    return null;
                }
                else if (item.Name.ToLower().Contains("roller"))
                {
                    if (oldColor == newColor)
                    {
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Unaltered"), Color.Red);
                        return null;
                    }
                    actionType = ActionType.WallPainted;
                    for (ushort index = 0; index < player.inventory.Length; index++)
                    {
                        if (newType > WallID.None)
                            if (player.inventory[index].paint > PaintID.None)
                            {
                                OreExcavator.ModifySpooler(actionType, (ushort)x, (ushort)y,
                                    (byte)OreExcavator.ClientConfig.recursionDelay,
                                    OreExcavator.ClientConfig.recursionLimit,
                                    OreExcavator.ClientConfig.doDiagonals,
                                    (byte)Main.myPlayer,
                                    oldType,
                                    oldColor,
                                    oldSubtype,
                                    player.inventory[index].netID,
                                    player.inventory[index].paint
                                );
                                return true;
                            }
                    }
                    OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.NoPaint"), Color.Orange);
                    return null;
                }
                else if (item.Name.ToLower().Contains("scraper"))
                {
                    if (oldColor == newColor) // TODO: check walls and tile colors?
                    {
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Unaltered"), Color.Red);
                        return null;
                    }
                    actionType = ActionType.ClearedPaint;
                    if (newType > WallID.None - (newHasTile ? 1 : 0))
                    {
                        OreExcavator.ModifySpooler(actionType, (ushort)x, (ushort)y,
                            (byte)OreExcavator.ClientConfig.recursionDelay,
                            OreExcavator.ClientConfig.recursionLimit,
                            OreExcavator.ClientConfig.doDiagonals,
                            (byte)Main.myPlayer,
                            oldType,
                            oldColor,
                            oldSubtype
                        );
                        return true;
                    }
                }
                else
                    OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.TypeInvalid"), Color.Red);
                return null;
            }
            else
            {
                OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.BadOperation"), Color.Red);
                return null;
            }

            if (createType < 0 || (actionType == ActionType.TileReplaced && createType == oldType && oldSubtype == newSubtype) // is it a valid type, and is what we're looking to replace valid?
                || (createType == oldType && (actionType == ActionType.WallReplaced || actionType == ActionType.TurfPlanted)))
                return null;

            if (actionType != ActionType.WallReplaced)
            {
                if (!Main.tile[x + 1, y].HasTile && !Main.tile[x, y + 1].HasTile && !Main.tile[x - 1, y].HasTile && !Main.tile[x, y - 1].HasTile)
                {
                    OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Unachored"), Color.Red);
                    return null;
                }
            }
            else if (Main.tile[x + 1, y].WallType <= WallID.None && Main.tile[x, y + 1].WallType <= WallID.None && Main.tile[x - 1, y].WallType <= WallID.None && Main.tile[x, y - 1].WallType <= WallID.None)
            {
                OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Unanchored"), Color.Red);
                return null;
            }

            string itemName = OreExcavator.GetFullNameById(item.type, actionType);
            string keybind = "<Unbound>";

            if (OreExcavator.ExcavateHotkey.GetAssignedKeys().Count > 0)
            {
                keybind = OreExcavator.ExcavateHotkey.GetAssignedKeys()[0];
                if (keybind.StartsWith("Oem"))
                    keybind = keybind.Substring(3);
                else if (keybind == "Mouse2")
                    keybind = "Right Click";
                else if (keybind == "Mouse1")
                    keybind = "Left Click";
            }

            if (OreExcavator.ClientConfig.itemWhitelistToggled && !OreExcavator.ClientConfig.itemWhitelist.Contains(itemName))
            {
                OreExcavator.Log($"Rejected chain-swapping 'ItemID.{item.Name} ({item.netID})' because it isn't whitelisted by you,\nHover over the item in your inventory and press '{keybind}' to start chain-swapping!", Color.Orange);
                return null;
            }
            if (OreExcavator.ServerConfig.itemBlacklistToggled && OreExcavator.ServerConfig.itemBlacklist.Contains(itemName))
            {
                OreExcavator.Log($"Rejected chain-swapping 'ItemID.{item.Name} ({item.netID})' because it's blacklisted by the server", Color.Orange);
                return null;
            }

            OreExcavator.ModifySpooler(
                actionType, (ushort)x, (ushort)y,
                (byte)OreExcavator.ClientConfig.recursionDelay,
                OreExcavator.ClientConfig.recursionLimit,
                (actionType == ActionType.TurfPlanted ? true : OreExcavator.ClientConfig.doDiagonals),
                (byte)Main.myPlayer,
                oldType,
                oldColor,
                oldSubtype,
                item.type,
                item.placeStyle
            );
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
                        keybind = keybind.Substring("Oem".Length);
                    else if (keybind == "Mouse2")
                        keybind = "Right Click";
                    else if (keybind == "Mouse1")
                        keybind = "Left Click";

                    if ((item.pick != 0 && OreExcavator.ServerConfig.allowPickaxing) && (item.hammer != 0 && OreExcavator.ServerConfig.allowHammering))
                        tooltips.Add(new TooltipLine(OreExcavator.myMod, "HowToUse", Language.GetTextValue("Mods.OreExcavator.UI.Tooltips.HoldToUse", keybind, $"{Language.GetTextValue("Mods.OreExcavator.Tiles")}/{Language.GetTextValue("Mods.OreExcavator.Walls")}")));
                    else if ((item.pick != 0 && OreExcavator.ServerConfig.allowPickaxing))
                        tooltips.Add(new TooltipLine(OreExcavator.myMod, "HowToUse", Language.GetTextValue("Mods.OreExcavator.UI.Tooltips.HoldToUse", keybind, Language.GetTextValue("Mods.OreExcavator.Tiles"))));
                    else if ((item.hammer != 0 && OreExcavator.ServerConfig.allowHammering))
                        tooltips.Add(new TooltipLine(OreExcavator.myMod, "HowToUse", Language.GetTextValue("Mods.OreExcavator.UI.Tooltips.HoldToUse", keybind, Language.GetTextValue("Mods.OreExcavator.Walls"))));
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
                        tooltips.Add(new TooltipLine(OreExcavator.myMod, "HowToUse", Language.GetTextValue("Mods.OreExcavator.KeyBinds.None", "Excavating")));
                    else if (name != "")
                        tooltips.Add(new TooltipLine(OreExcavator.myMod, "HowToUse", Language.GetTextValue("Mods.OreExcavator.KeyBinds.None", $"Chain-{(item.Name.ToLower().Contains("seed") ? "planting" : "swapping")}!")));
            }
        }

        public override bool CanUseItem(Item item, Player player)
        {
            if (Main.netMode == NetmodeID.Server)
                return true;

            // || !OreExcavator.ServerConfig.allowReplace || !OreExcavator.ClientConfig.tileWhitelistToggled || !OreExcavator.ClientConfig.doSpecials

            if (OreExcavator.lookingCoordX != Player.tileTargetX)
            {
                OreExcavator.lookingCoordX = (ushort)Player.tileTargetX;
                if (OreExcavator.lookingCoordY != Player.tileTargetY)
                    OreExcavator.lookingCoordY = (ushort)Player.tileTargetY;
                OreExcavator.lookingAtTile.CopyFrom(Main.tile[Player.tileTargetX, Player.tileTargetY]);
            }
            else if (OreExcavator.lookingCoordY != Player.tileTargetY)
            {
                OreExcavator.lookingCoordY = (ushort)Player.tileTargetY;
                if (OreExcavator.lookingCoordX != Player.tileTargetX)
                    OreExcavator.lookingCoordX = (ushort)Player.tileTargetX;
                OreExcavator.lookingAtTile.CopyFrom(Main.tile[Player.tileTargetX, Player.tileTargetY]);
            }

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
                    OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.KeyBinds.NoBind", OreExcavator.myMod.DisplayName, OreExcavator.myMod.Version), Color.Red, LogType.Warn);
                }).Start();
            }
            else if (OreExcavator.ClientConfig.showWelcome070 && OreExcavator.ServerConfig.showWelcome)
            {
                new Task(delegate
                {
                    Thread.Sleep(2000);
                    OreExcavator.Log($"[{OreExcavator.myMod.DisplayName}] - v{OreExcavator.myMod.Version}" +
                                     "\n\t  We now have a discord for bug reporting and feature suggestions:" +
                                     "\n\t  https://discord.gg/FtrsRtPe6h" +
                                     "\n\n\t  We released an update to combat some of the major dupes." +
                                     "\n\t  We've finally released chain painting, planting, harvesting and briding!" +
                                     "\n\t  We recommend resetting OE's configs, and re-editing them as you see fit." +
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