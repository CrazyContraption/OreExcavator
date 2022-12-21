using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OreExcavator.Enumerations;
using Terraria;
using TUnit.UnitTest;
using Terraria.ModLoader;


namespace OreExcavator
{
    [JITWhenModsEnabled("TUnit")]
    internal class UnitTests
    {
        private static OreExcavatorConfig_Client cfgClient;
        private static OreExcavatorConfig_Server cfgServ;

        internal static void CompileTests()
        {
            TestList.AddTest(new Test(DoAlteration_TileKilledHasMana_ReturnFalse, "|Alteration| Tile Killed With Mana"));
            TestList.AddTest(new Test(DoAlteration_TileKilledNoMana_ReturnTrue, "|Alteration| Tile Killed Without Mana"));
            TestList.AddTest(new Test(DoAlteration_WallKilledHasMana_ReturnFalse, "|Alteration| Wall Killed With Mana"));
            TestList.AddTest(new Test(DoAlteration_WallKilledHasNoMana_ReturnTrue, "|Alteration| Wall Killed Without Mana"));
            TestList.AddTest(new Test(DoAlteration_WallReplacedHasMana_ReturnFalse, "|Alteration| Wall Replaced With Mana"));
            TestList.AddTest(new Test(DoAlteration_WallReplacedHasNoMana_ReturnTrue, "|Alteration| Wall Replaced Without Mana"));
        }
        private static void Setup()
        {
            cfgClient = OreExcavator.ClientConfig;
            cfgServ = OreExcavator.ServerConfig;
        }

        private static void Cleanup()
        {
            OreExcavator.ClientConfig = cfgClient;
            OreExcavator.ServerConfig = cfgServ;
        }

        private static TestStatus DoAlteration_TileKilledHasMana_ReturnFalse()
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

        private static TestStatus DoAlteration_TileKilledNoMana_ReturnTrue()
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

        private static TestStatus DoAlteration_WallKilledHasMana_ReturnFalse()
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

        private static TestStatus DoAlteration_WallKilledHasNoMana_ReturnTrue()
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

        private static TestStatus DoAlteration_WallReplacedHasMana_ReturnFalse()
        {
            Setup();
            // Create Alteration
            // Thread 0, WallReplaced, null coords, Player 1, no puppet, no consumption, no subtype.
            Alteration alt = new(0, Enumerations.ActionType.WallReplaced, 0, 0, (byte)Main.myPlayer, false, -1, -1);

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

        private static TestStatus DoAlteration_WallReplacedHasNoMana_ReturnTrue()
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
    }
}