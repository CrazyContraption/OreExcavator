using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader.Config;

namespace OreExcavator
{
    [Label("$Mods.OreExcavator.Config.Server.Header")]
    public class OreExcavatorConfig_Server : ModConfig
    {
        internal const string IconBuffer = " : ";
        internal const string HeaderBuffer = " - ";

        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Header("World Settings" + HeaderBuffer + "Properties")]

        [Label("$Mods.OreExcavator.Config.Server.RecursionLimit.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Server.RecursionLimit.Description")]
        [Range(int.MinValue, int.MaxValue)]
        [DefaultValue(10000)]
        public int recursionLimit;

        [Label("$Mods.OreExcavator.Config.Server.AllowDiagonals.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Server.AllowDiagonals.Description")]
        [DefaultValue(true)]
        public bool allowDiagonals;

        [Label("$Mods.OreExcavator.Config.Server.ChainSeeding.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Server.ChainSeeding.Description")]
        [DefaultValue(true)]
        public bool chainSeeding;

        [Label("$Mods.OreExcavator.Config.Server.ChainPainting.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Server.ChainPainting.Description")]
        [DefaultValue(true)]
        public bool chainPainting;

        [Label("$Mods.OreExcavator.Config.Server.AllowQuickWhitelisting.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Server.AllowQuickWhitelisting.Description")]
        [DefaultValue(true)]
        public bool allowQuickWhitelisting;

        [Label("$Mods.OreExcavator.Config.Server.ManaConsumption.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Server.ManaConsumption.Description")]
        [DefaultValue(0)]
        [Increment(0.5f)]
        [DrawTicks]
        [Range(0,6f)]
        [Slider]
        public float manaConsumption;

        [Label("$Mods.OreExcavator.Config.Server.TeleportLoot.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Server.TeleportLoot.Description")]
        [DefaultValue(false)]
        public bool teleportLoot;

        [Label("$Mods.OreExcavator.Config.Server.SafeItems.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Server.SafeItems.Description")]
        [DefaultValue(false)]
        public bool safeItems;

        [Label("$Mods.OreExcavator.Config.Server.CreativeMode.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Server.CreativeMode.Description")]
        [DefaultValue(false)]
        public bool creativeMode;

        [Label("$Mods.OreExcavator.Config.Server.AggressiveCompatibility.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Server.AggressiveCompatibility.Description")]
        [DefaultValue(true)]
        public bool aggressiveCompatibility;


        [Header("World Settings" + HeaderBuffer + "Blocks")]

        [Label("$Mods.OreExcavator.Config.Server.AllowPickaxing.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Server.AllowPickaxing.Description")]
        [DefaultValue(true)]
        public bool allowPickaxing;

        [Label("$Mods.OreExcavator.Config.Server.TileBlacklistToggled.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Server.TileBlacklistToggled.Description")]
        [DefaultValue(true)]
        public bool tileBlacklistToggled;

        [Label("$Mods.OreExcavator.Config.Server.TileBlacklist.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Server.TileBlacklist.Description")]
        [DefaultListValue("Terraria:")]
        public HashSet<string> tileBlacklist = new() {
            "Terraria:" + TileID.Search.GetName(TileID.Dirt),
            "Terraria:" + TileID.Search.GetName(TileID.Stone),
            "Terraria:" + TileID.Search.GetName(TileID.Sand),
            "Terraria:" + TileID.Search.GetName(TileID.Mud),
            "Terraria:" + TileID.Search.GetName(TileID.Ebonstone),
            "Terraria:" + TileID.Search.GetName(TileID.Crimstone),
            "Terraria:" + TileID.Search.GetName(TileID.Pearlstone),
            "Terraria:" + TileID.Search.GetName(TileID.BlueDungeonBrick),
            "Terraria:" + TileID.Search.GetName(TileID.GreenDungeonBrick),
            "Terraria:" + TileID.Search.GetName(TileID.PinkDungeonBrick),
            "Terraria:" + TileID.Search.GetName(TileID.Containers),
            "Terraria:" + TileID.Search.GetName(TileID.Pots),
            "Terraria:" + TileID.Search.GetName(TileID.Heart)
        };


