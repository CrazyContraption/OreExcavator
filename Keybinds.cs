using OreExcavator.Enumerations;

using Terraria;
using Terraria.ID;
using Terraria.GameInput;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ObjectData;

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
        internal static void setListUpdates(ActionType actionType, int typeId, string name, int style = -1)
        {
            Item item = Main.HoverItem;
            int count =
                OreExcavator.ClientConfig.tileWhitelist.Count +
                OreExcavator.ClientConfig.wallWhitelist.Count +
                OreExcavator.ClientConfig.itemWhitelist.Count;

            switch (actionType)
            {
                case ActionType.TileWhiteListed:
                    if (!OreExcavator.ClientConfig.tileWhitelistToggled)
                        OreExcavator.Log("Y'know your Tile whitelist is off right? We'll add it for you anyways though!", Color.Red, LogType.Warn);

                    if (!OreExcavator.ServerConfig.tileBlacklistToggled || !OreExcavator.ServerConfig.tileBlacklist.Contains(name))
                        if (!OreExcavator.ClientConfig.tileWhitelist.Contains(name))
                        {
                            OreExcavator.ClientConfig.tileWhitelist.Add(name);
                            OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                            OreExcavator.Log($"Added 'TileID.{name} ({typeId})' to your Tile whitelist", Color.Green, LogType.Info);
                        }
                        else
                            OreExcavator.Log($"'TileID.{name} ({typeId})' is already whitelisted", Color.Yellow, LogType.Warn);
                    else if (Main.netMode == NetmodeID.SinglePlayer)
                    {
                        if (timeout <= 0)
                        {
                            timeout = byte.MaxValue;
                            OreExcavator.Log($"Rejected adding 'TileID.{name} ({typeId})' because it is blacklisted by the server host", Color.Red, LogType.Error);
                            OreExcavator.Log($"Since this is a singleplayer world, you can tap the key twice to override your host blacklist", Color.Aqua, LogType.Info);
                        }
                        else
                        {
                            timeout = 0;
                            OreExcavator.ServerConfig.tileBlacklist.Remove(name);
                            OreExcavator.ClientConfig.tileWhitelist.Add(name);
                            OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                            OreExcavator.Log($"Added 'TileID.{name} ({typeId})' to your Tile whitelist, and ignoring its blacklist for this session", Color.Green, LogType.Info);
                        }
                    }
                    else
                    {
                        OreExcavator.Log($"Rejected adding 'TileID.{name} ({typeId})' because it is blacklisted by the server host", Color.Red, LogType.Error);
                        OreExcavator.Log($"If you think this is an error, ask the host to remove this Tile from their server's blacklist configuration", Color.Aqua, LogType.Info);
                    }
                    break;

                case ActionType.WallWhiteListed:
                    if (!OreExcavator.ClientConfig.wallWhitelistToggled)
                        OreExcavator.Log("Y'know your Wall whitelist is off right? We'll add it for you anyways though!", Color.Red, LogType.Warn);

                    if (!OreExcavator.ServerConfig.wallBlacklistToggled || !OreExcavator.ServerConfig.wallBlacklist.Contains(name))
                        if (!OreExcavator.ClientConfig.wallWhitelist.Contains(name))
                        {
                            OreExcavator.ClientConfig.wallWhitelist.Add(name);
                            OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                            OreExcavator.Log($"Added `WallID.{name} ({typeId})' to your Wall whitelist", Color.Green, LogType.Info);
                        }
                        else
                            OreExcavator.Log($"'WallID.{name} ({typeId})' is already whitelisted", Color.Yellow, LogType.Warn);
                    else if (Main.netMode == NetmodeID.SinglePlayer)
                    {
                        if (timeout <= 0)
                        {
                            timeout = byte.MaxValue;
                            OreExcavator.Log($"Rejected adding 'WallID.{name} ({typeId})' because it is blacklisted by the server host", Color.Red, LogType.Error);
                            OreExcavator.Log($"Since this is a singleplayer world, you can tap the key twice to override your host blacklist", Color.Aqua, LogType.Info);
                        }
                        else
                        {
                            timeout = 0;
                            OreExcavator.ServerConfig.wallBlacklist.Remove(name);
                            OreExcavator.ClientConfig.wallWhitelist.Add(name);
                            OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                            OreExcavator.Log($"Added 'TileID.{name} ({typeId})' to your Tile whitelist, and ignoring its blacklist for this session", Color.Green, LogType.Info);
                        }
                    }
                    else
                    {
                        OreExcavator.Log($"Rejected adding 'WallID.{name} ({typeId})' because it is blacklisted by the server host", Color.Red, LogType.Error);
                        OreExcavator.Log($"If you think this is an error, ask the host to remove this Wall from their server's blacklist configuration", Color.Aqua, LogType.Info);
                    }
                    break;

                case ActionType.ItemWhiteListed:
                    if ((item.Name ?? "") == "")
                        break;
                    if (item.createTile < 0.0 && item.createWall < 1) // Tried adding non-placeable
                    {
                        OreExcavator.Log($"You sly dog, 'ItemID.{name} ({typeId})' doesn't place anything, why are you trying to whitelist it?", Color.Red, LogType.Warn);
                        break;
                    }

                    if (!OreExcavator.ClientConfig.itemWhitelistToggled)
                        OreExcavator.Log("Y'know your Item whitelist is off right? We'll add it for you anyways though!", Color.Red, LogType.Warn);

                    if (!OreExcavator.ServerConfig.itemBlacklistToggled || !OreExcavator.ServerConfig.itemBlacklist.Contains(name))
                    {
                        if (!OreExcavator.ClientConfig.itemWhitelist.Contains(name))
                        {
                            OreExcavator.ClientConfig.itemWhitelist.Add(name);
                            OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                            OreExcavator.Log($"Added 'ItemID.{name} ({typeId})' to the Item vein-swap whitelist", Color.Green, LogType.Info);
                        }
                        else if (typeId > 0)
                            OreExcavator.Log($"'ItemID.{name} ({typeId})' is already whitelisted", Color.Yellow, LogType.Warn);
                        else
                            OreExcavator.Log($"You can't whitelist nothing! Hover over a tile, wall or item and try again.", Color.Red, LogType.Error);
                    }
                    else if (Main.netMode == NetmodeID.SinglePlayer)
                    {
                        if (timeout <= 0)
                        {
                            timeout = byte.MaxValue;
                            OreExcavator.Log($"Rejected adding 'ItemID.{name} ({typeId})' because it is blacklisted by the server host", Color.Red, LogType.Error);
                            OreExcavator.Log($"Since this is a singleplayer world, you can tap the key twice to override your host blacklist", Color.Aqua, LogType.Info);
                        }
                        else
                        {
                            if (!OreExcavator.ClientConfig.itemWhitelist.Contains(name))
                            {
                                OreExcavator.ServerConfig.itemBlacklist.Remove(name);
                                OreExcavator.ClientConfig.itemWhitelist.Add(name);
                                OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                                OreExcavator.Log($"Added 'ItemID.{name} ({typeId})' to the Item vein-swap whitelist, and ignoring its blacklist for this session", Color.Green, LogType.Info);
                            }
                            else if (typeId > 0)
                                OreExcavator.Log($"'ItemID.{name} ({typeId})' is already whitelisted", Color.Yellow, LogType.Warn);
                            else
                                OreExcavator.Log($"You can't whitelist nothing! Hover over a tile, wall or item and try again.", Color.Red, LogType.Error);
                        }
                    }
                    else
                    {
                        OreExcavator.Log($"Rejected adding 'ItemID.{name} ({typeId})' because it is blacklisted by the server host", Color.Red, LogType.Error);
                        OreExcavator.Log($"If you think this is an error, ask the host to remove this Item from their server's blacklist configuration", Color.Aqua, LogType.Info);
                    }
                    break;

                case ActionType.TileBlackListed:
                    if (!OreExcavator.ClientConfig.tileWhitelistToggled)
                        OreExcavator.Log("Y'know your Tile whitelist is off right? We'll remove it for you anyways though!", Color.Red, LogType.Warn);

                    if (OreExcavator.ClientConfig.tileWhitelist.Contains(name))
                    {
                        OreExcavator.ClientConfig.tileWhitelist.Remove(name);
                        OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                        OreExcavator.Log($"Removed 'TileID.{name} ({typeId})' from your Tile whitelist", Color.Orange, LogType.Info);
                    }
                    else
                        OreExcavator.Log($"'TileID.{name} ({typeId})' isn't whitelisted, cannot remove", Color.Yellow, LogType.Warn);
                    break;

                case ActionType.WallBlackListed:
                    if (!OreExcavator.ClientConfig.wallWhitelistToggled)
                        OreExcavator.Log("Y'know your Wall whitelist is off right? We'll remove it for you anyways though!", Color.Red, LogType.Warn);

                    if (OreExcavator.ClientConfig.wallWhitelist.Contains(name))
                    {

                        OreExcavator.ClientConfig.wallWhitelist.Remove(name);
                        OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                        OreExcavator.Log($"Removed 'WallID.{name} ({typeId})' from your Wall whitelist", Color.Orange, LogType.Info);
                    }
                    else
                        OreExcavator.Log($"'WallID.{name} ({typeId})' isn't whitelisted, cannot remove", Color.Yellow, LogType.Warn);
                    break;

                case ActionType.ItemBlackListed:
                    if (!OreExcavator.ClientConfig.itemWhitelistToggled)
                        OreExcavator.Log("Y'know your Item whitelist is off right? We'll remove it for you anyways though!", Color.Red, LogType.Warn);

                    if ((item.Name ?? "") != "" && OreExcavator.ClientConfig.itemWhitelist.Contains(name))
                    {
                        OreExcavator.ClientConfig.itemWhitelist.Remove(name);
                        OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                        OreExcavator.Log($"Removed 'ItemID.{name} ({typeId})' from your Item chain-swap whitelist", Color.Orange, LogType.Info);
                    }
                    else if ((item.Name ?? "") != "")
                        OreExcavator.Log($"'ItemID.{name} ({typeId})' isn't whitelisted, cannot remove", Color.Yellow, LogType.Warn);
                    else if (typeId <= 0)
                        OreExcavator.Log($"You can't remove nothing from the whitelist! Hover over a tile, wall or item and try again.", Color.Red, LogType.Error);
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
                packet.Write((int)style);
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

            int x = Player.tileTargetX;
            int y = Player.tileTargetY;

            // Tooltips when excavating
            if (OreExcavator.ClientConfig.showCursorTooltips)
                if (OreExcavator.ClientConfig.toggleExcavations ? OreExcavator.excavationToggled : excavatorHeld)
                    if ((item.Name ?? "") != "")
                        if (Main.tile[x, y].HasTile)
                        {
                            if (item.pick != 0)
                                Player.cursorItemIconText = "Excavating";
                            else if (item.createTile >= TileID.Dirt || item.createWall > WallID.None)
                            {
                                if (item.Name.ToLower().Contains("seed"))
                                    Player.cursorItemIconText = "Planting";
                                else
                                    Player.cursorItemIconText = "Replacing";
                            }
                            else if (item.Name.Contains("Paint") && item.paint == PaintID.None)
                                Player.cursorItemIconText = "Painting";
                        }
                        else
                        {
                            if (item.createTile >= TileID.Dirt)
                                switch (item.createTile)
                                {
                                    case TileID.Platforms:
                                    case TileID.MinecartTrack:
                                    case TileID.Rope:
                                    case TileID.VineRope:
                                    case TileID.WebRope:
                                    case TileID.PlanterBox:
                                        Player.cursorItemIconText = "Placing";
                                        break;
                                }
                            else if (item.createWall > WallID.None)
                                Player.cursorItemIconText = "Replacing";
                            else if (Main.tile[x, y].WallType > WallID.None)
                            {
                                if (item.hammer != 0)
                                    Player.cursorItemIconText = "Excavating";
                                else if (item.Name.Contains("Paint") && item.paint == PaintID.None)
                                    Player.cursorItemIconText = "Painting";
                            }
                        }

            item = Main.HoverItem;
            if (OreExcavator.ServerConfig.allowQuickWhitelisting)
                if (OreExcavator.WhitelistHotkey.JustPressed) {

                    Tile localTile = Main.tile[x, y];
                    int style = TileObjectData.GetTileStyle(localTile);
                    if (item.Name != "") // Item ADD
                        setListUpdates(ActionType.ItemWhiteListed, item.type, OreExcavator.GetFullNameById(item.type, ActionType.ItemWhiteListed));
                    else if (localTile.HasTile) // Tile ADD
                        setListUpdates(ActionType.TileWhiteListed, localTile.TileType, OreExcavator.GetFullNameById(localTile.TileType, ActionType.TileWhiteListed, style), style);
                    else if (localTile.WallType > 0) // Wall ADD
                        setListUpdates(ActionType.WallWhiteListed, localTile.WallType, OreExcavator.GetFullNameById(localTile.WallType, ActionType.WallWhiteListed));

                } else if (OreExcavator.BlacklistHotkey.JustPressed) {

                    Tile localTile = Main.tile[x, y];
                    int style = TileObjectData.GetTileStyle(localTile);
                    if (item.Name != "") // Item REMOVE
                        setListUpdates(ActionType.ItemBlackListed, item.type, OreExcavator.GetFullNameById(item.type, ActionType.ItemBlackListed));
                    else if (localTile.HasTile) // Tile REMOVE
                        setListUpdates(ActionType.TileBlackListed, localTile.TileType, OreExcavator.GetFullNameById(localTile.TileType, ActionType.TileBlackListed, style), style);
                    else if (localTile.WallType > 0) // Wall REMOVE
                        setListUpdates(ActionType.WallBlackListed, localTile.WallType, OreExcavator.GetFullNameById(localTile.WallType, ActionType.WallBlackListed));
                }
        }
    }
}