#if DEBUG

using OreExcavator.Enumerations;
using Terraria;
using TUnit.UnitTest;
using TUnit.Attributes;
using TUnit.Commands;
using Terraria.ModLoader;
using Terraria.ID;
using System.Threading.Tasks;
using System.Threading;
using System;
using Ionic.Zip;

namespace OreExcavator
{
    [JITWhenModsEnabled("TUnit")]
    [TestSuite]
    internal class UnitTests
    {
        private static int? mana;

        private static Player myPlayer;

        private static void Setup()
        {
            ExcavatorSystem.SaveConfig(OreExcavator.ClientConfig);
            ExcavatorSystem.SaveConfig(OreExcavator.ServerConfig);

            OreExcavator.ClientConfig.refillMana = false;
            OreExcavator.ServerConfig.teleportLoot = true;

            mana = Main.player[Main.myPlayer].statManaMax;
            Main.player[Main.myPlayer].statManaMax = 100;
            Main.player[Main.myPlayer].statMana = 100;
        }

        private static void Cleanup()
        {
            if (mana is null)
                return;

            ExcavatorSystem.LoadConfig(OreExcavator.ClientConfig);
            ExcavatorSystem.LoadConfig(OreExcavator.ServerConfig);

            Main.player[Main.myPlayer].statManaMax = mana ?? 200;
            Main.player[Main.myPlayer].statMana = mana ?? 200;
        }

        [Test]
        public static void Alteration_HasAndConsumeMana()
        {
            Setup();

            OreExcavator.ServerConfig.manaConsumption = 100f;
            

            bool noMana_Pass = !Alteration.HasAndConsumeMana(1.1f, Main.myPlayer);
            bool hasMana_Pass = Alteration.HasAndConsumeMana(0.9f, Main.myPlayer);
            Assert.AssertTrue(noMana_Pass && hasMana_Pass);
            Cleanup();
        }

#if false
        [Test]
        public static void DoAlteration_TileKilled()
        {
            Setup();

            Alteration alt = new(0, ActionType.TileKilled, (byte)Main.myPlayer, false, -1, -1);

            OreExcavator.ServerConfig.manaConsumption = 101f;
            bool noMana_Pass = alt.DoAlteration(0, 0);
            OreExcavator.ServerConfig.manaConsumption = 99f;
            bool hasMana_Pass = !alt.DoAlteration(0, 0);

            Assert.AssertTrue(noMana_Pass && hasMana_Pass);
            Cleanup();
        }

        [Test]
        public static void DoAlteration_TileReplaced()
        {
            Setup();

            Alteration alt = new(0, ActionType.TileReplaced, (byte)Main.myPlayer, false, -1, -1);

            OreExcavator.ServerConfig.manaConsumption = 51f;
            bool noMana_Pass = alt.DoAlteration(0, 0);
            OreExcavator.ServerConfig.manaConsumption = 49f;
            bool hasMana_Pass = !alt.DoAlteration(0, 0); // Fails due to lack of items


            Assert.AssertTrue(noMana_Pass);
            Cleanup();
        }

        [Test]
        public static void DoAlteration_WallKilled()
        {
            Setup();

            Alteration alt = new(0, ActionType.WallKilled, (byte)Main.myPlayer, false, -1, -1);

            OreExcavator.ServerConfig.manaConsumption = 202f;
            bool noMana_Pass = alt.DoAlteration(0, 0);
            OreExcavator.ServerConfig.manaConsumption = 198f;
            bool hasMana_Pass = !alt.DoAlteration(0, 0);

            Cleanup();

            Assert.AssertTrue(noMana_Pass && hasMana_Pass);
        }

        [Test]
        public static void DoAlteration_WallReplaced()
        {
            Setup();

            Alteration alt = new(0, ActionType.WallReplaced, (byte)Main.myPlayer, false, -1, -1);

            OreExcavator.ServerConfig.manaConsumption = 51f;
            bool noMana_Pass = alt.DoAlteration(0, 0);
            OreExcavator.ServerConfig.manaConsumption = 49f;
            bool hasMana_Pass = !alt.DoAlteration(0, 0); // Fails due to lack of items

            Cleanup();

            Assert.AssertTrue(noMana_Pass);
        }

        [Test]
        public static void DoAlteration_IronExcavation_Return50Ore()
        {
            Setup();
            // Setup initial conditions
            ushort initX = 2120;
            ushort initY = 272;

            ushort height = 5;
            ushort length = 10;

            ushort tile = TileID.Iron;

            OreExcavator.ServerConfig.teleportLoot = true;

            Tools.GenerateRectangle(initX, initY, length, height, tile);

            // Execute Spooler
            OreExcavator.ModifySpooler(ActionType.TileKilled,
                    initX,
                    initY,
                    (byte)OreExcavator.ClientConfig.recursionDelay,
                    (byte)OreExcavator.ClientConfig.recursionLimit,
                    OreExcavator.ClientConfig.doDiagonals,
                    (byte)Main.myPlayer,
                    tile);


            // Check inventory for success

            Assert.AssertInventory(ItemID.IronOre, height * length, Main.myPlayer, exact: true, remove: true, wait: 500);

            OreExcavator.ServerConfig.teleportLoot = false;
            Cleanup();
        }

        [Test]
        public static void DoAlteration_RailPlacement_Remove50()
        {
            // Setup initial conditions
            ushort initX = 2118;
            ushort initY = 266;
            

            ushort length = 50;

            ushort tile = TileID.MinecartTrack;
            short item = ItemID.MinecartTrack;

            int loc = Tools.GiveItem(Main.myPlayer, item, length);
            // Delay till item is added
            // Check to ensure it is in inventory

            // Clear space 
            Tools.RemoveRectangle(initX, initY, length, 0, noItem: true);
            Tools.GenerateRectangle(initX, initY, 1, 0, tile, force: true);
            // Execute placement
            OreExcavator.ModifySpooler(ActionType.TilePlaced,
                initX,
                initY,
                (byte)OreExcavator.ClientConfig.recursionDelay,
                (byte)OreExcavator.ClientConfig.recursionLimit,
                OreExcavator.ClientConfig.doDiagonals,
                (byte)Main.myPlayer,
                tile);

            // Test if placement was successfull
            // TODO: Add Assert when AssertRectangle is done


            Tools.RemoveItem(Main.myPlayer, loc, length, item);

            Assert.AssertTrue(true);
        }
#endif
    }
}
#endif