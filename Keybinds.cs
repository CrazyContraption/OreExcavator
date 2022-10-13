using OreExcavator.Enumerations;

using Terraria;
using Terraria.ID;
using Terraria.GameInput;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ObjectData;
using Terraria.Localization;

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
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Off", "Tile", $"ID.{name} ({typeId})"), Color.Red, LogType.Warn);

                    if (!OreExcavator.ServerConfig.tileBlacklistToggled || !OreExcavator.ServerConfig.tileBlacklist.Contains(name))
                    {
                        if (!OreExcavator.ClientConfig.tileWhitelist.Contains(name))
                        {
                            OreExcavator.ClientConfig.tileWhitelist.Add(name);
                            OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                            OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Added", "Tile", $"ID.{name} ({typeId})"), Color.Green, LogType.Info);
                        }
                        else
                            OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Contains", "Tile", $"ID.{name} ({typeId})"), Color.Yellow, LogType.Warn);
                    }
                    else if (Main.netMode == NetmodeID.SinglePlayer)
                    {
                        if (timeout <= 0)
                        {
                            timeout = byte.MaxValue;
                            OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Blacklisted", "Tile", $"ID.{name} ({typeId})"), Color.Red, LogType.Error);
                            OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Override", "Tile", $"ID.{name} ({typeId})"), Color.Aqua, LogType.Info);
                        }
                        else
                        {
                            timeout = 0;
                            OreExcavator.ServerConfig.tileBlacklist.Remove(name);
                            OreExcavator.ClientConfig.tileWhitelist.Add(name);
                            OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                            OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Added", "Tile", $"ID.{name} ({typeId})") + Language.GetTextValue("Mods.OreExcavator.Whitelisting.Ignore", "Wall", $"ID.{name} ({typeId})"), Color.Green, LogType.Info);
                        }
                    }
                    else
                    {
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Blacklisted", "Tile", $"ID.{name} ({typeId})"), Color.Red, LogType.Error);
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.ContactHost", "Tile", $"ID.{name} ({typeId})"), Color.Aqua, LogType.Info);
                    }
                    break;

                case ActionType.WallWhiteListed:
                    if (!OreExcavator.ClientConfig.wallWhitelistToggled)
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Off", "Wall", $"ID.{name} ({typeId})"), Color.Red, LogType.Warn);

                    if (!OreExcavator.ServerConfig.wallBlacklistToggled || !OreExcavator.ServerConfig.wallBlacklist.Contains(name))
                    {
                        if (!OreExcavator.ClientConfig.wallWhitelist.Contains(name))
                        {
                            OreExcavator.ClientConfig.wallWhitelist.Add(name);
                            OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                            OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Added", "Wall", $"ID.{name} ({typeId})"), Color.Green, LogType.Info);
                        }
                        else
                            OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Contains", "Wall", $"ID.{name} ({typeId})"), Color.Yellow, LogType.Warn);
                    }
                    else if (Main.netMode == NetmodeID.SinglePlayer)
                    {
                        if (timeout <= 0)
                        {
                            timeout = byte.MaxValue;
                            OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Blacklisted", "Wall", $"ID.{name} ({typeId})"), Color.Red, LogType.Error);
                            OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Override", "Wall", $"ID.{name} ({typeId})"), Color.Aqua, LogType.Info);
                        }
                        else
                        {
                            timeout = 0;
                            OreExcavator.ServerConfig.wallBlacklist.Remove(name);
                            OreExcavator.ClientConfig.wallWhitelist.Add(name);
                            OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                            OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Contains", "Wall", $"ID.{name} ({typeId})") + Language.GetTextValue("Mods.OreExcavator.Whitelisting.Ignore", "Wall", $"ID.{name} ({typeId})"), Color.Green, LogType.Info);
                        }
                    }
                    else
                    {
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Blacklist", "Wall", $"ID.{name} ({typeId})"), Color.Red, LogType.Error);
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.ContactHost", "Wall", $"ID.{name} ({typeId})"), Color.Aqua, LogType.Info);
                    }
                    break;

                case ActionType.ItemWhiteListed:
                    if ((item.Name ?? "") == "")
                        break;
                    if (item.createTile < 0.0 && item.createWall < 1) // Tried adding non-placeable
                    {
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.NoPlace", "Item", $"ID.{name} ({typeId})"), Color.Red, LogType.Warn);
                        break;
                    }

                    if (!OreExcavator.ClientConfig.itemWhitelistToggled)
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Off", "Item", $"ID.{name} ({typeId})"), Color.Red, LogType.Warn);

                    if (!OreExcavator.ServerConfig.itemBlacklistToggled || !OreExcavator.ServerConfig.itemBlacklist.Contains(name))
                    {
                        if (!OreExcavator.ClientConfig.itemWhitelist.Contains(name))
                        {
                            OreExcavator.ClientConfig.itemWhitelist.Add(name);
                            OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                            OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Added", "Item", $"ID.{name} ({typeId})"), Color.Green, LogType.Info);
                        }
                        else if (typeId > 0)
                            OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Contains", "Item", $"ID.{name} ({typeId})"), Color.Yellow, LogType.Warn);
                        else
                            OreExcavator.Log($"You can't whitelist nothing! Hover over a tile, wall or item and try again.", Color.Red, LogType.Error);
                    }
                    else if (Main.netMode == NetmodeID.SinglePlayer)
                    {
                        if (timeout <= 0)
                        {
                            timeout = byte.MaxValue;
                            OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Blacklisted", "Item", $"ID.{name} ({typeId})"), Color.Red, LogType.Error);
                            OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Override", "Item", $"ID.{name} ({typeId})"), Color.Aqua, LogType.Info);
                        }
                        else
                        {
                            if (!OreExcavator.ClientConfig.itemWhitelist.Contains(name))
                            {
                                OreExcavator.ServerConfig.itemBlacklist.Remove(name);
                                OreExcavator.ClientConfig.itemWhitelist.Add(name);
                                OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                                OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Added", "Item", $"ID.{name} ({typeId})") + Language.GetTextValue("Mods.OreExcavator.Whitelisting.Ignore", "Item", $"ID.{name} ({typeId})"), Color.Green, LogType.Info);
                            }
                            else if (typeId > 0)
                                OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Contains", "Item", $"ID.{name} ({typeId})"), Color.Yellow, LogType.Warn);
                            else
                                OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Nothing"), Color.Red, LogType.Error);
                        }
                    }
                    else
                    {
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Blacklisted", "Item", $"ID.{name} ({typeId})"), Color.Red, LogType.Error);
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.ContactHost", "Item", $"ID.{name} ({typeId})"), Color.Aqua, LogType.Info);
                    }
                    break;

                case ActionType.TileBlackListed:
                    if (!OreExcavator.ClientConfig.tileWhitelistToggled)
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Blacklisting.Off", "Tile", $"ID.{name} ({typeId})"), Color.Red, LogType.Warn);

                    if (OreExcavator.ClientConfig.tileWhitelist.Contains(name))
                    {
                        OreExcavator.ClientConfig.tileWhitelist.Remove(name);
                        OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Removed", "Tile", $"ID.{name} ({typeId})"), Color.Orange, LogType.Info);
                    }
                    else
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Missing", "Tile", $"ID.{name} ({typeId})"), Color.Yellow, LogType.Warn);
                    break;

                case ActionType.WallBlackListed:
                    if (!OreExcavator.ClientConfig.wallWhitelistToggled)
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.RemoveOff", "Wall", $"ID.{name} ({typeId})"), Color.Red, LogType.Warn);

                    if (OreExcavator.ClientConfig.wallWhitelist.Contains(name))
                    {

                        OreExcavator.ClientConfig.wallWhitelist.Remove(name);
                        OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Removed", "Wall", $"ID.{name} ({typeId})"), Color.Orange, LogType.Info);
                    }
                    else
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Missing", "Wall", $"ID.{name} ({typeId})"), Color.Yellow, LogType.Warn);
                    break;

                case ActionType.ItemBlackListed:
                    if (!OreExcavator.ClientConfig.itemWhitelistToggled)
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.RemoveOff", "Item", $"ID.{name} ({typeId})"), Color.Red, LogType.Warn);

                    if ((item.Name ?? "") != "" && OreExcavator.ClientConfig.itemWhitelist.Contains(name))
                    {
                        OreExcavator.ClientConfig.itemWhitelist.Remove(name);
                        OreExcavator.SaveConfig(OreExcavator.ClientConfig);
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Removed", "Item", $"ID.{name} ({typeId})"), Color.Orange, LogType.Info);
                    }
                    else if ((item.Name ?? "") != "")
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Missing", "Item", $"ID.{name} ({typeId})"), Color.Yellow, LogType.Warn);
                    else if (typeId <= 0)
                        OreExcavator.Log(Language.GetTextValue("Mods.OreExcavator.Whitelisting.Nothing", "Item", $"ID.{name} ({typeId})"), Color.Red, LogType.Error);
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