        [Header("World Settings" + HeaderBuffer + "Walls")]

        [Label("$Mods.OreExcavator.Config.Server.AllowHammering.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Server.AllowHammering.Description")]
        [DefaultValue(true)]
        public bool allowHammering;

        [Label("$Mods.OreExcavator.Config.Server.WallBlacklistToggled.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Server.WallBlacklistToggled.Description")]
        [DefaultValue(true)]
        public bool wallBlacklistToggled;

        [Label("$Mods.OreExcavator.Config.Server.WallBlacklist.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Server.WallBlacklist.Description")]
        [DefaultListValue("Terraria:")]
        public HashSet<string> wallBlacklist = new() {
            "Terraria:" + WallID.Search.GetName(WallID.SpiderUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.BlueDungeonUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.PinkDungeonUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.GreenDungeonUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.BlueDungeonTileUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.PinkDungeonTileUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.GreenDungeonTileUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.BlueDungeonSlabUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.PinkDungeonSlabUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.GreenDungeonSlabUnsafe)
        };


        [Header("World Settings - Blockswap")]

        [Label("$Mods.OreExcavator.Config.Server.AllowReplace.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Server.AllowReplace.Description")]
        [DefaultValue(true)]
        public bool allowReplace;

        [Label("$Mods.OreExcavator.Config.Server.ItemBlacklistToggled.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Server.ItemBlacklistToggled.Description")]
        [DefaultValue(true)]
        public bool itemBlacklistToggled;

        [Label("$Mods.OreExcavator.Config.Server.ItemBlacklist.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Server.ItemBlacklist.Description")]
        [DefaultListValue("Terraria:")]
        public HashSet<string> itemBlacklist = new() {
            "Terraria:" + ItemID.Search.GetName(ItemID.CopperCoin),
            "Terraria:" + ItemID.Search.GetName(ItemID.SilverCoin),
            "Terraria:" + ItemID.Search.GetName(ItemID.GoldCoin),
            "Terraria:" + ItemID.Search.GetName(ItemID.PlatinumCoin),
            "Terraria:" + ItemID.Search.GetName(ItemID.AdamantiteBar),
            "Terraria:" + ItemID.Search.GetName(ItemID.ChlorophyteBar),
            "Terraria:" + ItemID.Search.GetName(ItemID.CobaltBar),
            "Terraria:" + ItemID.Search.GetName(ItemID.CopperBar),
            "Terraria:" + ItemID.Search.GetName(ItemID.CrimtaneBar),
            "Terraria:" + ItemID.Search.GetName(ItemID.DemoniteBar),
            "Terraria:" + ItemID.Search.GetName(ItemID.GoldBar),
            "Terraria:" + ItemID.Search.GetName(ItemID.HallowedBar),
            "Terraria:" + ItemID.Search.GetName(ItemID.HellstoneBar),
            "Terraria:" + ItemID.Search.GetName(ItemID.IronBar),
            "Terraria:" + ItemID.Search.GetName(ItemID.LeadBar),
            "Terraria:" + ItemID.Search.GetName(ItemID.LunarBar),
            "Terraria:" + ItemID.Search.GetName(ItemID.MeteoriteBar),
            "Terraria:" + ItemID.Search.GetName(ItemID.MythrilBar),
            "Terraria:" + ItemID.Search.GetName(ItemID.OrichalcumBar),
            "Terraria:" + ItemID.Search.GetName(ItemID.PalladiumBar),
            "Terraria:" + ItemID.Search.GetName(ItemID.PlatinumBar),
            "Terraria:" + ItemID.Search.GetName(ItemID.ShroomiteBar),
            "Terraria:" + ItemID.Search.GetName(ItemID.SilverBar),
            "Terraria:" + ItemID.Search.GetName(ItemID.SpectreBar),
            "Terraria:" + ItemID.Search.GetName(ItemID.TinBar),
            "Terraria:" + ItemID.Search.GetName(ItemID.TitaniumBar),
            "Terraria:" + ItemID.Search.GetName(ItemID.TungstenBar)
        };

