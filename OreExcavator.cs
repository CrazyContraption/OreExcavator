using Microsoft.Xna.Framework;       // XNA
using MonoMod.RuntimeDetour.HookGen; // Rip my soul
using MonoMod.Cil;                   // IL
using OreExcavator.Enumerations;     // Enums

using System;                        // IndexOf
using System.Collections.Concurrent; // ConcurrentDictionary
using System.Collections.Generic;    // Lists & Dictionaries
using System.IO;                     // Binary Reader, Path

using System.Reflection;             // Send help if you see this
using System.Threading;              // Threading
using System.Threading.Tasks;        // Threading

using Terraria;                      // Terraria
using Terraria.DataStructures;       // Point16
using Terraria.GameContent.UI.Chat;  // Used for message de-duplication
using Terraria.GameInput;            // Detecting controller vs. keyboard
using Terraria.ID;                   // Networking
using Terraria.Localization;
using Terraria.ModLoader;            // Modloader
using Terraria.ModLoader.Config;     // Configs
using Terraria.ObjectData;           // No a clue
using Terraria.UI.Chat;              // Used for message de-duplication



namespace OreExcavator /// The Excavator of Ores
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
        internal static ConcurrentDictionary<ushort?, Task> alterTasks = new();
        internal static ConcurrentQueue<(int, int, Alteration)> alterQueue = new();

        internal static OreExcavator myMod = ModContent.GetInstance<OreExcavator>();

        /// <summary>
        /// Static boolean that signifies if an excavation-related actions are taking place.
        /// </summary>
        public static bool KillCalled = false;

        /// <summary>
        /// Per-thread boolean that signifies if an excavation puppeting is taking place on that thread.
        /// </summary>
        [ThreadStatic] public static bool Puppeting = false;

        /// <summary>
        /// Per-thread ushort identifying the ID of the thread being run if any.
        /// </summary>
        [ThreadStatic] internal static ushort? ThreadId = null;


        internal static Tile lookingAtTile;
        internal static uint lookingCoordX;
        internal static uint lookingCoordY;
        internal static bool[] playerHalt = new bool[Main.maxPlayers + 1];
        internal static bool excavationToggled = false;
        internal static int msgReps = 1;
        internal static bool hostOnly = true;

        /// <summary>
        /// Called by tML when the mod is asked to load.
        /// This binds various important aspects of the mod.
        /// </summary>
        public override void Load()
        {
#if DEBUG
            Log($"Loading [{myMod.DisplayName}] - v{myMod.Version}... (DEBUG)", default, LogType.Info);
#elif TML_2022_10
            Log($"Loading [{myMod.DisplayName}] - v{myMod.Version}... (RELEASE)", default, LogType.Info);
#else
            Log($"Loading [{myMod.DisplayName}] - v{myMod.Version}... (PREVIEW)", default, LogType.Info);
#endif

            ExcavateHotkey = KeybindLoader.RegisterKeybind(this, "Excavate", "OemTilde");
            WhitelistHotkey = KeybindLoader.RegisterKeybind(this, "Whitelist", "Insert");
            BlacklistHotkey = KeybindLoader.RegisterKeybind(this, "UnWhitelist", "Delete");
            
            On.Terraria.Main.Update += Detour_Update; // Alteration updates to main world, required
            
            // Optional edits for QoL
            if (ServerConfig.aggressiveModCompatibility is false)
            {
                // Detours
                On.Terraria.WorldGen.SpawnFallingBlockProjectile += Detour_SpawnFallingBlockProjectile; // Stop falling tile gravity during excavations
                //On.Terraria.WorldGen.CheckOrb += WorldGen_CheckOrb;
                //On.Terraria.WorldGen.CheckPot += WorldGen_CheckPot;

                // IL edits
                //IL.Terraria.WorldGen.KillTile += ReturnIfCalled; // Stop vanilla cracked dungeon brick break propigation
                //IL.Terraria.WorldGen.KillTile_PlaySounds += ReturnIfCalled;
                //IL.Terraria.WorldGen.KillWall_PlaySounds += ReturnIfCalled;
            }
        }

        /// <summary>
        /// Pops alteration tasks off the queue to be updated in-game in a thread-safe way respective of Draw.
        /// </summary>
        private void Detour_Update(On.Terraria.Main.orig_Update orig, Main self, GameTime gameTime)
        {
            while (alterQueue.IsEmpty is false && (Main.gamePaused is false || Main.netMode is NetmodeID.MultiplayerClient))
            {
                _ = alterQueue.TryDequeue(out (int x, int y, Alteration alteration) alteration);
                if (alteration.alteration is not null)
                {
                    bool taskIsAlive = TaskOkay(alteration.alteration.threadID);
                    if (taskIsAlive)
                    {
                        KillCalled = true;
                        bool fatal = alteration.alteration.DoAlteration(alteration.x, alteration.y);
                        KillCalled = false;

                        if (Main.netMode is not NetmodeID.Server)
                            if (alteration.x == Player.tileTargetX && alteration.y == Player.tileTargetY)
                                lookingAtTile.CopyFrom(Main.tile[alteration.x, alteration.y]);

                        if (fatal)
                        {
                            Log($"Excavation killed - Fatal response (#{alteration.alteration.threadID})", Color.Orange);

                            if (Main.netMode is NetmodeID.MultiplayerClient)
                            {
                                ModPacket packet = GetPacket();
                                packet.Write((byte)ActionType.HaltExcavation);
                                packet.Write((ushort)(alteration.alteration.threadID ?? 0));
                                packet.Send(-1, Main.myPlayer);
                            }
                            _ = alterTasks.TryRemove(alteration.alteration.threadID, out _);
                        }
                    }
                }
            }
            orig(self, gameTime);
        }

        /// <summary>
        /// Prevents falling tiles from updating during an excavation.
        /// </summary>
        private bool Detour_SpawnFallingBlockProjectile(
            On.Terraria.WorldGen.orig_SpawnFallingBlockProjectile orig,
            int x,
            int y,
            Tile tileCache,
            Tile tileTopCache,
            Tile tileBottomCache,
            int type)
        {
            if (Puppeting || KillCalled)
                return false;

            return orig(x, y, tileCache, tileTopCache, tileBottomCache, type);
        }

#if DEBUG
        /// <summary>
        /// Prevents orb duplication during an excavation.
        /// </summary>
        private void WorldGen_CheckOrb(On.Terraria.WorldGen.orig_CheckOrb orig, int i, int j, int type)
        {
            if (Puppeting || KillCalled)
                return;
            orig(i, j, type);
        }

        /// <summary>
        /// Prevents pot duplication during an excavation.
        /// </summary>
        private void WorldGen_CheckPot(On.Terraria.WorldGen.orig_CheckPot orig, int i, int j, int type)
        {
            if (Puppeting || KillCalled)
                return;
            orig(i, j, type);
        }
#endif

        /// <summary>
        /// IL-ready injection that returns any injected method immediately if the thread calling it is doing an excavation
        /// </summary>
        /// 
        /// <param name="il"></param>
        private static void ReturnIfCalled(ILContext il)
        {
            var cursor = new ILCursor(il); // The current "instruction"
            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchStloc(33), i => i.MatchLdloca(33)))
                return; // Patch unable to be applied
            cursor.Index--;

            var label = il.DefineLabel();
            cursor.EmitDelegate<Func<bool>>(() => KillCalled); // If this thread manually called a kill
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Brfalse_S, label); // If kill was not manually called, goto label
            //cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_0); // Add false to the stack for the return
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ret); // If kill was called, return immediately, don't continue
            cursor.MarkLabel(label); // Label
        }

