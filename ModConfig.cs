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
    [Label("$Mods.OreExcavator.Config.Server.Headers.Header")]
    public class OreExcavatorConfig_Server : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Header("$Mods.OreExcavator.Config.Server.Headers.Properties")]

        [Label("$Mods.OreExcavator.Config.Server.ShowWelcome.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Server.ShowWelcome.Description")]
        [DefaultValue(true)]
        public bool showWelcome;

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


        [Header("$Mods.OreExcavator.Config.Server.Headers.Tiles")]

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


        [Header("$Mods.OreExcavator.Config.Server.Headers.Walls")]

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


        [Header("$Mods.OreExcavator.Config.Server.Headers.Items")]

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
                message = Language.GetTextValue("Mods.OreExcavator.Config.Common.Changes.Remote");
                return true;
            }

            if (OreExcavator.hostOnly && Main.CurrentFrameFlags.ActivePlayersCount == 1 && whoAmI == 0)
            {
                message = Language.GetTextValue("Mods.OreExcavator.Config.Common.Changes.HostOnly");
                return true;
            }

            OreExcavator.hostOnly = false;
            message = Language.GetTextValue("Mods.OreExcavator.Config.Common.Changes.NoHost");
            return false;
        }
    }

    [Label("Player Config")]
    public class OreExcavatorConfig_Client : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;


        [Header("$Mods.OreExcavator.Config.Client.Headers.UI")]

        [Label("$Mods.OreExcavator.Config.Client.ShowWelcome.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Client.ShowWelcome.Description")]
        [DefaultValue(true)]
        public bool showWelcome070;

        [Label("$Mods.OreExcavator.Config.Client.ShowCursorTooltips.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Client.ShowCursorTooltips.Description")]
        [DefaultValue(true)]
        public bool showCursorTooltips;

        [Label("$Mods.OreExcavator.Config.Client.ShowItemTooltips.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Client.ShowItemTooltips.Description")]
        [DefaultValue(true)]
        public bool showItemTooltips;

        [Label("$Mods.OreExcavator.Config.Client.ReducedEffects.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Client.ReducedEffects.Description")]
        [DefaultValue(false)]
        public bool reducedEffects;

        [Label("$Mods.OreExcavator.Config.Client.RefillMana.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Client.RefillMana.Description")]
        [DefaultValue(false)]
        public bool refillMana;

        [Label("$Mods.OreExcavator.Config.Client.DoDebugStuff.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Client.DoDebugStuff.Description")]
        [DefaultValue(false)]
        public bool doDebugStuff;

        [Header("$Mods.OreExcavator.Config.Client.Headers.Core")]

        [Label("$Mods.OreExcavator.Config.Client.RecursionLimit.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Client.RecursionLimit.Description")]
        [Range(int.MinValue, int.MaxValue)]
        [DefaultValue(600)]
        public int recursionLimit;

        [Label("$Mods.OreExcavator.Config.Client.DoDiagonals.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Client.DoDiagonals.Description")]
        [DefaultValue(false)]
        public bool doDiagonals;

        [Label("$Mods.OreExcavator.Config.Client.RecursionDelay.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Client.RecursionDelay.Description")]
        [Range(byte.MinValue, byte.MaxValue)]
        [DefaultValue(10)]
        public int recursionDelay;

        [Label("$Mods.OreExcavator.Config.Client.InititalChecks.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Client.InititalChecks.Description")]
        [DefaultValue(false)]
        public bool inititalChecks;


        [Header("$Mods.OreExcavator.Config.Client.Headers.Tiles")]

        [Label("$Mods.OreExcavator.Config.Client.TileWhitelistToggled.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Client.TileWhitelistToggled.Description")]
        [DefaultValue(true)]
        public bool tileWhitelistToggled;

        [Label("$Mods.OreExcavator.Config.Client.TileWhitelist.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Client.TileWhitelist.Description")]
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


        [Header("$Mods.OreExcavator.Config.Client.Headers.Walls")]

        [Label("$Mods.OreExcavator.Config.Client.WallWhitelistToggled.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Client.WallWhitelistToggled.Description")]
        [DefaultValue(true)]
        public bool wallWhitelistToggled;

        [Label("$Mods.OreExcavator.Config.Client.WallWhitelist.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Client.WallWhitelist.Description")]
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


        [Header("$Mods.OreExcavator.Config.Client.Headers.Items")]

        [Label("$Mods.OreExcavator.Config.Client.ChainPlacing.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Client.ChainPlacing.Description")]
        [DefaultValue(true)]
        public bool ChainPlacing;

        [Label("$Mods.OreExcavator.Config.Client.ItemWhitelistToggled.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Client.ItemWhitelistToggled.Description")]
        [DefaultValue(true)]
        public bool itemWhitelistToggled;

        [Label("$Mods.OreExcavator.Config.Client.ItemWhitelist.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Client.ItemWhitelist.Description")]
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


        [Header("$Mods.OreExcavator.Config.Client.Headers.Controls")]

        [Label("$Mods.OreExcavator.Config.Client.ToggleExcavations.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Client.ToggleExcavations.Description")]
        [DefaultValue(false)]
        public bool toggleExcavations;

        [Label("$Mods.OreExcavator.Config.Client.ReleaseCancelsExcavations.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Client.ReleaseCancelsExcavations.Description")]
        [DefaultValue(false)]
        public bool releaseCancelsExcavations;

        [Label("$Mods.OreExcavator.Config.Client.DoSpecials.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Client.DoSpecials.Description")]
        [DefaultValue(true)]
        public bool doSpecials;

        [Label("$Mods.OreExcavator.Config.Client.Keybind.Label")]
        [Tooltip("$Mods.OreExcavator.Config.Client.Keybind.Description")]
        [DefaultValue("Unknown")]
        [ReadOnly(true)]
        [JsonIgnore]
        public string keybind => (OreExcavator.ExcavateHotkey != null ? (OreExcavator.ExcavateHotkey.GetAssignedKeys().Count > 0 ? OreExcavator.ExcavateHotkey.GetAssignedKeys()[0] : "Not Set") : "Unknown");
    }
}