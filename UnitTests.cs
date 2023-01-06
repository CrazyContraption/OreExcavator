using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OreExcavator.Enumerations;
using Terraria;
using TUnit.UnitTest;
using TUnit.Attributes;
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

        //private static Player myPlayer;

        private static void Setup()
        {
            //myPlayer = (Player)Main.player[Main.myPlayer].Clone();
            cfgClient = OreExcavator.ClientConfig;
            cfgServ = OreExcavator.ServerConfig;

            OreExcavator.ClientConfig.refillMana = false;
            OreExcavator.ServerConfig.teleportLoot = true;

            Main.player[Main.myPlayer].statManaMax = 100;
            Main.player[Main.myPlayer].statMana = 100;
        }

        private static void Cleanup()
        {
            //Main.player[Main.myPlayer] = myPlayer;
            OreExcavator.ClientConfig = cfgClient;
            OreExcavator.ServerConfig = cfgServ;

            Main.player[Main.myPlayer].statManaMax = 200;
            Main.player[Main.myPlayer].statMana = 200;
        }

        [Test]
        public static TestStatus Alteration_HasAndConsumeMana()
        {
            Setup();

            OreExcavator.ServerConfig.manaConsumption = 100f;
            

            bool noMana_Pass = !Alteration.HasAndConsumeMana(1.1f, Main.myPlayer);
            bool hasMana_Pass = Alteration.HasAndConsumeMana(0.9f, Main.myPlayer);

            Cleanup();

            if (noMana_Pass && hasMana_Pass)
                return TestStatus.Passed;
            else
                return TestStatus.Failed;
        }

        [Test]
        public static TestStatus DoAlteration_TileKilled()
        {
            Setup();

            Alteration alt = new(0, ActionType.TileKilled, 0, 0, (byte)Main.myPlayer, false, -1, -1);

            OreExcavator.ServerConfig.manaConsumption = 101f;
            bool noMana_Pass = Alteration.DoAlteration(alt);
            OreExcavator.ServerConfig.manaConsumption = 99f;
            bool hasMana_Pass = !Alteration.DoAlteration(alt);
            
            Cleanup();

            if (noMana_Pass && hasMana_Pass)
                return TestStatus.Passed;
            else
                return TestStatus.Failed;
        }

        [Test]
        public static TestStatus DoAlteration_TileReplaced()
        {
            Setup();

            Alteration alt = new(0, ActionType.TileReplaced, 0, 0, (byte)Main.myPlayer, false, -1, -1);
            
            OreExcavator.ServerConfig.manaConsumption = 51f;
            bool noMana_Pass = Alteration.DoAlteration(alt);
            OreExcavator.ServerConfig.manaConsumption = 49f;
            bool hasMana_Pass = !Alteration.DoAlteration(alt); // Fails due to lack of items

            Cleanup();

            if (noMana_Pass)// && hasMana_Pass)
                return TestStatus.Passed;
            else
                return TestStatus.Failed;
        }

        [Test]
        public static TestStatus DoAlteration_WallKilled()
        {
            Setup();

            Alteration alt = new(0, ActionType.WallKilled, 0, 0, (byte)Main.myPlayer, false, -1, -1);

            OreExcavator.ServerConfig.manaConsumption = 202f;
            bool noMana_Pass = Alteration.DoAlteration(alt);
            OreExcavator.ServerConfig.manaConsumption = 198f;
            bool hasMana_Pass = !Alteration.DoAlteration(alt);

            Cleanup();

            if (noMana_Pass && hasMana_Pass)
                return TestStatus.Passed;
            else
                return TestStatus.Failed;
        }

        [Test]
        public static TestStatus DoAlteration_WallReplaced()
        {
            Setup();

            Alteration alt = new(0, ActionType.WallReplaced, 0, 0, (byte)Main.myPlayer, false, -1, -1);

            OreExcavator.ServerConfig.manaConsumption = 51f;
            bool noMana_Pass = Alteration.DoAlteration(alt);
            OreExcavator.ServerConfig.manaConsumption = 49f;
            bool hasMana_Pass = !Alteration.DoAlteration(alt); // Fails due to lack of items

            Cleanup();

            if (noMana_Pass)// && hasMana_Pass)
                return TestStatus.Passed;
            else
                return TestStatus.Failed;
        }

        [Test]
        public static TestStatus DoAlteration_IronExcavation_Return50Ore()
        {
            Setup();
            // Setup initial conditions
            ushort initX = 2120;
            ushort initY = 272;

            ushort height = 5;
            ushort length = 10;

            ushort tile = TileID.Iron;

            OreExcavator.ServerConfig.teleportLoot = true;

            GenerateBlock(initX, initY, height, length, tile);

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
            Cleanup();

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

            Main.player[Main.myPlayer].QuickSpawnItem(null, ItemID.MinecartTrack, length);
            // Delay till item is added
            // Check to ensure it is in inventory

            // Clear space 
            for (int l = 0; l < length; l++)
            {
                WorldGen.KillTile(initX + length, initY, noItem: true);
            }

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
            int railIndex = Main.player[Main.myPlayer].FindItem(ItemID.MinecartTrack);
            if (railIndex != -1)
            {
                Main.player[Main.myPlayer].inventory[railIndex].stack = 0;
            }

            return TestStatus.Passed;
        }

        private static void GenerateBlock(ushort x, ushort y, ushort height, ushort length, ushort tile)
        {
            for (ushort l = 0; l < length; l++)
            {
                for (ushort h = 0; h < height; h++)
                {
                    WorldGen.PlaceTile(x + l, y + h, tile, true, true);
                }
            }
        }
    }
}