#if DEBUG == false
        /// <summary>
        /// Executes once most -if not all- modded content is loaded by tML.
        /// Looks for items from other mods that could be classified as ores, gems, chunks, etc.
        /// May change from time to time as mods change their preferences.
        /// </summary>
        public override void PostSetupContent()
        {
            Log("Auto-whitelist runner started. Looking for modded ores and gems to whitelist...", default, LogType.Info);

            Log("Running through tML Ore Tile Set...", default, LogType.Info);

            foreach (var ore in TileID.Sets.Ore.GetTrueIndexes())
            {
                string name = GetFullNameById(ore, ActionType.TileWhiteListed);
                if (ClientConfig.tileWhitelist.Contains(name) is false)
                {
                    Log($"Found ore from Ore Set: '{name}', adding to whitelist...", default, LogType.Info);
                    ClientConfig.tileWhitelist.Add(name);
                }
                else
                    Log($"Found ore from Ore Set: '{name}', but was already whitelisted.", default, LogType.Info);
            }

            if (ModLoader.TryGetMod("CalamityMod", out var calamityMod) && calamityMod != null)
            {
                Log("Found Calamity - Adding hard-coded tiles to whitelist!", default, LogType.Info);
                string[] calamityTiles =
                {

                };
                if (calamityTiles.Length <= 0)
                    Log("Ooops - nothing to whitelist, guess I didn't finish this. CALAMITY SEND ME YOUR ORE LIST YOU WANT WHITELISTED", default, LogType.Info);
                foreach (string calamityTile in calamityTiles)
                {
                    string calamityName = "CalamityMod.CalamityMod." + calamityTile;
                    if (ClientConfig.tileWhitelist.Contains(calamityName) is false)
                    {
                        Log($"Found Calamity Ore: 'TileID.{calamityTile}'... Adding to whitelist as '{calamityName}'", default, LogType.Info);
                        ClientConfig.tileWhitelist.Add(calamityName);
                    }
                    else
                        Log($"Found Calamity Ore: 'TileID.{calamityTile}'... But already exists in whitelist as '{calamityName}'", default, LogType.Info);
                }
                Log("CalamityMod manual whitelisting concluded.", default, LogType.Info);
            }
            if (ModLoader.TryGetMod("ThoriumMod", out var thoriumMod) && thoriumMod != null)
            {
                Log("Found ThroiumMod - Adding hard-coded tiles to whitelist!", default, LogType.Info);
                string[] thoriumTiles =
                {
                    "Opal",
                    "SmoothCoal",
                    "Aquamarine",
                    "LifeQuartz",
                    "Aquaite",
                    "ValadiumChunk",
                    "LodestoneChunk",
                    "LumiteChunk",
                    "SynthGold",
                    "SynthPlatinum",
                    "ThoriumOre"
                };
                foreach (string thoriumTile in thoriumTiles)
                {
                    string thoriumName = "ThoriumMod.ThoriumMod." + thoriumTile;
                    if (ClientConfig.tileWhitelist.Contains(thoriumName) is false)
                    {
                        Log($"Found Thorium Ore: 'TileID.{thoriumTile}'... Adding to whitelist as '{thoriumName}'", default, LogType.Info);
                        ClientConfig.tileWhitelist.Add(thoriumName);
                    }
                    else
                        Log($"Found Thorium Ore: 'TileID.{thoriumTile}'... But already exists in whitelist as '{thoriumName}'", default, LogType.Info);
                }
                Log("ThoriumMod manual whitelisting concluded.", default, LogType.Info);
            }

            for (int id = TileID.Count; id < TileLoader.TileCount; id++)
            {
                string name;
                ModTile tile = TileLoader.GetTile(id);
                if (tile is null)
                    name = TileID.Search.GetName(id);
                else
                    name = tile.Name;
                
                // Strip Tile and T from modded ores (common naming convention)
                // This is done to use EndsWith in next if statement (faster than IndexOf). 
                if (name.EndsWith("Tile", StringComparison.Ordinal))
                    name = name[0..^4];
                else if (name.EndsWith('T'))
                    name = name[0..^1];

                string newName = name;

                // If modded tile is ore, gem, or chunk, replace with upper case (formatting).
                // This if is used for auto-whitelist adding modded tiles later.
                if (name.EndsWith("Ore", StringComparison.Ordinal))
                    newName = newName.Replace("Ore", "'ORE'");
                else if (name.EndsWith("Gem", StringComparison.Ordinal))
                    newName = newName.Replace("Ore", "'GEM'");
                else if (name.EndsWith("Chunk", StringComparison.Ordinal))
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
                            if (name.EndsWith("Ore", StringComparison.Ordinal))
                                newName = newName.Replace("Ore", "'ORE'");
                            else if (name.EndsWith("Gem", StringComparison.Ordinal))
                                newName = newName.Replace("Ore", "'GEM'");
                            else if (name.EndsWith("Chunk", StringComparison.Ordinal))
                                newName = newName.Replace("Ore", "'CHUNK'");
                        }
                    }
                }
                if (name != newName)
                {
                    name = GetFullNameById(id, ActionType.TileWhiteListed);
                    if (ClientConfig.tileWhitelist.Contains(name) is false)
                    {
                        Log($"Found Other Ore: 'TileID.{newName}'... Adding to whitelist as '{name}'", default, LogType.Info);
                        ClientConfig.tileWhitelist.Add(name);
                    }
                    else
                        Log($"Found Other Ore: 'TileID.{newName}'... But already exists in whitelist as '{name}'", default, LogType.Info);
                }
            }
            ExcavatorSystem.SaveConfig(ClientConfig);
            Log("Auto-whitelist runner concluded. Finishing up...", default, LogType.Info);
        }
