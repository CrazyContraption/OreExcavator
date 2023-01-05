using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OreExcavator.Enumerations;
using Terraria;
using TUnit.UnitTest;
using TUnit.Attributes;
using TUnit.Commands;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;

namespace OreExcavator
{
    [JITWhenModsEnabled("TUnit")]
    [TestSuite]
    internal class UnitTests
    {
        private static OreExcavatorConfig_Client cfgClient;
        private static OreExcavatorConfig_Server cfgServ;

        private static void Setup()
        {
            cfgClient = OreExcavator.ClientConfig;
            cfgServ = OreExcavator.ServerConfig;
        }

        private static void Cleanup()
        {
            OreExcavator.ClientConfig = cfgClient;
            OreExcavator.ServerConfig = cfgServ;
            OreExcavator.ServerConfig.manaConsumption = 0f;
        }

        [Test]
        public static TestStatus DoAlteration_TileKilledHasMana_ReturnFalse()
        {
            Setup();
            // Create Alteration
            // Thread 0, TileKilled, null coords, Player 1, no puppet, no consumption, no subtype.
            Alteration alt = new(0, Enumerations.ActionType.TileKilled, 0, 0, (byte)Main.myPlayer, false, -1, -1);

            // Change configs to match expectation
            OreExcavator.ServerConfig.manaConsumption = 100f;
            OreExcavator.ClientConfig.refillMana = true;

            // Alter player mana to be above cost.
            Main.player[Main.myPlayer].statManaMax = 101;
            Main.player[Main.myPlayer].statMana = 101;

            // Test Alteration
            bool ret = Alteration.DoAlteration(alt);


            Cleanup();
            // Output results, expecting false (good return)
            if (!ret)
            {
                return TestStatus.Passed;
            }
            else
            {
                return TestStatus.Failed;
            }
        }

        [Test]
        public static TestStatus DoAlteration_TileKilledNoMana_ReturnTrue()
        {
            Setup();
            // Create Alteration
            // Thread 0, TileKilled, null coords, Player 1, no puppet, no consumption, no subtype.
            Alteration alt = new(0, Enumerations.ActionType.TileKilled, 0, 0, (byte)Main.myPlayer, false, -1, -1);

            // Change configs to match expectation
            OreExcavator.ServerConfig.manaConsumption = 100f;
            OreExcavator.ClientConfig.refillMana = false;

            // Alter player mana to be under cost.
            Main.player[Main.myPlayer].statManaMax = 99;
            Main.player[Main.myPlayer].statMana = 99;

            // Test Alteration
            bool ret = Alteration.DoAlteration(alt);

            Cleanup();
            if (ret)
            {
                return TestStatus.Passed;
            }
            else
            {
                return TestStatus.Failed;
            }
        }

        [Test]
        public static TestStatus DoAlteration_WallKilledHasMana_ReturnFalse()
        {
            Setup();
            // Create Alteration
            // Thread 0, WallKilled, null coords, Player 1, no puppet, no consumption, no subtype.
            Alteration alt = new(0, Enumerations.ActionType.WallKilled, 0, 0, (byte)Main.myPlayer, false, -1, -1);

            // Change configs to match expectation
            OreExcavator.ServerConfig.manaConsumption = 100f;
            OreExcavator.ClientConfig.refillMana = false;

            // Alter player mana to be above cost.
            Main.player[Main.myPlayer].statManaMax = 101;
            Main.player[Main.myPlayer].statMana = 101;

            bool ret = Alteration.DoAlteration(alt);

            Cleanup();
            if (!ret)
            {
                return TestStatus.Passed;
            }
            else
            {
                return TestStatus.Failed;
            }
        }

