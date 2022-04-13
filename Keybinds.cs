using OreExcavator.Enumerations;

using Terraria;
using Terraria.ID;
using Terraria.GameInput;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace OreExcavator
{
    public class OreExcavatorKeybinds : ModPlayer
    {
        internal static bool excavatorHeld = false;
        internal static byte timeout = 0;

        /// <summary>
        /// Called manually, this updates configs and does the various checks to ensure a list update is valid.
        /// Also formats and saves the update accordingly if it is deemed valid.
        /// </summary>
        /// 
        /// <param name="actionType">What was the action being performed, usually whilelisting/removing the various types</param>
        /// <param name="name">The game-name of the object that is being modified with the list</param>
        /// <param name="typeId">The game-ID of the object that is being modified with the list</param>
        internal static void setListUpdates(ActionType actionType, int typeId, string name)
        {
            Item item = Main.HoverItem;
            int count =
                OreExcavator.ClientConfig.tileWhitelist.Count +
                OreExcavator.ClientConfig.wallWhitelist.Count +
                OreExcavator.ClientConfig.itemWhitelist.Count;

            switch (actionType)
            {
                case ActionType.TileWhiteListed:
                    if (!OreExcavator.ServerConfig.tileBlacklistToggled || !OreExcavator.ServerConfig.tileBlacklist.Contains(name))
                        if (!OreExcavator.ClientConfig.tileWhitelist.Contains(name))
                        {
                            OreExcavator.ClientConfig.tileWhitelist.Add(name);
                            OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                            OreExcavator.Log($"Added 'TileID.{name} ({typeId})' to the Tile whitelist", Color.Green, LogType.Info);
                        }
                        else
                            OreExcavator.Log($"'TileID.{name} ({typeId})' is already whitelisted", Color.Yellow, LogType.Warn);
                    else if (Main.netMode == NetmodeID.SinglePlayer)
                    {
                        if (timeout <= 0)
                        {
                            timeout = byte.MaxValue;
                            OreExcavator.Log($"Rejected adding 'TileID.{name} ({typeId})' because it is blacklisted", Color.Red, LogType.Error);
                            OreExcavator.Log($"Since this is a singleplayer world, tap the key twice to override the blacklist", Color.Aqua, LogType.Info);
                        }
                        else
                        {
                            timeout = 0;
                            OreExcavator.ServerConfig.tileBlacklist.Remove(name);
                            OreExcavator.ClientConfig.tileWhitelist.Add(name);
                            OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                            OreExcavator.Log($"Added 'TileID.{name} ({typeId})' to the Tile whitelist, and ignoring the blacklist for this session", Color.Green, LogType.Info);
                        }
                    }
                    else
                    {
                        OreExcavator.Log($"Rejected adding 'TileID.{name} ({typeId})' because it is blacklisted by the host", Color.Red, LogType.Error);
                        OreExcavator.Log($"If you think this is an error, ask the host to remove this Tile from their server's blacklist configuration", Color.Aqua, LogType.Info);
                    }
                    break;

                case ActionType.WallWhiteListed:
                    if (!OreExcavator.ServerConfig.WallBlacklistToggled || !OreExcavator.ServerConfig.wallBlacklist.Contains(name))
                        if (!OreExcavator.ClientConfig.wallWhitelist.Contains(name))
                        {
                            OreExcavator.ClientConfig.wallWhitelist.Add(name);
                            OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                            OreExcavator.Log($"Added `WallID.{name} ({typeId})' to the Wall whitelist", Color.Green, LogType.Info);
                        }
                        else
                            OreExcavator.Log($"'WallID.{name} ({typeId})' is already whitelisted", Color.Yellow, LogType.Warn);
                    else if (Main.netMode == NetmodeID.SinglePlayer)
                    {
                        if (timeout <= 0)
                        {
                            timeout = byte.MaxValue;
                            OreExcavator.Log($"Rejected adding 'WallID.{name} ({typeId})' because it is blacklisted", Color.Red, LogType.Error);
                            OreExcavator.Log($"Since this is a singleplayer world, tap the key twice to override the blacklist", Color.Aqua, LogType.Info);
                        }
                        else
                        {
                            timeout = 0;
                            OreExcavator.ServerConfig.wallBlacklist.Remove(name);
                            OreExcavator.ClientConfig.wallWhitelist.Add(name);
                            OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                            OreExcavator.Log($"Added 'TileID.{name} ({typeId})' to the Tile whitelist, and ignoring the blacklist for this session", Color.Green, LogType.Info);
                        }
                    }
                    else
                    {
                        OreExcavator.Log($"Rejected adding 'WallID.{name} ({typeId})' because it is blacklisted by the host", Color.Red, LogType.Error);
                        OreExcavator.Log($"If you think this is an error, ask the host to remove this Wall from their server's blacklist configuration", Color.Aqua, LogType.Info);
                    }
                    break;

                case ActionType.ItemWhiteListed:
                    if (!OreExcavator.ServerConfig.itemBlacklistToggled || !OreExcavator.ServerConfig.itemBlacklist.Contains(name))
                    {
                        if ((item.Name ?? "") != "" && !OreExcavator.ClientConfig.itemWhitelist.Contains(name))
                            if (item.createTile < 0.0 && item.createWall < 1) // Tried adding non-placeable
                                OreExcavator.Log($"You sly dog, 'ItemID.{name} ({typeId})' doesn't place anything, why are you trying to whitelist it?", Color.Red, LogType.Warn);
                            else
                            {
                                OreExcavator.ClientConfig.itemWhitelist.Add(name);
                                OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                                OreExcavator.Log($"Added 'ItemID.{name} ({typeId})' to the Item vein-swap whitelist", Color.Green, LogType.Info);
                            }
                        else if ((item.Name ?? "") != "")
                            OreExcavator.Log($"'ItemID.{name} ({typeId})' is already whitelisted", Color.Yellow, LogType.Warn);
                        else if (typeId <= 0)
                            OreExcavator.Log($"You can't whitelist nothing!", Color.Red, LogType.Error);
                    }
                    else
                    {
                        OreExcavator.Log($"Rejected adding 'ItemID.{name} ({typeId})' because it is blacklisted by the host", Color.Red, LogType.Error);
                        OreExcavator.Log($"If you think this is an error, ask the host to remove this Item from their server's blacklist configuration", Color.Aqua, LogType.Info);
                    }
                    break;

                case ActionType.TileBlackListed:
                    if (OreExcavator.ClientConfig.tileWhitelist.Contains(name))
                    {
                        OreExcavator.ClientConfig.tileWhitelist.Remove(name);
                        OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                        OreExcavator.Log($"Removed 'TileID.{name} ({typeId})' from the Tile whitelist", Color.Orange, LogType.Info);
                    }
                    else
                        OreExcavator.Log($"'TileID.{name} ({typeId})' isn't whitelisted, cannot remove", Color.Yellow, LogType.Warn);
                    break;

                case ActionType.WallBlackListed:
                    if (OreExcavator.ClientConfig.wallWhitelist.Contains(name))
                    {

                        OreExcavator.ClientConfig.wallWhitelist.Remove(name);
                        OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                        OreExcavator.Log($"Removed 'WallID.{name} ({typeId})' from the Wall whitelist", Color.Orange, LogType.Info);
                    }
                    else
                        OreExcavator.Log($"'WallID.{name} ({typeId})' isn't whitelisted, cannot remove", Color.Yellow, LogType.Warn);
                    break;

                case ActionType.ItemBlackListed:
                    if ((item.Name ?? "") != "" && OreExcavator.ClientConfig.itemWhitelist.Contains(name))
                    {
                        OreExcavator.ClientConfig.itemWhitelist.Remove(name);
                        OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                        OreExcavator.Log($"Removed 'ItemID.{name} ({typeId})' from the Item vein-swap whitelist", Color.Orange, LogType.Info);
                    }
                    else if ((item.Name ?? "") != "")
                        OreExcavator.Log($"'ItemID.{name} ({typeId})' isn't whitelisted, cannot remove", Color.Yellow, LogType.Warn);
                    else if (typeId <= 0)
                        OreExcavator.Log($"You can't remove nothing from the whitelist!", Color.Red, LogType.Error);
                    break;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient && count !=
                OreExcavator.ClientConfig.tileWhitelist.Count +
                OreExcavator.ClientConfig.wallWhitelist.Count +
                OreExcavator.ClientConfig.itemWhitelist.Count)
            {
                ModPacket packet = OreExcavator.myMod.GetPacket();
                packet.Write((byte)actionType);
                packet.Write((ushort)typeId);
                packet.Send();
            }
        }

        /// <summary>
        /// Called each tick to process any keybinding triggers that should be handled in that tick.
        /// Used to update excavation status, whitelisting, etc.
        /// </summary>
        /// 
        /// <param name="triggersSet"></param>
        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            excavatorHeld = OreExcavator.ExcavateHotkey.Current;

            Item item = Player.HeldItem;

            if (Main.netMode == NetmodeID.SinglePlayer && timeout > 0)
                timeout--;

            // Excavation toggle logic
            if (OreExcavator.ExcavateHotkey.JustPressed && OreExcavator.ClientConfig.toggleExcavations)
            {
                OreExcavator.excavationToggled = !OreExcavator.excavationToggled;
                if (OreExcavator.excavationToggled)
                    OreExcavator.Log("Excavations activated", Color.Green, LogType.Info);
                else
                    OreExcavator.Log("Excavations disabled", Color.Orange, LogType.Info);
            }

            // Tooltips when excavating
            if (OreExcavator.ClientConfig.showTooltips)
                if (OreExcavator.ClientConfig.toggleExcavations ? OreExcavator.excavationToggled : excavatorHeld)
                    if ((item.Name ?? "") != "" && item.pick + item.axe + item.hammer != 0)
                        Player.cursorItemIconText = "Excavating";
                    else if ((item.Name ?? "") != "" && (item.createTile >= 0.0 || item.createWall > 0))
                        if (item.Name.ToLower().Contains("seed"))
                            Player.cursorItemIconText = "Planting";
                        else if (item.Name.ToLower().Contains("paint"))
                            Player.cursorItemIconText = "Painting";
                        else
                            Player.cursorItemIconText = "Replacing";

            item = Main.HoverItem;
            if (OreExcavator.ServerConfig.allowQuickWhitelisting)
            {
                int x = Player.tileTargetX;
                int y = Player.tileTargetY;

                if (OreExcavator.WhitelistHotkey.JustPressed && Main.tile[x, y].HasTile) // Tile Add
                    setListUpdates(ActionType.TileWhiteListed, Main.tile[x, y].TileType, OreExcavator.GetFullNameById(Main.tile[x, y].TileType, ActionType.TileWhiteListed));
                else if (OreExcavator.WhitelistHotkey.JustPressed) // Wall Add
                    if (Main.tile[x, y].WallType <= 0) // Hover Add
                        setListUpdates(ActionType.ItemWhiteListed, item.type, OreExcavator.GetFullNameById(item.type, ActionType.ItemWhiteListed));
                    else
                        setListUpdates(ActionType.WallWhiteListed, Main.tile[x, y].WallType, OreExcavator.GetFullNameById(Main.tile[x, y].WallType, ActionType.WallWhiteListed));

                if (OreExcavator.BlacklistHotkey.JustPressed && Main.tile[x, y].HasTile) // Tile Remove
                    setListUpdates(ActionType.TileBlackListed, Main.tile[x, y].TileType, OreExcavator.GetFullNameById(Main.tile[x, y].TileType, ActionType.TileBlackListed));
                else if (OreExcavator.BlacklistHotkey.JustPressed) // Wall Remove
                    if (Main.tile[x, y].WallType <= 0) // Hover Remove
                        setListUpdates(ActionType.ItemBlackListed, item.type, OreExcavator.GetFullNameById(item.type, ActionType.ItemBlackListed));
                    else
                        setListUpdates(ActionType.WallBlackListed, Main.tile[x, y].WallType, OreExcavator.GetFullNameById(Main.tile[x, y].WallType, ActionType.WallBlackListed));
            }
        }
    }
}