#endif

        internal static bool TaskOkay(ushort? id)
        {
            if (id is null)
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

            switch (msgType)
            {
                case ActionType.None:
                case ActionType.ResetExcavations:
                case ActionType.HaltExcavations:
                case ActionType.TileWhiteListed:
                case ActionType.WallWhiteListed:
                case ActionType.ItemWhiteListed:
                case ActionType.TileBlackListed:
                case ActionType.WallBlackListed:
                case ActionType.ItemBlackListed:
                    break;

                default:
                    ThreadId = reader.ReadUInt16();
                    break;
            }
#if DEBUG
            Log($"Packet from: '{(Main.netMode == NetmodeID.MultiplayerClient ? $"SERVER', origin: 'Player.{Main.player[origin].name}" : "Player." + Main.player[origin].name) + $" ({origin})"}', type: [{ThreadId}]'ActionType.{msgType}'");
#endif

            switch (msgType)
            {
                case ActionType.HaltExcavation:
                    {
                        if (Main.netMode is NetmodeID.Server)
                        {
                            packet.Write((byte)ActionType.HaltExcavation);
                            packet.Write((ushort)ThreadId);
                            packet.Send(-1, origin);
                        }
                        _ = alterTasks.TryRemove(ThreadId, out var _);
                    }
                    break;

                case ActionType.HaltExcavations:
                    {
                        if (Main.netMode is NetmodeID.Server)
                        {
                            packet.Write((byte)ActionType.HaltExcavations);
                            packet.Send(-1, origin);
                        }
                        playerHalt[origin] = true;
                    }
                    break;

                case ActionType.ResetExcavations:
                    {
                        if (Main.netMode is NetmodeID.Server)
                        {
                            packet.Write((byte)ActionType.ResetExcavations);
                            packet.Send(-1, origin);
                        }
                        playerHalt[origin] = false;
                    }
                    break;

                case ActionType.ClearedPaint:
                case ActionType.TileKilled:
                case ActionType.WallKilled:
                    {
                        playerHalt[origin] = false;
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
                case ActionType.TilePainted:
                case ActionType.WallPainted:
                    {
                        playerHalt[origin] = false;
                        ushort x = reader.ReadUInt16(), y = reader.ReadUInt16();
                        byte delay = reader.ReadByte();
                        int limit = reader.ReadInt32();
                        bool doDiagonals = reader.ReadBoolean();
                        ushort targetType = reader.ReadUInt16();
                        byte targetColor = reader.ReadByte();
                        int targetSubtype = reader.ReadInt32(), replaceType = reader.ReadInt32();
                        sbyte replaceSubtype = reader.ReadSByte();
                        ModifySpooler(msgType, x, y, delay, limit, doDiagonals, origin, targetType, targetColor, targetSubtype, replaceType, replaceSubtype, true);
                    }
                    break;

                case ActionType.SeedHarvested:
                case ActionType.SeedPlanted:
                    {
                        playerHalt[origin] = false;
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

                        if (Main.netMode is NetmodeID.MultiplayerClient)
                            switch (msgType)
                            {
                                case ActionType.TileWhiteListed:
                                    
                                    Log(Language.GetTextValue("Mods.OreExcavator.Network.Added", Main.player[origin].name, $"TileID.{GetFullNameById(id, msgType, subtype)} ({id})"),
                                        Color.Green, LogType.Info);
                                    break;

                                case ActionType.WallWhiteListed:
                                    Log(Language.GetTextValue("Mods.OreExcavator.Network.Added", Main.player[origin].name, $"WallID.{GetFullNameById(id, msgType, subtype)} ({id})"),
                                        Color.Orange, LogType.Info);
                                    break;

                                case ActionType.ItemWhiteListed:
                                    Log(Language.GetTextValue("Mods.OreExcavator.Network.Added", Main.player[origin].name, $"ItemID.{GetFullNameById(id, msgType, subtype)} ({id})"),
                                        Color.Green, LogType.Info);
                                    break;

                                case ActionType.TileBlackListed:
                                    Log(Language.GetTextValue("Mods.OreExcavator.Network.Removed", Main.player[origin].name, $"TileID.{GetFullNameById(id, msgType, subtype)} ({id})"),
                                        Color.Orange, LogType.Info);
                                    break;

                                case ActionType.WallBlackListed:
                                    Log(Language.GetTextValue("Mods.OreExcavator.Network.Removed", Main.player[origin].name, $"WallID.{GetFullNameById(id, msgType, subtype)} ({id})"),
                                        Color.Green, LogType.Info);
                                    break;

                                case ActionType.ItemBlackListed:
                                    Log(Language.GetTextValue("Mods.OreExcavator.Network.Removed", Main.player[origin].name, $"ItemID.{GetFullNameById(id, msgType, subtype)} ({id})"),
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
        /// <param name="playerID"></param>
        /// <param name="puppeting"></param>
        /// <param name="replacementType"></param>
        /// <param name="replacementSubtype"></param>
        internal static ushort? ModifySpooler(ActionType actionType, ushort x, ushort y, byte delay, int limit, bool doDiagonals, byte playerID, ushort targetType, byte targetColor = 0, int targetSubtype = -1, int replacementType = -1, sbyte replacementSubtype = -1, bool puppeting = false)
        {
            var checkPoint = new Point16(x, y);

            if (masterTiles.ContainsKey(checkPoint))
                masterTiles.Remove(checkPoint, out _);

            if (actionType == ActionType.TileReplaced && replacementType < 0)
                return null;
            if (actionType == ActionType.WallReplaced && replacementType <= 0)
                return null;

            ushort? randomId = null;

            if (puppeting is false)
                do
                    randomId = (ushort)Main.rand.Next(ushort.MinValue, ushort.MaxValue);
                while (alterTasks.ContainsKey(randomId));
            else
                randomId = ThreadId;

            int hardlimit = Math.Min(ClientConfig.recursionLimit, ServerConfig.recursionLimit);
            if (limit <= 0 || hardlimit <= 0)
                return null;

            // Not a server, and not multiplayer or non-puppeting
            // Singleplayer or non-puppeting multiplayer
            if (Main.netMode is NetmodeID.SinglePlayer || (Main.netMode is NetmodeID.MultiplayerClient && puppeting is false))
            {
                switch (actionType)
                {
                    case ActionType.ExtendPlacement:
                    case ActionType.TileReplaced:
                    case ActionType.WallReplaced:
                    case ActionType.TilePlaced:
                    case ActionType.WallPlaced:
                    case ActionType.TilePainted:
                    case ActionType.WallPainted:
                    case ActionType.SeedPlanted:
                        limit = Main.player[playerID].CountItem(replacementType) + (Main.mouseItem.netID == replacementType ? Main.mouseItem.stack : 0);
                        break;
                }
                limit = Math.Min(limit, hardlimit);
            }
            // Multi-non-puppet, or server 
            if (Main.netMode is NetmodeID.Server || (Main.netMode is NetmodeID.MultiplayerClient && puppeting is false))
            {
                ModPacket packet = myMod.GetPacket();
                if (Main.netMode is NetmodeID.Server)
                    packet.Write((byte)playerID);
                packet.Write((byte)actionType);
                packet.Write((ushort)(randomId ?? 0));
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
                    case ActionType.TilePainted:
                    case ActionType.WallPainted:
                        packet.Write((int)replacementType);
                        packet.Write((sbyte)replacementSubtype);
                        break;

                    case ActionType.SeedHarvested:
                    case ActionType.SeedPlanted:
                        packet.Write((int)replacementType);
                        break;
                }
                packet.Send(-1, playerID);

#if DEBUG
                switch (actionType)
                {
                    case ActionType.None:
                        Log($"Packet FAIL: '{(Main.netMode == NetmodeID.MultiplayerClient ? "TO SERVER" : "TO PLAYERS")}', type: [{ThreadId}]'ActionType.{actionType}'");
                        break;

                    case ActionType.HaltExcavation:
                    case ActionType.TileKilled:
                    case ActionType.TilePlaced:
                    case ActionType.WallKilled:
                    case ActionType.WallPlaced:
                    case ActionType.TileReplaced:
                    case ActionType.WallReplaced:
                    case ActionType.SeedPlanted:
                    case ActionType.SeedHarvested:
                    case ActionType.TilePainted:
                    case ActionType.WallPainted:
                    case ActionType.ClearedPaint:
                    case ActionType.ExtendPlacement:
                        Log($"Packet sent: '{(Main.netMode == NetmodeID.MultiplayerClient ? "TO SERVER" : "TO PLAYERS")}', type: [{ThreadId}]'ActionType.{actionType}'");
                        break;

                    default:
                        Log($"Packet sent: '{(Main.netMode == NetmodeID.MultiplayerClient ? "TO SERVER" : "TO PLAYERS")}', type: 'ActionType.{actionType}'");
                        break;
                }
#endif
            }

            Task task = new(delegate
            {
                ThreadId = randomId;
                Puppeting = puppeting;
                ModifyAdjacentFloodfill(actionType, x, y, limit, delay, doDiagonals, playerID, targetType, targetColor, targetSubtype, replacementType, replacementSubtype);
            });

            if (x == Player.tileTargetX && y == Player.tileTargetY)
                lookingAtTile.CopyFrom(Main.tile[x, y]);

            _ = alterTasks.TryAdd(randomId, task);
            task.Start();
            return randomId;
        }

        internal static void ModifySpooler(ActionType actionType, ushort x, ushort y, byte delay, int limit, bool doDiagonals, byte playerId, Tile targetTile, int replacementType = -1, sbyte replacementSubtype = -1, bool puppeting = false)
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
        internal static void ModifyAdjacentFloodfill(ActionType actionType, ushort originX, ushort originY, int limit, byte delay, bool doDiagonals, byte playerID, int targetType, byte targetColor = PaintID.None, int targetSubtype = -1, int itemType = -1, sbyte itemSubtype = -1)
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

            switch (actionType)
            {
                case ActionType.SeedHarvested:
                case ActionType.ExtendPlacement:
                case ActionType.SeedPlanted:
                case ActionType.TilePainted:
                case ActionType.WallPainted:
                    passReplacementTypes = true;
                    break;

                case ActionType.TileReplaced:
                case ActionType.WallReplaced:
                case ActionType.TilePlaced:
                case ActionType.WallPlaced:
                    passReplacementTypes = true;
                    isFallingType = !isWallBounded && TileID.Sets.Falling[targetType] || (ModContent.TileType<ModTile>() > 0 && TileID.Sets.Falling[ModContent.TileType<ModTile>()]);
                    break;
            }

            bool doOrigin = !passReplacementTypes;
            if (Puppeting)
                doOrigin = false;

            Direction extensionDirection = Direction.None;
            if (actionType == ActionType.ExtendPlacement)
            {
                Item item = new(itemType);
                extensionDirection = GetExtendsDirection(item.createTile);
            }

            sbyte[] ch_x = new sbyte[] { -1,  1,  0,  0,  1,  1, -1, -1 };
            sbyte[] ch_y = new sbyte[] {  0,  0, 1,  -1, -1,  1,  1, -1 };

            if (actionType == ActionType.ExtendPlacement && !isWallBounded)
                limit = Math.Min(200, limit); // Ensure that placements don't get out of hand

            Alteration alteration = new(ThreadId, actionType, playerID, Puppeting, (passReplacementTypes ? itemType : -1), passReplacementTypes ? itemSubtype : (sbyte)-1);
            Log($"[{ThreadId}] (ID:{playerID} - P:{Puppeting}) {actionType}, OX:{originX}, OY:{originY}, L:{limit}, D:{delay}, DD:{doDiagonals}, TT:{targetType}:{targetSubtype}({targetColor})[{(isWallBounded ? "W" : "T" )}], RT:{itemType}:{itemSubtype} F:{isFallingType}", Color.Yellow);

            Queue<Point16> queue = new();
            queue.Enqueue(new Point16(originX, originY));

            for (int tileCount = 0; queue.Count > 0 && tileCount < limit && !Main.gameMenu; tileCount++)
            {
                Point16 currentPoint = queue.Dequeue();

                if (currentPoint.X != originX || currentPoint.Y != originY || doOrigin)
                    if (masterTiles.TryAdd(new Point16(currentPoint.X, currentPoint.Y), false))
                    {
                        if (TaskOkay(ThreadId))
                        {
                            alterQueue.Enqueue((currentPoint.X, currentPoint.Y, alteration));
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

                byte max = (byte)(actionType == ActionType.ExtendPlacement ? (extensionDirection is Direction.Vertical ? 4 : 2) : (doDiagonals ? 8 : 4)); // Because testing this every loop is terrible
                for (byte index = (byte)(actionType == ActionType.ExtendPlacement ? (extensionDirection is Direction.Vertical ? 2 : 0) : 0 ); index < max; index++)
                {
                    Point16 nextPoint = new(currentPoint.X + ch_x[index], currentPoint.Y + ch_y[index]);

                    ushort x = (ushort)nextPoint.X;
                    ushort y = (ushort)nextPoint.Y;

                    if (WorldGen.InWorld(x, y) && queue.Contains(nextPoint) is false && masterTiles.ContainsKey(nextPoint) is false && Framing.GetTileSafely(nextPoint) is Tile checkTile)
                        if (actionType == ActionType.ExtendPlacement)
                        {
                            if (checkTile.HasTile is false)
                            {
                                queue.Enqueue(nextPoint);
                                if (ClientConfig.reducedEffects is false && checkTile.TileType != Main.tile[originX, originY].TileType)
                                    Dust.NewDustPerfect(new Vector2(x * 16, y * 16), DustID.Teleporter);
                            }
                        }
                        else if (isWallBounded is false && WorldGen.CanKillTile(x, y) && checkTile.HasTile && checkTile.TileType == targetType)
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

                                case ActionType.SeedPlanted:
                                    if (Main.tile[x, y - 1].HasTile is false || Main.tile[x, y + 1].HasTile is false || Main.tile[x - 1, y].HasTile is false || Main.tile[x + 1, y].HasTile is false)
                                        if (checkTile.TileType != Main.tile[originX, originY].TileType)
                                            goto default;
                                    continue;

                                default:
                                    if (checkTile.TileColor == targetColor)
                                    {
                                        queue.Enqueue(nextPoint);
                                        if (ClientConfig.reducedEffects is false)
                                            Dust.NewDustPerfect(new Vector2(x * 16, y * 16), DustID.Teleporter);
                                    }
                                    continue;
                            }
                        else if (isWallBounded && (Main.tile[x, y].HasTile is false || Main.tile[x, y].Slope != SlopeType.Solid || WorldGen.SolidTile(x, y) is false) && Main.tile[x, y].WallType == targetType)
                            if (checkTile.WallColor == targetColor)
                            {
                                queue.Enqueue(nextPoint);
                                if (ClientConfig.reducedEffects is false)
                                    Dust.NewDustPerfect(new Vector2(x * 16, y * 16), DustID.Teleporter);
                            }
                }
                if ((playerHalt[playerID] && Puppeting) || (Main.netMode != NetmodeID.Server && playerID == Main.myPlayer && ClientConfig.releaseCancelsExcavations && (ClientConfig.toggleExcavations ? excavationToggled : OreExcavatorKeybinds.excavatorHeld) is false))
                    break;
            }

            if (Main.gameMenu)
            {
                masterTiles.Clear();
                queue.Clear();
                Puppeting = false;
                return;
            }

            if ((playerHalt[playerID] && Puppeting) || (Main.netMode != NetmodeID.Server && playerID == Main.myPlayer && ClientConfig.releaseCancelsExcavations && (ClientConfig.toggleExcavations ? excavationToggled : OreExcavatorKeybinds.excavatorHeld) is false))
            {
                //Log(playerID + " HALTED");
                if (Main.netMode is NetmodeID.MultiplayerClient && playerID == Main.myPlayer)
                {
                    ModPacket packet = myMod.GetPacket();
                    packet.Write((byte)ActionType.HaltExcavations);
                    packet.Send();
                    //Log(playerID + " SENDING " + ActionType.HaltExcavations);
                }

                Thread.Sleep(delay + (Math.Max(delay, (ushort)2) / 2));

                while (queue.Count > 0)
                {
                    Point16 temp = queue.Dequeue();
                    WorldGen.TileFrame(temp.X, temp.Y);
                    _ = masterTiles.TryRemove(temp, out _);
                }

                if (Main.netMode is NetmodeID.MultiplayerClient && playerID == Main.myPlayer)
                {
                    ModPacket packet = myMod.GetPacket();
                    packet.Write((byte)ActionType.ResetExcavations);
                    packet.Send();
                    //Log(playerID + " SENDING " + ActionType.ResetExcavations);
                }
                playerHalt[playerID] = false; // No longer halted because operations are terminated
            }

            queue.Clear(); // Clear the queue

            Puppeting = false;

            //Log(playerID + " EXCAVATION COMPLETED");
        }

        /// <summary>
        /// Checks if an id is valid for a given action against the whitelists and blacklists.
        /// Respecting their enabled status and contents. Will return null for invalid lookups.
        /// </summary>
        /// 
        /// <param name="id">ID to translate into a string</param>
        /// <param name="type">Whitelist type of the ID being passed, black/white treated the same</param>
        /// <param name="subtype">Whitelist frameY of the ID being passed, if any</param>
        /// <param name="checkClient">Should checks be done against the local whitelist too?</param>
        /// <returns>Translated string</returns>
        internal static bool? CheckIfAllowed(int id, ActionType type, int subtype = -1, bool checkClient = true)
        {
            if (Main.netMode is NetmodeID.Server)
                checkClient = false;

            string fullName = GetFullNameById(id, type, subtype);
            if (fullName is null)
                return null;

            switch (type)
            {
                case ActionType.TileKilled:
                    if (ServerConfig.allowPickaxing)
                        goto case ActionType.TileWhiteListed;
                    goto default;
                case ActionType.TileWhiteListed:
                case ActionType.TileBlackListed:
                    if (subtype >= 0) // Checks to see if the global variant is listed, if not - check the specific subtype definition if any
                    {
                        bool? baseIsAllowed = CheckIfAllowed(id, type, -1, false);
                        if (baseIsAllowed is null)
                            return null;
                        if (baseIsAllowed is false)
                            return false;
                    }
                    return
                        (!checkClient || !ClientConfig.tileWhitelistAll || ClientConfig.tileWhitelist.Contains(fullName)) &&
                        (!ServerConfig.tileBlacklistToggled || !ServerConfig.tileBlacklist.Contains(fullName));

                case ActionType.WallKilled:
                    if (ServerConfig.allowHammering)
                        goto case ActionType.WallWhiteListed;
                    goto default;
                case ActionType.WallWhiteListed:
                case ActionType.WallBlackListed:
                    return
                        (!checkClient || !ClientConfig.wallWhitelistAll || ClientConfig.wallWhitelist.Contains(fullName)) &&
                        (!ServerConfig.wallBlacklistToggled || !ServerConfig.wallBlacklist.Contains(fullName));

                case ActionType.TileReplaced:
                case ActionType.WallReplaced:
                    if (ServerConfig.allowReplace)
                        goto case ActionType.ItemWhiteListed;
                    goto default;

                case ActionType.SeedPlanted:
                    if (ServerConfig.chainSeeding)
                        goto case ActionType.ItemWhiteListed;
                    goto default;

                case ActionType.ItemWhiteListed:
                case ActionType.ItemBlackListed:
                    return
                        (!checkClient || !ClientConfig.itemWhitelistAll || ClientConfig.itemWhitelist.Contains(fullName)) &&
                        (!ServerConfig.itemBlacklistToggled || !ServerConfig.itemBlacklist.Contains(fullName));

                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets the extendable direction of the tile being interacted with, if any.
        /// </summary>
        /// 
        /// <param name="tileID">TileID type to check the direction of extension, if any.</param>
        /// <returns>Translated string</returns>
        internal static Direction GetExtendsDirection(int tileID)
        {
            return tileID switch
            {
                TileID.Rope or TileID.SilkRope or TileID.VineRope or TileID.WebRope => Direction.Vertical,
                TileID.Platforms or TileID.PlanterBox or TileID.MinecartTrack => Direction.Horizontal,
                _ => Direction.None,
            };
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
            switch (type)
            {
                case ActionType.TileWhiteListed:
                case ActionType.TileBlackListed:
                case ActionType.TileKilled:
                    ModTile modTile = TileLoader.GetTile(id);
                    if (modTile is not null)
                        return $"{modTile.Mod}:{modTile.Name}" + (subtype >= 0 ? $":{subtype}" : "");
                    else if (id < TileID.Count)
                        return "Terraria:" + TileID.Search.GetName(id) + (subtype >= 0 ? $":{subtype}" : "");
                    else
                        return null;

                case ActionType.WallWhiteListed:
                case ActionType.WallBlackListed:
                case ActionType.WallKilled:
                    ModWall modWall = WallLoader.GetWall(id);
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
                case ActionType.SeedPlanted:
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

#if DEBUG && false
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
                case ActionType.SeedHarvested:
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
                case ActionType.SeedPlanted:
                    return ItemID.Search.GetId(names[1]);

                default:
                    return -1;
            }
        }
#endif

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
                        if (ClientConfig.doDebugStuff)
                            myMod.Logger.Debug(msg);
                        else
                        {
#if DEBUG
                            myMod.Logger.Debug(msg);
#endif
                        }
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
#if DEBUG == false
            if (type is not LogType.Debug || ClientConfig.doDebugStuff)
#endif
                if (Main.netMode is not NetmodeID.Server && color != default)
                    {
                        // This stuff combines duplicated messages
                        try {
                            List<ChatMessageContainer> messages = (List<ChatMessageContainer>)typeof(RemadeChatMonitor).GetField("_messages", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Main.chatMonitor);
                            if (messages != null && messages.Count > 0)
                            {
                                List<TextSnippet[]> parsedText = (List<TextSnippet[]>)typeof(ChatMessageContainer).GetField("_parsedText", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(messages[0]);
                                if (parsedText[^1][0].TextOriginal.Equals(msg))
                                {
                                    int newline = parsedText[^1][0].TextOriginal.LastIndexOf('\n');
                                    parsedText[^1][0].Text = string.Concat(parsedText[^1][0].TextOriginal.AsSpan(newline >= 0 ? newline : 0), $" [{++msgReps}x]");
                                }
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
            if (WorldGen.gen || Main.tile[x, y].HasTile is false || oldType < 0 || OreExcavator.KillCalled || Main.netMode == NetmodeID.Server || Main.gameMenu)
                return;

            if (OreExcavator.ServerConfig.creativeMode)
                noItem = true;

            if (OreExcavator.ExcavateHotkey.GetAssignedKeys(PlayerInput.UsingGamepad ? InputMode.XBoxGamepad : InputMode.Keyboard).Count <= 0)
            {
                OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.NoKey"), Color.Orange);
                return;
            }

            // Not essential, but helps reset the player during inactive phases
            if (OreExcavator.ClientConfig.toggleExcavations ? !OreExcavator.excavationToggled : !OreExcavatorKeybinds.excavatorHeld)
            {
                if (OreExcavator.ClientConfig.releaseCancelsExcavations && OreExcavator.playerHalt[Main.myPlayer] is true)
                {
                    OreExcavator.masterTiles.Clear();
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        ModPacket packet = OreExcavator.myMod.GetPacket();
                        packet.Write((byte)ActionType.ResetExcavations);
                        packet.Send();
                    }
                    OreExcavator.playerHalt[Main.myPlayer] = false;
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
            
            if (OreExcavator.CheckIfAllowed(oldType, ActionType.TileKilled, oldSubtype) is null or false)
            {
                OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.WhitelistFailed"), Color.Orange);
                return;
            }

            if (fail)
            {
                _ = Task.Run(delegate // Create a new thread
                {
                    Thread.Sleep(50);
                    if (x != Player.tileTargetX || y != Player.tileTargetY)
                    {
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.PositionMismatch"), Color.Orange);
                        return;
                    }
                    if (Main.tile[x, y].HasTile && oldType != Main.tile[x, y].TileType)
                        OreExcavator.ModifySpooler(
                            ActionType.SeedHarvested, (ushort)x, (ushort)y,
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
            if (WorldGen.gen || fail || OreExcavator.KillCalled || Main.netMode == NetmodeID.Server || Main.gameMenu)
                return;

            if (OreExcavator.ExcavateHotkey.GetAssignedKeys(PlayerInput.UsingGamepad ? InputMode.XBoxGamepad : InputMode.Keyboard).Count <= 0)
            {
                OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.NoKey"), Color.Orange);
                return;
            }

            // Not essential, but helps reset the player during inactive phases
            if (OreExcavator.ClientConfig.toggleExcavations ? !OreExcavator.excavationToggled : !OreExcavatorKeybinds.excavatorHeld)
            {
                if (OreExcavator.ClientConfig.releaseCancelsExcavations && OreExcavator.playerHalt[Main.myPlayer] is true)
                {
                    OreExcavator.masterTiles.Clear();
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        ModPacket packet = OreExcavator.myMod.GetPacket();
                        packet.Write((byte)ActionType.ResetExcavations);
                        packet.Send();
                    }
                    OreExcavator.playerHalt[Main.myPlayer] = false;
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

            if (OreExcavator.CheckIfAllowed(oldType, ActionType.WallKilled) is null or false)
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
            if (WorldGen.gen || Main.netMode == NetmodeID.Server || Main.gameMenu)
                return null;

            if (Main.mouseItem is not null && Main.mouseItem.Name != "")
                item = Main.mouseItem;
            if (item.pick + item.axe + item.hammer != 0)
                return null;

            if (OreExcavator.ExcavateHotkey.GetAssignedKeys(PlayerInput.UsingGamepad ? InputMode.XBoxGamepad : InputMode.Keyboard).Count <= 0) // || item.Name.ToLower().Contains("seed") || item.Name.ToLower().Contains("paint"))
            {
                OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.NoKey"), Color.Red);
                return null;
            }

            string inputKeyName = OreExcavator.ExcavateHotkey.GetAssignedKeys(PlayerInput.UsingGamepad ? InputMode.XBoxGamepad : InputMode.Keyboard)[0];

            if (inputKeyName is "Mouse1" or "RightTrigger")
                if (OreExcavator.ServerConfig.allowReplace && OreExcavator.ClientConfig.doSpecials)
                {
                    OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Keybind.MainActionWarning",
                        OreExcavator.myMod.Name, (PlayerInput.UsingGamepad ? "Trigger" : "Click")),
                        Color.Orange, LogType.Warn);
                    OreExcavator.ClientConfig.doSpecials = false;
                    OreExcavator.ClientConfig.showCursorTooltips = false;
                    // OreExcavator.ClientConfig.inititalChecks = true;
                    ExcavatorSystem.SaveConfig(OreExcavator.ClientConfig);
                    return true;
                }

            if (OreExcavator.ClientConfig.toggleExcavations ? (OreExcavator.excavationToggled is false) : (OreExcavatorKeybinds.excavatorHeld is false))
            {
                if (OreExcavator.ClientConfig.releaseCancelsExcavations && OreExcavator.playerHalt[Main.myPlayer] is true)
                {
                    OreExcavator.masterTiles.Clear();
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        ModPacket packet = OreExcavator.myMod.GetPacket();
                        packet.Write((byte)ActionType.ResetExcavations);
                        packet.Send();
                    }
                    OreExcavator.playerHalt[Main.myPlayer] = false;
                }
                return null;
            }

            if (OreExcavator.ClientConfig.doSpecials is false)
            {
                OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Client.DisabledAlternatives"), Color.Red);
                return null;
            }

            int x = Player.tileTargetX, y = Player.tileTargetY;

            if (x != OreExcavator.lookingCoordX || y != OreExcavator.lookingCoordY)
            {
                if (item.createTile >= TileID.Dirt || item.createWall > WallID.None)
                {
                    if (OreExcavator.ServerConfig.allowReplace is false)
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Server.DisabledSwap"), Color.Red);
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

                if (oldType == newType && oldSubtype == newSubtype)
                {
                    OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Unaltered"), Color.Red);
                    return null;
                }

                if (item.Name.EndsWith("seeds", StringComparison.OrdinalIgnoreCase))
                {
                    if (OreExcavator.ServerConfig.chainSeeding is false)
                    {
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Server.DisabledPlanting"), Color.Red);
                        return null;
                    }

                    actionType = ActionType.SeedPlanted;
                    createType = (short)item.createTile;
                }
                else
                {
                    createType = (short)item.createTile;

                    if (!oldHasTile)
                        if (OreExcavator.ServerConfig.chainPlacing is false)
                        {
                            OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Client.DisabledPlacing"), Color.Red);
                            return null;
                        }
                        else if (GetExtendsDirection(item.createTile) > Direction.None)
                        {
                            actionType = ActionType.ExtendPlacement;
                            OreExcavator.ModifySpooler(actionType, (ushort)x, (ushort)y,
                                (byte)OreExcavator.ClientConfig.recursionDelay,
                                OreExcavator.ClientConfig.recursionLimit,
                                OreExcavator.ClientConfig.doDiagonals,
                                (byte)Main.myPlayer,
                                0, 0, 0,
                                item.netID,
                                (sbyte)item.placeStyle
                            );
                            return true;
                        }
                        else
                            return null;
                    else if (!newHasTile)
                    {
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.DoesNotExist"), Color.Red);
                        return null;
                    }
                    else if (OreExcavator.ServerConfig.allowReplace is false)
                    {
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Server.DisabledSwap"), Color.Red);
                        return null;
                    }
                    else 
                        actionType = ActionType.TileReplaced;
                }
            }
            else if (item.createWall > WallID.None && item.createTile < item.createWall)
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

                actionType = ActionType.WallReplaced;
                createType = (short)item.createWall;
            }
            else if (item.paint is PaintID.None && item.Name.Contains("paint", StringComparison.OrdinalIgnoreCase))
            {
                if (OreExcavator.ServerConfig.chainPainting is false)
                {
                    OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Server.DisabledPainting"), Color.Red);
                    return null;
                }

                if (item.Name.EndsWith("brush", StringComparison.OrdinalIgnoreCase))
                {
                    if (oldColor == newColor)
                    {
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Unaltered"), Color.Red);
                        return null;
                    }
                    actionType = ActionType.TilePainted;
                    if (newHasTile)
                        for (ushort index = 0; index < player.inventory.Length; index++) // Not sure if this consumes in the correct order
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
                                    (sbyte)player.inventory[index].paint
                                );
                                return true;
                            }
                    OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.NoPaint"), Color.Orange);
                    return null;
                }
                else if (item.Name.EndsWith("roller", StringComparison.OrdinalIgnoreCase)) // EndsWith is 1000% faster than Contains when culture info is ignored
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
                                    (sbyte)player.inventory[index].paint
                                );
                                return true;
                            }
                    }
                    OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.NoPaint"), Color.Orange);
                    return null;
                }
                else if (item.Name.EndsWith("scraper", StringComparison.OrdinalIgnoreCase))
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
                || (createType == oldType && (actionType == ActionType.WallReplaced || actionType == ActionType.SeedPlanted)))
                return null;

            if (actionType is not ActionType.WallReplaced)
            {
                if (!Main.tile[x + 1, y].HasTile && !Main.tile[x, y + 1].HasTile && !Main.tile[x - 1, y].HasTile && !Main.tile[x, y - 1].HasTile)
                    if (!OreExcavator.ServerConfig.allowDiagonals || (!Main.tile[x + 1, y + 1].HasTile && !Main.tile[x + 1, y - 1].HasTile && !Main.tile[x - 1, y + 1].HasTile && !Main.tile[x - 1, y - 1].HasTile))
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Unanchored"), Color.Red);
                    else
                        return null;
            }
            else if (Main.tile[x + 1, y].WallType <= WallID.None && Main.tile[x, y + 1].WallType <= WallID.None && Main.tile[x - 1, y].WallType <= WallID.None && Main.tile[x, y - 1].WallType <= WallID.None)
            {
                OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Unanchored"), Color.Red);
                return null;
            }

            string itemName = OreExcavator.GetFullNameById(item.type, actionType);

            if (OreExcavator.ClientConfig.itemWhitelistAll is false && OreExcavator.ClientConfig.itemWhitelist.Contains(itemName) is false)
            {
                string keybind = OreExcavator.WhitelistHotkey.GetAssignedKeys(PlayerInput.UsingGamepad ? InputMode.XBoxGamepad : InputMode.Keyboard)[0] ?? "<Unbound>";
                if (keybind.StartsWith("oem", StringComparison.OrdinalIgnoreCase))
                    keybind = keybind[3..];
                else if (keybind == "Mouse2")
                    keybind = "Right Click";
                else if (keybind == "Mouse1")
                    keybind = "Left Click";
                
                OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Client.RejectSwapUnwhitelisted", $"ItemID.{item.Name} ({ item.netID})", keybind), Color.Orange);
                switch (actionType)
                {
                    case ActionType.TilePlaced:
                    case ActionType.TileReplaced:
                    case ActionType.SeedPlanted:
                    case ActionType.ExtendPlacement:
                        OreExcavator.lookingAtTile.TileType = (ushort)item.createTile;
                        break;

                    case ActionType.WallPlaced:
                    case ActionType.WallReplaced:
                        OreExcavator.lookingAtTile.WallType = (ushort)item.createWall;
                        break;

                    //case ActionType.TilePainted:
                    //case ActionType.WallPainted:
                    //case ActionType.ClearedPaint:
                    //    OreExcavator.lookingAtTile.TileColor = (ushort)item.createTile;
                    //    break;
                }
                
                return null;
            }

            if (OreExcavator.ServerConfig.itemBlacklistToggled && OreExcavator.ServerConfig.itemBlacklist.Contains(itemName))
            {
                OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Logging.Warnings.Client.RejectSwapBlacklisted", $"ItemID.{item.Name} ({ item.netID})"), Color.Orange);
                return null;
            }

            OreExcavator.ModifySpooler(
                actionType, (ushort)x, (ushort)y,
                (byte)OreExcavator.ClientConfig.recursionDelay,
                OreExcavator.ClientConfig.recursionLimit,
                (actionType == ActionType.SeedPlanted ? true : OreExcavator.ClientConfig.doDiagonals),
                (byte)Main.myPlayer,
                oldType,
                oldColor,
                oldSubtype,
                item.type,
                (sbyte)item.placeStyle
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
                if (item is not null && item.active && (source is null || source.Context is null || source.Context == "") && (item.createTile != -1 || item.createWall != -1))
                {
                    if (OreExcavator.ServerConfig.teleportLoot) // Add check for initial drop from excavations
                        if (OreExcavator.KillCalled || OreExcavator.Puppeting)
                        {
                            item.noGrabDelay = 0;
                            float closestDistance = -1f;
                            byte closestPlayer = 0;
                            for (short index = 0; index < Main.player.Length; index++)
                            {
                                if (OreExcavator.playerHalt[index])
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
            if (OreExcavator.ClientConfig.showItemTooltips is false || Main.netMode is NetmodeID.Server)
                return;

            string name = OreExcavator.GetFullNameById(item.type, ActionType.ItemBlackListed);

            // Keybind?
            if (OreExcavator.ExcavateHotkey.GetAssignedKeys(PlayerInput.UsingGamepad ? InputMode.XBoxGamepad : InputMode.Keyboard).Count <= 0)
            {
                tooltips.Add(new TooltipLine(OreExcavator.myMod, "HowToUse", $"[OE] [c/E11919:{Language.GetTextValue("Mods.OreExcavator.Keybind.None")}]"));
                return;
            }

            // Blacklisted?
            if (OreExcavator.ServerConfig.itemBlacklistToggled && OreExcavator.ServerConfig.itemBlacklist.Contains(name))
            {
                tooltips.Add(new TooltipLine(OreExcavator.myMod, "HowToUse", $"[OE] [c/E11919:{Language.GetTextValue("Mods.OreExcavator.UI.Tooltips.Blacklisted")}]"));
                return;
            }

            string keybind = OreExcavator.ExcavateHotkey.GetAssignedKeys(PlayerInput.UsingGamepad ? InputMode.XBoxGamepad : InputMode.Keyboard)[0];
            if (keybind.StartsWith("Oem"))
                keybind = keybind["Oem".Length..];
            else if (keybind == "Mouse2")
                keybind = "Right Click";
            else if (keybind == "Mouse1")
                keybind = "Left Click";

            // Pickaxe or Hammer?
            if ((item.pick > 0 && OreExcavator.ServerConfig.allowPickaxing) || (item.hammer > 0 && OreExcavator.ServerConfig.allowHammering))
                tooltips.Add(new TooltipLine(OreExcavator.myMod, "HowToUse", $"[OE] [c/32FF82:{Language.GetTextValue("Mods.OreExcavator.UI.Tooltips.HoldToExcavate", keybind)}]"));
            // Item?
            else if (OreExcavator.ClientConfig.doSpecials)
                // Tile or Wall
                if (item.createTile >= TileID.Dirt || item.createWall > WallID.None)
                {
                    if (OreExcavator.ClientConfig.itemWhitelistAll is false && OreExcavator.ClientConfig.itemWhitelist.Contains(name) is false)
                    {
                        string whitelistKey = OreExcavator.WhitelistHotkey.GetAssignedKeys(PlayerInput.UsingGamepad ? InputMode.XBoxGamepad : InputMode.Keyboard)[0];
                        tooltips.Add(new TooltipLine(OreExcavator.myMod, "HowToUse", $"[OE] [c/FFF014:{Language.GetTextValue("Mods.OreExcavator.UI.Tooltips.PressToWhitelist", whitelistKey)}]"));
                    }
                    else if (OreExcavator.GetExtendsDirection(item.createTile) > Direction.None)
                        tooltips.Add(new TooltipLine(OreExcavator.myMod, "HowToUse", $"[OE] [c/32FF82:{Language.GetTextValue("Mods.OreExcavator.UI.Tooltips.HoldToPlace", keybind)}]"));
                    else if (OreExcavator.ServerConfig.chainSeeding)
                        if (item.Name.EndsWith("seeds", StringComparison.OrdinalIgnoreCase))
                            tooltips.Add(new TooltipLine(OreExcavator.myMod, "HowToUse", $"[OE] [c/32FF82:{Language.GetTextValue("Mods.OreExcavator.UI.Tooltips.HoldToPlant", keybind)}]"));
                        else if (OreExcavator.ServerConfig.allowReplace)
                            tooltips.Add(new TooltipLine(OreExcavator.myMod, "HowToUse", $"[OE] [c/32FF82:{Language.GetTextValue("Mods.OreExcavator.UI.Tooltips.HoldToSwap", keybind)}]"));
                }
                // Paint
                else if (OreExcavator.ServerConfig.chainPainting && item.paint is PaintID.None && (item.Name.EndsWith("paintbrush", StringComparison.OrdinalIgnoreCase) || item.Name.EndsWith("paint roller", StringComparison.OrdinalIgnoreCase) || item.Name.EndsWith("paint scraper", StringComparison.OrdinalIgnoreCase)))
                    tooltips.Add(new TooltipLine(OreExcavator.myMod, "HowToUse", $"[OE] [c/32FF82:{Language.GetTextValue("Mods.OreExcavator.UI.Tooltips.HoldToPaint", keybind)}]"));
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
                (
                    OreExcavatorKeybinds.excavatorHeld ||
                    OreExcavator.excavationToggled
                ) &&
                (
                    (OreExcavator.ExcavateHotkey.GetAssignedKeys(InputMode.Keyboard).Count > 0 && OreExcavator.ExcavateHotkey.GetAssignedKeys(InputMode.Keyboard)[0] == "Mouse2")
                    //|| (OreExcavator.ExcavateHotkey.GetAssignedKeys(InputMode.XBoxGamepad).Count > 0 && OreExcavator.ExcavateHotkey.GetAssignedKeys(InputMode.XBoxGamepad)[0] == "B")
                )
               )
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
            
            _ = Task.Run(delegate // Create a new thread
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
            if (OreExcavator.ExcavateHotkey.GetAssignedKeys(PlayerInput.UsingGamepad ? InputMode.XBoxGamepad : InputMode.Keyboard).Count <= 0)
            {
                new Task(delegate
                {
                    Thread.Sleep(2000);
                    OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Keybind.NoBind", OreExcavator.myMod.DisplayName, OreExcavator.myMod.Version), Color.Red, LogType.Warn);
                }).Start();
            }
            else if (OreExcavator.ClientConfig.showWelcome080 && OreExcavator.ServerConfig.showWelcome)
            {
                new Task(delegate
                {
                    Thread.Sleep(2000);
                    OreExcavator.Log($"[{OreExcavator.myMod.DisplayName}] - v{OreExcavator.myMod.Version}" +
                                     "\n\t  We have a discord for bug reporting and feature suggestions:" +
                                     "\n\t  https://discord.gg/FtrsRtPe6h" +

                                   "\n\n\t  We've released a HUGE update with translations, fixes, and more!" +
                                     "\n\t  Most major issues have been fixed, and compatibility expanded." +
                                     "\n\t  We recommend resetting OE's configs, and re-editing them as you see fit." +
                                     "\n\t  If your language is missing, come help us translate the mod on Discord!" +

                                   "\n\n\t  Oh yeah, you can also disable this popup in your Client configs.", Color.SteelBlue, LogType.Info);
                }).Start();
            }
            else
            {
                // Add new update available logger?
            }
        }
    }

    class ExcavatorSystem : ModSystem
    {
        MethodInfo OnBindMethod = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.Config.UI.IntInputElement").GetMethod("OnBind", BindingFlags.Public | BindingFlags.Instance);

#if DEBUG
        public override void Load()
        {
            HookEndpointManager.Modify(OnBindMethod, OnBindIL);
        }

        public override void Unload()
        {
            HookEndpointManager.Unmodify(OnBindMethod, OnBindIL);
        }

        static void OnBindIL(ILContext il)
        {

            // Do IL stuff here...
            var c = new ILCursor(il);
            // Try to find where -20 is placed onto the stack
            if (!c.TryGotoNext(i => i.MatchLdcR4(180f)))
                return; // Patch unable to be applied
            // Remove the current op (180)
            c.Remove();
            //c.Emit(Mono.Cecil.Cil.OpCodes.Pop);
            // Push new width instance onto the stack
            c.Emit(Mono.Cecil.Cil.OpCodes.Ldc_R4, 80f);
        }
#endif

        /// <summary>
        /// For whatever reason, ModConfig has no native way of saving runtime changes to a config.
        /// So here I am, writing a file system to manually save changes, usually just whitelist changes.
        /// </summary>
        /// 
        /// <param name="config">The configuration object to write to storage from memory.</param>
        internal static void SaveConfig(ModConfig config)
        {
            OreExcavator.Log($"Saving config '{config.Name}' changes...");
            MethodInfo saveMethodInfo = typeof(ConfigManager).GetMethod("Save", BindingFlags.Static | BindingFlags.NonPublic);
            if (saveMethodInfo is not null)
            {
                saveMethodInfo.Invoke(null, new object[] { config });
                OreExcavator.Log($"Config '{config.Name}' saved, and updated!");
            }
            else
                throw new Exception($"A file could not be created, or updated at:"
                    + "\n'{path}'"
                    + "\n\n If you ARE using Onedrive, please reinstall tModloader in a different location."
                    + "\n If you ARE NOT using Onedrive, please disable Windows Real-Time protection.");
        }

        /// <summary>
        /// For whatever reason, ModConfig has no native way of saving runtime changes to a config.
        /// So here I am, writing a file system to manually save changes, usually just whitelist changes.
        /// </summary>
        /// 
        /// <param name="config">The configuration object to write to storage from memory.</param>
        internal static void LoadConfig(ModConfig config)
        {
            if (Main.netMode is NetmodeID.Server && Main.CurrentFrameFlags.ActivePlayersCount > 1)
            {
                OreExcavator.hostOnly = false;
                return;
            }

            OreExcavator.Log($"Loading config '{config.Name}' from files...");
            MethodInfo loadMethodInfo = typeof(ConfigManager).GetMethod("Load", BindingFlags.Static | BindingFlags.NonPublic);
            if (loadMethodInfo is not null)
            {
                loadMethodInfo.Invoke(null, new object[] { config });
                switch (config.Mode)
                {
                    case ConfigScope.ServerSide:
                        OreExcavator.ServerConfig = ModContent.GetInstance<OreExcavatorConfig_Server>();
                        break;
                    case ConfigScope.ClientSide:
                        OreExcavator.ClientConfig = ModContent.GetInstance<OreExcavatorConfig_Client>();
                        break;
                }
                OreExcavator.Log($"Config '{config.Name}' loaded, and updated!");
            }
            else
                throw new Exception($"A file could not be loaded, or accessed at:"
                    + "\n'{path}'"
                    + "\n\n If you ARE using Onedrive, please reinstall tModloader in a different location."
                    + "\n If you ARE NOT using Onedrive, please disable Windows Real-Time protection.");
        }
    }
}