using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using Terraria.ID;
using Terraria.ModLoader.Config;

namespace OreExcavator
{
    [Label("World Config")]
    public class OreExcavatorConfig_Server : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Header("World Settings - Properties")]

        [Label("Block Modification Limit")]
        [Tooltip("Determines the maximum number of tiles" +
            "\nalterable per excavation for ALL clients!" +
            "\nThe smallest limit between the client and server will be used per client." +
            "\nSet to 0 to disable the mod, or max to let players decide their own limits." +
            "\n\nLarger numbers WILL negatively affect performance!")]
        [Range(0, 10000000)]
        [DefaultValue(10000)]
        [ReloadRequired]
        public int recursionLimit;

        [Label("Allow Diagonal Searching")]
        [Tooltip("When enabled, players will be allowed to also" +
            "\ncheck for matches diagonal of the source when searching." +
            "\n\nDisabling this WILL slightly improve performance!")]
        [DefaultValue(true)]
        [ReloadRequired]
        public bool allowDiagonals;

        [Label("Allow Chain Seeding")]
        [Tooltip("When enabled, players will be allowed to chain-plant seeds." +
            "\nDoes NOT work with saplings (yet!), only grasses." +
            "\n\nDoes NOT impact performance!")]
        [DefaultValue(true)]
        [ReloadRequired]
        public bool chainSeeding;

        [Label("Allow Chain Painting")]
        [Tooltip("When enabled, players will be allowed to chain-paint large areas." +
            "\nConsumes paints as normal - ignores paint sprayer." +
            "\n\nDoes NOT impact performance!")]
        [DefaultValue(true)]
        [ReloadRequired]
        public bool chainPainting;

        [Label("Allow Quick Whitelist Keys")]
        [Tooltip("When enabled, using the whitelist keybinds will" +
            "\nadd/remove hovered tiles/walls/items to/from their own whitelist" +
            "\n\nDoes NOT impact performance!")]
        [DefaultValue(true)]
        [ReloadRequired]
        public bool allowQuickWhitelisting;

        [Label("Teleport Loot to Player")]
        [Tooltip("When enabled, excavated drops will be warped to" +
            "\nthe player that commanded the excavation(s)." +
            "\n\nNOT ADVISED ON MULTIPLAYER SERVERS!!" +
            "\n\nDisabling this may improve performance... or hurt it.")]
        [DefaultValue(false)]
        [ReloadRequired]
        public bool teleportLoot;

        [Label("Make Loot Invulnerable")]
        [Tooltip("When enabled, item drops will be immune to hazards," +
            "\nthis includes things like lava." +
            "\n\nEnabling this may hurt performance," +
            "\nand may be considered cheating..." +
            "\n\nWORK IN PROGRESS!!")]
        [DefaultValue(false)]
        [ReloadRequired]
        public bool safeItems;

        [Label("Creative Mode")]
        [Tooltip("When enabled, items won't drop, items won't" +
            "\nbe consumed, and mining power will be ignored." +
            "\n\nEnabling this may improve performance," +
            "\nbut may be considered cheating..." +
            "\n\nWORK IN PROGRESS!!")]
        [DefaultValue(false)]
        [ReloadRequired]
        public bool creativeMode;

        [Label("Use Aggressive Mod Compatibility")]
        [Tooltip("When enabled, extra checks will be enforced in attempt" +
            "\nto properly bind modded tiles, tools, walls, and items" +
            "\n\nDisabling this may improve performance, at the cost of instabilities!" +
            "\n\nWORK IN PROGRESS!!")]
        [DefaultValue(true)]
        [ReloadRequired]
        public bool aggressiveCompatibility;


        [Header("World Settings - Blocks")]

        [Label("Allow Pickaxe Excavations")]
        [Tooltip("When enabled, the excavation algorithm will" +
            "\nbe allowed for blocks when using a sufficient pickaxe." +
            "\n\nDoes NOT impact performance!")]
        [DefaultValue(true)]
        [ReloadRequired]
        public bool allowPickaxing;

        [Label("Enable Tile Blacklist")]
        [Tooltip("When enabled, the world will enforce the Tile blacklist on its players" +
            "\nDisable this to give players free whitelist controls over Tiles" +
            "\n\nDoes NOT impact performance!")]
        [DefaultValue(true)]
        [ReloadRequired]
        public bool tileBlacklistToggled;

        [Label("Tile Blacklist")]
        [Tooltip("Configure this list to manually set what Tiles CANNOT be chain-excavated" +
            "\nPrefixed by the mod that owns them. Players' whitelists are overruled." +
            "\n\nDoes NOT impact performance!")]
        [DefaultListValue("Terraria:")]
        [ReloadRequired]
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
            "Terraria:" + TileID.Search.GetName(TileID.PinkDungeonBrick)
        };


        [Header("World Settings - Walls")]

        [Label("Allow Hammer Excavations")]
        [Tooltip("When enabled, the excavation algorithm will" +
            "\nbe allowed for walls when using a sufficient hammer." +
            "\n\nDoes NOT impact performance!")]
        [DefaultValue(true)]
        [ReloadRequired]
        public bool allowHammering;

        [Label("Enable Wall Blacklist")]
        [Tooltip("When enabled, the world will enforce the Wall blacklist on its players" +
            "\nDisable this to give players free whitelist controls over Walls" +
            "\n\nDoes NOT impact performance!")]
        [DefaultValue(true)]
        [ReloadRequired]
        public bool WallBlacklistToggled;

        [Label("Wall Blacklist")]
        [Tooltip("Configure this list to manually set what Walls CANNOT be chain-excavated" +
            "\nPrefixed by the mod that owns them. Players' whitelists are overruled." +
            "\n\nDoes NOT impact performance!")]
        [DefaultListValue("Terraria:")]
        [ReloadRequired]
        public HashSet<string> wallBlacklist = new() {
            //"Terraria:" + WallID.Search.GetName(WallID.Stone),
        };


        [Header("World Settings - Blockswap")]

        [Label("Allow Blockswap Excavations")]
        [Tooltip("When enabled, the excavation algorithm will" +
            "\nbe allowed for blockswaps when replacing a tile/wall." +
            "\n\nSlightly impacts performance!")]
        [DefaultValue(true)]
        [ReloadRequired]
        public bool allowReplace;

        [Label("Enable Item Blacklist")]
        [Tooltip("When enabled, the server will enforce the Item blacklist on its players" +
            "\nDisable this to give players free whitelist controls over Items" +
            "\n\nDoes NOT impact performance!")]
        [DefaultValue(true)]
        [ReloadRequired]
        public bool itemBlacklistToggled;

        [Label("Item Blacklist")]
        [Tooltip("If you don't know what this is, you probably shouldn't touch it..." +
            "\nThis controls what items are forbidden by players for whitelisting" +
            "\n\nDoes NOT impact performance!")]
        [DefaultListValue("Terraria:")]
        [ReloadRequired]
        public HashSet<string> itemBlacklist = new() {
            /*
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
            */
        };
    }

    [Label("Player Config")]
    public class OreExcavatorConfig_Client : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;


        [Header("Player Settings - UI")]

        [Label("Show Startup Message")]
        [Tooltip("When disabled, welcome messages will" +
            "\nbe hidden for this version of the mod." +
            "\n\nNew versions will re-enable this feature.")]
        [DefaultValue(true)]
        public bool showWelcome070;

        [Label("Show Excavation Tooltip")]
        [Tooltip("When disabled, holding the excavation " +
            "\nkey will no longer provide a contextual tooltip." +
            "\n\nPlease enable this before reporting bugs!")]
        [DefaultValue(true)]
        public bool showCursorTooltips;

        [Label("Show Item Tooltips")]
        [Tooltip("When disabled, items, walls, and tiles the" +
            "\n will no longer provide a contextual tooltip." +
            "\n\nPlease enable this before reporting bugs!")]
        [DefaultValue(true)]
        public bool showItemTooltips;

        [Label("Show Debug Logs")]
        [Tooltip("When enabled, debug logs" +
            "\nwill be hidden from the files." +
            "\n\nPlease enable this before reporting bugs!")]
        [DefaultValue(false)]
        public bool doDebugStuff;

        [Header("Player Settings - Core")]

        [Label("Block Modification Limit")]
        [Tooltip("Determines the maximum number of tiles" +
            "\nalterable per excavation" +
            "\n\nLarger numbers WILL negatively affect performance!")]
        [Range(0, 10000000)]
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
            "\nattempting an excavation, when bound to a right mouse." +
            "\n\nEnabling this may hurt performance, but" +
            "\nmight produce more stable behaviour!")]
        [DefaultValue(false)]
        public bool inititalChecks;


        [Header("Player Settings - Blocks")]

        [Label("Enable Tile Whitelist")]
        [Tooltip("When enabled, all tiles will be whitelisted by default." +
            "\n\nSlightly improves performance!")]
        [DefaultValue(true)]
        public bool tileWhitelistToggled;

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
            "Terraria:" + TileID.Search.GetName(TileID.LunarOre),
            "Terraria:" + TileID.Search.GetName(TileID.CrackedBlueDungeonBrick),
            "Terraria:" + TileID.Search.GetName(TileID.CrackedGreenDungeonBrick),
            "Terraria:" + TileID.Search.GetName(TileID.CrackedPinkDungeonBrick),
            "Terraria:" + TileID.Search.GetName(TileID.Spikes)
        };


        [Header("Player Settings - Walls")]

        [Label("Enable Wall Whitelist")]
        [Tooltip("When enabled, all walls will be whitelisted by default." +
            "\n\nSlightly improves performance!")]
        [DefaultValue(true)]
        public bool wallWhitelistToggled;

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


        [Header("Player Settings - Blockswap")]

        [Label("Enable Item Whitelist")]
        [Tooltip("When enabled, all items will be whitelisted by default." +
            "\n\nSlightly improves performance!")]
        [DefaultValue(true)]
        public bool itemWhitelistToggled;

        [Label("Item Whitelist")]
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

        [Label("Keybind Toggles Excavations")]
        [Tooltip("When enabled, tapping the keybind will toggle the" +
            "\nactive state of initiating excavations." +
            "\n\nDoes NOT impact performance!")]
        [DefaultValue(false)]
        public bool toggleExcavations;

        [Label("Cancel Excavations on Keybind Release")]
        [Tooltip("When enabled, letting go of the Excavation" +
            "\ncontrol key will cease all excavation operations." +
            "\n\nNOT ADVISED ON MULTIPLAYER SERVERS!!" +
            "\n\nDoes NOT impact performance!")]
        [DefaultValue(false)]
        public bool releaseCancelsExcavations;

        [Label("Enable Alternative Features")]
        [Tooltip("When enabled, the client will allow for special non-veinmine actions" +
            "\nDisable this if you don't plan on using these features, or are binding excavations to Mouse1" +
            "\n\nModerately impacts performance!")]
        [DefaultValue(true)]
        public bool doSpecials;

        [Label("Looking for your keybind?")]
        [Tooltip("Set your keybind in the vanilla controls area," +
            "\nThis is just for display purposes and to direct confused users.")]
        [DefaultValue("Unknown")]
        [JsonIgnore]
        public string keybind => (OreExcavator.ExcavateHotkey != null ? (OreExcavator.ExcavateHotkey.GetAssignedKeys().Count > 0 ? OreExcavator.ExcavateHotkey.GetAssignedKeys()[0] : "Not Set") : "Unknown");
    }
}