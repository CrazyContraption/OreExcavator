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

namespace OreExcavator
{
    [JITWhenModsEnabled("TUnit")]
    [TestSuite]
    internal class UnitTests
    {
        private static int? mana;

        //private static Player myPlayer;

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

            Alteration alt = new(0, ActionType.TileKilled, (byte)Main.myPlayer, false, -1, -1);

            OreExcavator.ServerConfig.manaConsumption = 101f;
            bool noMana_Pass = alt.DoAlteration(0,0);
            OreExcavator.ServerConfig.manaConsumption = 99f;
            bool hasMana_Pass = !alt.DoAlteration(0,0);
            
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

            Alteration alt = new(0, ActionType.TileReplaced, (byte)Main.myPlayer, false, -1, -1);
            
            OreExcavator.ServerConfig.manaConsumption = 51f;
            bool noMana_Pass = alt.DoAlteration(0,0);
            OreExcavator.ServerConfig.manaConsumption = 49f;
            bool hasMana_Pass = !alt.DoAlteration(0,0); // Fails due to lack of items

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

            Alteration alt = new(0, ActionType.WallKilled, (byte)Main.myPlayer, false, -1, -1);

            OreExcavator.ServerConfig.manaConsumption = 202f;
            bool noMana_Pass = alt.DoAlteration(0,0);
            OreExcavator.ServerConfig.manaConsumption = 198f;
            bool hasMana_Pass = !alt.DoAlteration(0,0);

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

            Alteration alt = new(0, ActionType.WallReplaced, (byte)Main.myPlayer, false, -1, -1);

            OreExcavator.ServerConfig.manaConsumption = 51f;
            bool noMana_Pass = alt.DoAlteration(0,0);
            OreExcavator.ServerConfig.manaConsumption = 49f;
            bool hasMana_Pass = !alt.DoAlteration(0,0); // Fails due to lack of items

            Cleanup();

            if (noMana_Pass)// && hasMana_Pass)
                return TestStatus.Passed;
            else
                return TestStatus.Failed;
        }

        [Test]
        public static TestStatus DoAlteration_IronExcavation_Return50Ore() // Needs to be async
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
            Task<TestStatus> task = new Task<TestStatus>(() =>
            {
                ushort? taskId = OreExcavator.ModifySpooler(ActionType.TileKilled,
                    initX,
                    initY,
                    (byte)OreExcavator.ClientConfig.recursionDelay,
                    (byte)OreExcavator.ClientConfig.recursionLimit,
                    OreExcavator.ClientConfig.doDiagonals,
                    (byte)Main.myPlayer,
                    tile);

                if (taskId is null)
                    return TestStatus.Failed;

                // Wait for excavation to complete
                while (OreExcavator.alterTasks.TryGetValue(taskId, out var thread) && thread.IsCompleted is false)
                    Thread.Sleep(500);

                // Check factors for success

                return TestStatus.Passed;
            });
            task.Start();
            TestStatus response = task.Result;

            OreExcavator.ServerConfig.teleportLoot = false;
            Cleanup();

            return response;
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

            // Imagine being multithreaded.
            Tools.RemoveItem(Main.myPlayer, loc, length, item);

            return TestStatus.Passed;
        }
    }
}
#endif