        public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref string message)
        {
            if (OreExcavator.hostOnly && Main.CurrentFrameFlags.ActivePlayersCount <= 0 && whoAmI == 0)
            {
                message = "Accepting changes (Reason: Changes requested via remote)";
                return true;
            }

            if (OreExcavator.hostOnly && Main.CurrentFrameFlags.ActivePlayersCount == 1 && whoAmI == 0)
            {
                message = "Accepting changes (Reason: Verified first/only player online is host)";
                return true;
            }

            OreExcavator.hostOnly = false;
            message = "Rejected changes (Reason: Cannot determine host)";
            return false;
        }
    }

    [Label("Player Config")]
    public class OreExcavatorConfig_Client : ModConfig
    {
        internal const string IconBuffer = OreExcavatorConfig_Server.IconBuffer;
        internal const string HeaderBuffer = OreExcavatorConfig_Server.HeaderBuffer;
        public override ConfigScope Mode => ConfigScope.ClientSide;


        [Header("Player Settings - UI")]

        [Label("[i/p0:4792] | Show Startup Message")]
        [Tooltip("When disabled, welcome messages will" +
            "\nbe hidden for this version of the mod." +
            "\n\nNew versions will re-enable this feature.")]
        [DefaultValue(true)]
        public bool showWelcome070;

        [Label("[i/p0:5075] | Show Excavation Tooltip")]
        [Tooltip("When disabled, holding the excavation " +
            "\nkey will no longer provide a contextual tooltip." +
            "\n\nPlease enable this before reporting bugs!")]
        [DefaultValue(true)]
        public bool showCursorTooltips;

        [Label("[i/p0:267] | Show Item Tooltips")]
        [Tooltip("When disabled, items, walls, and tiles the" +
            "\n will no longer provide a contextual tooltip." +
            "\n\nPlease enable this before reporting bugs!")]
        [DefaultValue(true)]
        public bool showItemTooltips;

        [Label("[i/p0:150] | Reduced Effects")]
        [Tooltip("Lagging? When disabled, the mod will attempt to run in a" +
            "\n reduced state, providing better performance graphically." +
            "\n\nWill not change anything functionally outside of cosmetic differences.")]
        [DefaultValue(false)]
        public bool reducedEffects;

        [Label("[i/p0:555] | Auto-use Mana Potions for Excavations")]
        [Tooltip("When enabled and the world has mana requirements turned on," +
            "\nshould the mod attempt to refill your mana if you run out?" +
            "\n\nDoes NOT impact performance!")]
        [DefaultValue(false)]
        public bool refillMana;

        [Label("[i/p0:37] | Show Debug Logs")]
        [Tooltip("When enabled, debug logs" +
            "\nwill be hidden from the files." +
            "\n\nPlease enable this before reporting bugs!")]
        [DefaultValue(false)]
        public bool doDebugStuff;

        [Header("Player Settings - Core")]

        [Label("[i/p0:18] | Block Modification Limit")]
        [Tooltip("Determines the maximum number of tiles" +
            "\nalterable per excavation" +
            "\n\nLarger numbers WILL negatively affect performance!")]
        [Range(int.MinValue, int.MaxValue)]
        [DefaultValue(600)]
        public int recursionLimit;

        [Label("[i/p0:2799] | Do Diagonal Searching")]
        [Tooltip("When enabled, the excavation algorithm will" +
            "\nalso check for matches directly diagonal of themselves." +
            "\n\nDisabling this WILL improve performance!")]
        [DefaultValue(false)]
        public bool doDiagonals;

        [Label("[i/p0:3099] | Block Breaking Delay")]
        [Tooltip("The ms delay between block breaks." +
            "\n\nHigher values may improve performance!")]
        [Range(byte.MinValue, byte.MaxValue)]
        [DefaultValue(10)]
        public int recursionDelay;

        [Label("[i/p0:321] | Do Initial Whitelist Checks")]
        [Tooltip("When enabled, the algorithm checks the whitelists & blacklists BEFORE" +
            "\nattempting an excavation, when bound to a right mouse." +
            "\n\nEnabling this may hurt performance, but" +
            "\nmight produce more stable behaviour!")]
        [DefaultValue(false)]
        public bool inititalChecks;


        [Header("Player Settings - Blocks")]

        [Label("[i/p0:3509] | Enable Tile Whitelist")]
        [Tooltip("When enabled, all tiles will be whitelisted by default." +
            "\n\nSlightly improves performance!")]
        [DefaultValue(true)]
        public bool tileWhitelistToggled;

        [Label("[i/p0:2695] | Tile Whitelist")]
        [Tooltip("Configure this list to manually set what Tiles can be chain-excavated" +
            "\nPrefixed by the mod that owns them. Also yields to the host's blacklist." +
            "\n\nDoes NOT impact performance!")]
        [DefaultListValue("Terraria:")]
        public HashSet<string> tileWhitelist = new()
        {
            "Terraria:" + TileID.Search.GetName(TileID.Iron),
            "Terraria:" + TileID.Search.GetName(TileID.Copper),
            "Terraria:" + TileID.Search.GetName(TileID.Gold),
            "Terraria:" + TileID.Search.GetName(TileID.Silver),
            "Terraria:" + TileID.Search.GetName(TileID.Demonite),
            "Terraria:" + TileID.Search.GetName(TileID.Meteorite),
            "Terraria:" + TileID.Search.GetName(TileID.ClayBlock),
            "Terraria:" + TileID.Search.GetName(TileID.Obsidian),
            "Terraria:" + TileID.Search.GetName(TileID.Hellstone),
            "Terraria:" + TileID.Search.GetName(TileID.Sapphire),
            "Terraria:" + TileID.Search.GetName(TileID.Ruby),
            "Terraria:" + TileID.Search.GetName(TileID.Emerald),
            "Terraria:" + TileID.Search.GetName(TileID.Topaz),
            "Terraria:" + TileID.Search.GetName(TileID.Amethyst),
            "Terraria:" + TileID.Search.GetName(TileID.Diamond),
            "Terraria:" + TileID.Search.GetName(TileID.Cobalt),
            "Terraria:" + TileID.Search.GetName(TileID.Mythril),
            "Terraria:" + TileID.Search.GetName(TileID.Adamantite),
            "Terraria:" + TileID.Search.GetName(TileID.Silt),
            "Terraria:" + TileID.Search.GetName(TileID.BreakableIce),
            "Terraria:" + TileID.Search.GetName(TileID.Tin),
            "Terraria:" + TileID.Search.GetName(TileID.Lead),
            "Terraria:" + TileID.Search.GetName(TileID.Tungsten),
            "Terraria:" + TileID.Search.GetName(TileID.Platinum),
            "Terraria:" + TileID.Search.GetName(TileID.ExposedGems),
            "Terraria:" + TileID.Search.GetName(TileID.Crimtane),
            "Terraria:" + TileID.Search.GetName(TileID.Chlorophyte),
            "Terraria:" + TileID.Search.GetName(TileID.Palladium),
            "Terraria:" + TileID.Search.GetName(TileID.Orichalcum),
            "Terraria:" + TileID.Search.GetName(TileID.Titanium),
            "Terraria:" + TileID.Search.GetName(TileID.Slush),
            "Terraria:" + TileID.Search.GetName(TileID.DesertFossil),
            "Terraria:" + TileID.Search.GetName(TileID.FossilOre),
            "Terraria:" + TileID.Search.GetName(TileID.LunarOre),
            "Terraria:" + TileID.Search.GetName(TileID.CrackedBlueDungeonBrick),
            "Terraria:" + TileID.Search.GetName(TileID.CrackedGreenDungeonBrick),
            "Terraria:" + TileID.Search.GetName(TileID.CrackedPinkDungeonBrick),
            "Terraria:" + TileID.Search.GetName(TileID.Spikes)
        };


        [Header("Player Settings - Walls")]

        [Label("[i/p0:196] | Enable Wall Whitelist")]
        [Tooltip("When enabled, all walls will be whitelisted by default." +
            "\n\nSlightly improves performance!")]
        [DefaultValue(true)]
        public bool wallWhitelistToggled;

        [Label("[i/p0:2696] | Wall Whitelist")]
        [Tooltip("Configure this list to manually set what Walls can be chain-excavated" +
            "\nPrefixed by the mod that owns them. Also yields to the hosts's blacklist." +
            "\n\nDoes NOT impact performance!")]
        [DefaultListValue("Terraria:")]
        public HashSet<string> wallWhitelist = new()
        {
            //"Terraria:" + WallID.Search.GetName(WallID.Stone),
            "Terraria:" + WallID.Search.GetName(WallID.DirtUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.EbonstoneUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.HellstoneBrickUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.ObsidianBrickUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.MudUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.Dirt),
            "Terraria:" + WallID.Search.GetName(WallID.PearlstoneBrickUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.SnowWallUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.AmethystUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.TopazUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.SapphireUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.EmeraldUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.RubyUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.DiamondUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.CaveUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.Cave2Unsafe),
            "Terraria:" + WallID.Search.GetName(WallID.Cave3Unsafe),
            "Terraria:" + WallID.Search.GetName(WallID.Cave4Unsafe),
            "Terraria:" + WallID.Search.GetName(WallID.Cave5Unsafe),
            "Terraria:" + WallID.Search.GetName(WallID.Cave6Unsafe),
            "Terraria:" + WallID.Search.GetName(WallID.Cave7Unsafe),
            "Terraria:" + WallID.Search.GetName(WallID.GrassUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.JungleUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.FlowerUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.CorruptGrassUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.HallowedGrassUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.IceUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.ObsidianBackUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.MushroomUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.CrimsonGrassUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.CrimstoneUnsafe),
            //"Terraria:" + WallID.Search.GetName(WallID.HiveUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.MarbleUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.GraniteUnsafe),
            "Terraria:" + WallID.Search.GetName(WallID.Cave8Unsafe),
            "Terraria:" + WallID.Search.GetName(WallID.CorruptionUnsafe1),
            "Terraria:" + WallID.Search.GetName(WallID.CorruptionUnsafe2),
            "Terraria:" + WallID.Search.GetName(WallID.CorruptionUnsafe3),
            "Terraria:" + WallID.Search.GetName(WallID.CorruptionUnsafe4),
            "Terraria:" + WallID.Search.GetName(WallID.CrimsonUnsafe1),
            "Terraria:" + WallID.Search.GetName(WallID.CrimsonUnsafe2),
            "Terraria:" + WallID.Search.GetName(WallID.CrimsonUnsafe3),
            "Terraria:" + WallID.Search.GetName(WallID.CrimsonUnsafe4),
            "Terraria:" + WallID.Search.GetName(WallID.DirtUnsafe1),
            "Terraria:" + WallID.Search.GetName(WallID.DirtUnsafe2),
            "Terraria:" + WallID.Search.GetName(WallID.DirtUnsafe3),
            "Terraria:" + WallID.Search.GetName(WallID.DirtUnsafe4),
            "Terraria:" + WallID.Search.GetName(WallID.HallowUnsafe1),
            "Terraria:" + WallID.Search.GetName(WallID.HallowUnsafe2),
            "Terraria:" + WallID.Search.GetName(WallID.HallowUnsafe3),
            "Terraria:" + WallID.Search.GetName(WallID.HallowUnsafe4),
            "Terraria:" + WallID.Search.GetName(WallID.JungleUnsafe1),
            "Terraria:" + WallID.Search.GetName(WallID.JungleUnsafe2),
            "Terraria:" + WallID.Search.GetName(WallID.JungleUnsafe3),
            "Terraria:" + WallID.Search.GetName(WallID.JungleUnsafe4),
            "Terraria:" + WallID.Search.GetName(WallID.LavaUnsafe1),
            "Terraria:" + WallID.Search.GetName(WallID.LavaUnsafe2),
            "Terraria:" + WallID.Search.GetName(WallID.LavaUnsafe3),
            "Terraria:" + WallID.Search.GetName(WallID.LavaUnsafe4),
            "Terraria:" + WallID.Search.GetName(WallID.RocksUnsafe1),
            "Terraria:" + WallID.Search.GetName(WallID.RocksUnsafe2),
            "Terraria:" + WallID.Search.GetName(WallID.RocksUnsafe3),
            "Terraria:" + WallID.Search.GetName(WallID.RocksUnsafe4)
        };


        [Header("Player Settings - Blockswap")]

        [Label("[i/p0:9] | Enable Item Whitelist")]
        [Tooltip("When enabled, all items will be whitelisted by default." +
            "\n\nSlightly improves performance!")]
        [DefaultValue(true)]
        public bool itemWhitelistToggled;

        [Label("[i/p0:38] | Item Whitelist")]
        [Tooltip("Configure this list to manually set what Items can be chain-replaced" +
            "\nPrefixed by the mod that owns them. Also yields to the host's blacklist." +
            "\n\nDoes NOT impact performance!")]
        [DefaultListValue("Terraria:")]
        public HashSet<string> itemWhitelist = new()
        {
            /*"Terraria:" + ItemID.Search.GetName(ItemID.DirtBlock),
            "Terraria:" + ItemID.Search.GetName(ItemID.StoneBlock),
            "Terraria:" + ItemID.Search.GetName(ItemID.Wood),
            "Terraria:" + ItemID.Search.GetName(ItemID.StoneWall),
            "Terraria:" + ItemID.Search.GetName(ItemID.DirtWall),
            "Terraria:" + ItemID.Search.GetName(ItemID.CorruptSeeds),
            "Terraria:" + ItemID.Search.GetName(ItemID.WoodWall),*/
        };


        [Header("Player Settings - Controls")]

        [Label("[i/p0:513] | Keybind Toggles Excavations")]
        [Tooltip("When enabled, tapping the keybind will toggle the" +
            "\nactive state of initiating excavations." +
            "\n\nDoes NOT impact performance!")]
        [DefaultValue(false)]
        public bool toggleExcavations;

        [Label("[i/p0:166] | Cancel Excavations on Keybind Release")]
        [Tooltip("When enabled, letting go of the Excavation" +
            "\ncontrol key will cease all excavation operations." +
            "\n\nNOT ADVISED ON MULTIPLAYER SERVERS!!" +
            "\n\nDoes NOT impact performance!")]
        [DefaultValue(false)]
        public bool releaseCancelsExcavations;

        [Label("[i/p0:3230] | Enable Alternative Features")]
        [Tooltip("When enabled, the client will allow for special non-veinmine actions" +
            "\nDisable this if you don't plan on using these features, or are binding excavations to Mouse1" +
            "\n\nModerately impacts performance!")]
        [DefaultValue(true)]
        public bool doSpecials;

        [Label("[g:25] | Looking for your keybind?")]
        [Tooltip("Set your keybind in the vanilla controls area," +
            "\nThis is just for display purposes and to direct confused users.")]
        [DefaultValue("Unknown")]
        [ReadOnly(true)]
        [JsonIgnore]
        public string keybind => (OreExcavator.ExcavateHotkey != null ? (OreExcavator.ExcavateHotkey.GetAssignedKeys().Count > 0 ? OreExcavator.ExcavateHotkey.GetAssignedKeys()[0] : "Not Set") : "Unknown");
    }
}