        [Test]
        public static TestStatus DoAlteration_WallKilledHasNoMana_ReturnTrue()
        {
            Setup();
            // Create Alteration
            // Thread 0, WallKilled, null coords, Player 1, no puppet, no consumption, no subtype.
            Alteration alt = new(0, Enumerations.ActionType.WallKilled, 0, 0, (byte)Main.myPlayer, false, -1, -1);

            // Change configs to match expectation
            OreExcavator.ServerConfig.manaConsumption = 100f;
            OreExcavator.ClientConfig.refillMana = false;

            // Alter player mana to be under cost.
            Main.player[Main.myPlayer].statManaMax = 99;
            Main.player[Main.myPlayer].statMana = 99;

            bool ret = Alteration.DoAlteration(alt);

            if (ret)
            {
                return TestStatus.Passed;
            }
            else
            {
                return TestStatus.Failed;
            }
        }

        [Test]
        public static TestStatus DoAlteration_WallReplacedHasMana_ReturnFalse()
        {
            Setup();
            // Create Alteration
            // Thread 0, WallReplaced, null coords, Player 1, no puppet, no consumption, no subtype.
            Alteration alt = new(0, Enumerations.ActionType.WallReplaced, 0, 0, (byte)Main.myPlayer, false, -1, -1);

            // Change configs to match expectation
            OreExcavator.ServerConfig.manaConsumption = 100f;
            OreExcavator.ClientConfig.refillMana = false;

            // Alter player mana to be above cost.
            Main.player[Main.myPlayer].statManaMax = 201;
            Main.player[Main.myPlayer].statMana = 201;

            bool ret = Alteration.DoAlteration(alt);

            Cleanup();
            if (!ret)
            {
                return TestStatus.Passed;
            }
            else
            {
                return TestStatus.Failed;
            }
        }

        [Test]
        public static TestStatus DoAlteration_WallReplacedHasNoMana_ReturnTrue()
        {
            Setup();
            // Create Alteration
            // Thread 0, WallReplaced, null coords, Player 1, no puppet, no consumption, no subtype.
            Alteration alt = new(0, Enumerations.ActionType.WallReplaced, 0, 0, (byte)Main.myPlayer, false, -1, -1);

            // Change configs to match expectation
            OreExcavator.ServerConfig.manaConsumption = 100f;
            OreExcavator.ClientConfig.refillMana = false;

            // Alter player mana to be above cost.
            Main.player[Main.myPlayer].statManaMax = 99;
            Main.player[Main.myPlayer].statMana = 99;

            bool ret = Alteration.DoAlteration(alt);

            Cleanup();
            if (ret)
            {
                return TestStatus.Passed;
            }
            else
            {
                return TestStatus.Failed;
            }
        }

        [Test]
        public static TestStatus DoAlteration_IronExcavation_Return50Ore()
        {
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

            // Test if successful after spooler action is complete

            // Insert testing noises here

            OreExcavator.ServerConfig.teleportLoot = false;

            for (ushort l = 0; l < length; l++)
            {
                for (ushort h = 0; h < height; h++)
                {
                    if (!Main.tile[initX + l, initY + h].HasTile)
                    {
                        return TestStatus.Failed;
                    }
                }
            }
            return TestStatus.Passed;
        }

        [Test]
        public static TestStatus DoAlteration_RailPlacement_Remove50()
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
            Tools.RemoveRectangle(initX, initY, length, 0, itemdrop: true);
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

            // Imagine being multithreaded.
            Tools.RemoveItem(Main.myPlayer, loc, length, item);

            return TestStatus.Passed;
        }

        [Test]
        public static TestStatus DoAlteration_PlatformPlace_Remove50()
        {
            // Setup initial conditions
            ushort initX = 2118;
            ushort initY = 266;

            ushort length = 50;

            ushort tile = TileID.Platforms;

            short item = ItemID.WoodPlatform;

            int loc = Tools.GiveItem(Main.myPlayer, item, length);
            // Delay till item is added
            // Check to ensure it is in inventory

            // Clear space 
            Tools.RemoveRectangle(initX, initY, length, 0, itemdrop: true);

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

            // Imagine being multithreaded.
            Tools.RemoveItem(Main.myPlayer, loc, length, item);

            return TestStatus.Passed;
        }
    }
}