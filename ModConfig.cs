using System.Collections.Generic;
using System.ComponentModel;

using Terraria.ID;
using Terraria.ModLoader.Config;

namespace OreExcavator
{
    [Label("Ore Excavator - Server Config")]
    public class OreExcavatorConfig_Server : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Header("Server Settings - Properties")]

        [Label("Block Modification Limit")]
        [Tooltip("Determines the maximum number of tiles" +
            "\nalterable per excavation for ALL clients!" +
            "\nThe smallest limit between the client and server will be used per client." +
            "\nSet to 0 to disable the mod, or max to let clients decide their own limits." +
            "\n\nLarger numbers WILL negatively affect performance!")]
        [Range(0, 10000)]
        [DefaultValue(10000)]
        public int recursionLimit;

        [Label("Allow Diagonal Searching")]
        [Tooltip("When enabled, clients will be allowed to also" +
            "\ncheck for matches diagonal of the source when searching." +
            "\n\nDisabling this WILL slightly improve performance!")]
        [DefaultValue(false)]
        public bool allowDiagonals;

        [Label("Allow Chain Planting")]
        [Tooltip("When enabled, clients will be allowed to chain-plant seeds." +
            "\nDoes NOT work with saplings (yet!), only grass." +
            "\n\nDoes NOT impact performance!")]
        [DefaultValue(true)]
        public bool chainPlanting;

        [Label("Allow Chain Painting")]
        [Tooltip("COMING SOON!")]
        [DefaultValue(true)]
        public bool chainPainting => false;

        [Label("Allow Quick Whitelist Keys")]
        [Tooltip("When enabled, using the whitelist keybinds will" +
            "\nadd/remove hovered tiles/walls/items to/from their client whitelist" +
            "\n\nDoes NOT impact performance!")]
        [DefaultValue(true)]
        public bool allowQuickWhitelisting;

        [Label("Teleport Loot to Player")]
        [Tooltip("When enabled, excavated drops will be warped to" +
            "\nthe player that commanded the excavation(s)." +
            "\n\nDisabling this may improve performance" +
            "\n\nDOES NOT WORK ON SERVERS YET!")]
        [DefaultValue(true)]
        public bool teleportItems;

        [Label("Make Loot Lava-Proof")]
        [Tooltip("COMING SOON!")]
        [DefaultValue(false)]
        public bool safeItems => false;

        [Label("Creative Mode")]
        [Tooltip("When enabled, items won't drop, items won't" +
            "\nbe consumed, and mining power will be ignored." +
            "\n\nEnabling this may improve performance," +
            "\nbut may be considered cheating..." +
            "\n\nWORK IN PROGRESS!!")]
        [DefaultValue(false)]
        public bool creativeMode;

        [Label("Use Agressive Mod Compatibility")]
        [Tooltip("When enabled, extra checks will be enforced in attempt" +
            "\nto properly bind modded tiles, tools, walls, and items" +
            "\n\nDisabling this may improve performance, at the cost of instabilities!" +
            "\n\nWORK IN PROGRESS!!")]
        [DefaultValue(true)]
        public bool agressiveCompatibility;


        [Header("Server Settings - Blocks")]

        [Label("Allow Pickaxe Excavations")]
        [Tooltip("When enabled, the excavation algorithm will" +
            "\nbe allowed for blocks when using a sufficient pickaxe." +
            "\n\nDoes NOT impact performance!")]
        [DefaultValue(true)]
        public bool allowPickaxing;

        [Label("Enable Tile Blacklist")]
        [Tooltip("When enabled, the server will enforce the Tile blacklist on its clients" +
            "\nDisable this to give clients free whitelist controls over Tiles" +
            "\n\nDoes NOT impact performance!")]
        [DefaultValue(true)]
        public bool tileBlacklistToggled;

        [Label("Tile Blacklist")]
        [Tooltip("Configure this list to manually set what Tiles CANNOT be chain-excavated" +
            "\nPrefixed by the mod that owns them. Client whitelists are overruled." +
            "\n\nDoes NOT impact performance!")]
        [DefaultListValue("Terraria:")]
        public HashSet<string> tileBlacklist = new() {
            "Terraria:" + TileID.Search.GetName(TileID.Dirt),
            "Terraria:" + TileID.Search.GetName(TileID.Stone),
            "Terraria:" + TileID.Search.GetName(TileID.Mud),
            "Terraria:" + TileID.Search.GetName(TileID.Ebonstone),
            "Terraria:" + TileID.Search.GetName(TileID.Crimstone),
            "Terraria:" + TileID.Search.GetName(TileID.Pearlstone),
            "Terraria:" + TileID.Search.GetName(TileID.BlueDungeonBrick),
            "Terraria:" + TileID.Search.GetName(TileID.GreenDungeonBrick),
            "Terraria:" + TileID.Search.GetName(TileID.PinkDungeonBrick)
        };


        [Header("Server Settings - Walls")]

        [Label("Allow Hammer Excavations")]
        [Tooltip("When enabled, the excavation algorithm will" +
            "\nbe allowed for walls when using a sufficient hammer." +
            "\n\nDoes NOT impact performance!")]
        [DefaultValue(true)]
        public bool allowHammering;

        [Label("Enable Wall Blacklist")]
        [Tooltip("When enabled, the server will enforce the Wall blacklist on its clients" +
            "\nDisable this to give clients free whitelist controls over Walls" +
            "\n\nDoes NOT impact performance!")]
        [DefaultValue(true)]
        public bool WallBlacklistToggled;

        [Label("Wall Blacklist")]
        [Tooltip("Configure this list to manually set what Walls CANNOT be chain-excavated" +
            "\nPrefixed by the mod that owns them. Client whitelists are overruled." +
            "\n\nDoes NOT impact performance!")]
        [DefaultListValue("Terraria:")]
        public HashSet<string> wallBlacklist = new() {
            "Terraria:" + WallID.Search.GetName(WallID.Stone),
        };


        [Header("Server Settings - Blockswap")]

        [Label("Allow Blockswap Excavations")]
        [Tooltip("When enabled, the excavation algorithm will" +
            "\nbe allowed for veinswaps when replacing a tile/wall." +
            "\n\nSlightly impacts performance!")]
        [DefaultValue(true)]
        public bool allowReplace;

        [Label("Enable Item Blacklist")]
        [Tooltip("When enabled, the server will enforce the Item blacklist on its clients" +
            "\nDisable this to give clients free whitelist controls over Items" +
            "\n\nDoes NOT impact performance!")]
        [DefaultValue(true)]
        public bool itemBlacklistToggled;

        [Label("Item Blacklist")]
        [Tooltip("If you don't know what this is, you probably shouldn't touch it...\nThis controls what items are forbidden by clients for whitelisting\n\nDoes NOT impact performance!")]
        [DefaultListValue("Terraria:")]
        public HashSet<string> itemBlacklist = new() {
            //"Terraria:" + ItemID.Search.GetName(WallID.Wood),
        };
    }

    [Label("Ore Excavator - Client Config")]
    public class OreExcavatorConfig_Client : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;


        [Header("Client Settings - UI")]

        [Label("Show Startup Message")]
        [Tooltip("When disabled, welcome messages will" +
            "\nbe hidden for this version of the mod." +
            "\n\nNew versions will re-enable this feature.")]
        [DefaultValue(true)]
        public bool showWelcome064;

        [Label("Show Excavation Tooltip")]
        [Tooltip("When disabled, holding the" +
            "\nexcavation key will no longer provide" +
            "\na contextual tooltip." +
            "\n\nPlease enable this before reporting bugs!")]
        [DefaultValue(true)]
        public bool showTooltips;

        [Label("Show Debug Logs")]
        [Tooltip("When enabled, debug logs" +
            "\nwill be hidden from the files." +
            "\n\nPlease enable this before reporting bugs!")]
        [DefaultValue(false)]
        public bool doDebugStuff;

        [Header("Client Settings - Core")]

        [Label("Block Modification Limit")]
        [Tooltip("Determines the maximum number of tiles" +
            "\nalterable per excavation" +
            "\n\nLarger numbers WILL negatively affect performance!")]
        [Range(0, 10000)]
        [DefaultValue(600)]
        public int recursionLimit;

        [Label("Do Diagonal Searching")]
        [Tooltip("When enabled, the excavation algorithm will" +
            "\nalso check for matches directly diagonal of themselves." +
            "\n\nDisabling this WILL improve performance!")]
        [DefaultValue(false)]
        public bool doDiagonals;

        [Label("Block Breaking Delay")]
        [Tooltip("The ms delay between block breaks." +
            "\n\nHigher values may improve performance!")]
        [Range(0, 10000)]
        [DefaultValue(10)]
        public int recursionDelay;

        [Label("Do Initial Whitelist Checks")]
        [Tooltip("When enabled, the algorithm checks the whitelists & blacklists BEFORE" +
            "\nand AFTER hitting attempting an excavation, rather than just AFTER." +
            "\n\nDisabling this may improve performance, but" +
            "\ncause potentially unexpected results!")]
        [DefaultValue(false)]
        public bool inititalChecks;


        [Header("Client Settings - Controls")]

        [Label("Keybind Toggles Excavations")]
        [Tooltip("When enabled, tapping the keybind will toggle the" +
            "\ncontrol will cease all excavation operations." +
            "\n\nDoes NOT impact performance!")]
        [DefaultValue(false)]
        public bool toggleExcavations;

        [Label("Cancel Excavation on Keybind Release")]
        [Tooltip("When enabled, letting go of the Excavation" +
            "\ncontrol key will cease all excavation operations." +
            "\n\nDoes NOT impact performance!")]
        [DefaultValue(true)]
        public bool releaseCancelsExcavation;

        [Label("Enable Alternative Features")]
        [Tooltip("When enabled, the client will allow for special non-veinmine actions" +
            "\nDisable this if you don't plan on using these features, or are binding excavations to Mouse1" +
            "\n\nModerately impacts performance!")]
        [DefaultValue(true)]
        public bool doSpecials;


        [Header("Client Settings - Blocks")]

        [Label("Tile Whitelist")]
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
            "Terraria:" + TileID.Search.GetName(TileID.LunarOre)
        };


        [Header("Client Settings - Walls")]

        [Label("Wall Whitelist")]
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
            //"Terraria:" + WallID.Search.GetName(WallID.SpiderUnsafe),
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


        [Header("Client Settings - Blockswap")]

        [Label("Item Whitelist")]
        [Tooltip("Configure this list to manually set what Items can be chain-replaced" +
            "\nPrefixed by the mod that owns them. Also yields to the host's blacklist." +
            "\n\nDoes NOT impact performance!")]
        [DefaultListValue("Terraria:")]
        public HashSet<string> itemWhitelist = new()
        {
            "Terraria:" + ItemID.Search.GetName(ItemID.DirtBlock),
            "Terraria:" + ItemID.Search.GetName(ItemID.StoneBlock),
            "Terraria:" + ItemID.Search.GetName(ItemID.Wood),
            "Terraria:" + ItemID.Search.GetName(ItemID.StoneWall),
            "Terraria:" + ItemID.Search.GetName(ItemID.DirtWall),
            "Terraria:" + ItemID.Search.GetName(ItemID.CorruptSeeds),
            "Terraria:" + ItemID.Search.GetName(ItemID.WoodWall),
        };


        [Header("NOTICE")]

        [Label("Join a world to edit Server Settings!")]
        [Tooltip("tModLoader has broken server settings from this menu for now!")]
        [DefaultValue(false)]
        public bool noticeClient1 => false;
    }
}