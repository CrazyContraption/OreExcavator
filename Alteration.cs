using Microsoft.Xna.Framework;
using OreExcavator.Enumerations;
using Terraria;
using Terraria.ID;

namespace OreExcavator
{
    internal class Alteration
    {

        public readonly int? threadID;
        private readonly ActionType actionType;
        private readonly int x;
        private readonly int y;
        private readonly byte playerID;
        private readonly bool puppeting;
        private readonly int consumesItemType = -1;
        private readonly int itemSubtype = -1;

        public Alteration(int? threadID, ActionType actionType, int x, int y, byte playerID, bool puppeting = false, int consumesItemType = -1, int itemSubtype = -1)
        {
            this.threadID = threadID; 
            this.actionType = actionType;
            this.x = x;
            this.y = y;
            this.playerID = playerID;
            this.puppeting = puppeting;
            this.consumesItemType = consumesItemType;
            this.itemSubtype = itemSubtype;
        }

        internal static bool HasAndConsumeMana(float manaCost, int player = -1)
        {
            if (manaCost <= 0)
                return true;
            if (player < 0)
                return false;
            return Main.player[player].CheckMana((int)(OreExcavator.ServerConfig.manaConsumption * manaCost), true, !OreExcavator.ClientConfig.refillMana);
        }

        /// <summary>
        /// Handles the individual manipulations based on the action type desired when called.
        /// This allows us to use the same floodfill for everything, but perform unique actions.
        /// </summary>
        /// 
        /// <param name="actionType">Type of action to be performed</param>
        /// <param name="x">X coordinate of the alteration</param>
        /// <param name="y">Y coordinate of the alteration</param>
        /// <param name="playerID">The player index to teleport items to and consume items from, if needed</param>
        /// <param name="consumesItemType">Item to consume if any, -1 to omit (invalid item)</param>
        /// <param name="itemSubtype">ID of the item style that is being replaced with, if needed - may also represent a color</param>
        public static bool DoAlteration(Alteration alteration)
        {
            if (OreExcavator.devmode)
                OreExcavator.Log($"AlterHandler ({alteration.x},{alteration.y}) - {alteration.actionType} = {alteration.consumesItemType}:{alteration.itemSubtype}", Color.Aquamarine, LogType.None);

            if (alteration.x < 0 || alteration.y < 0 || alteration.x > Main.maxTilesX || alteration.y > Main.maxTilesY)
                return false;

            switch (alteration.actionType)
            {
                case ActionType.TileKilled:
                    if (HasAndConsumeMana(1.0f, alteration.playerID) is false)
                        return true;
                    WorldGen.KillTile(alteration.x, alteration.y, !Main.tile[alteration.x, alteration.y].HasTile, false, OreExcavator.ServerConfig.creativeMode);
                    return false;

                case ActionType.WallKilled:
                    if (HasAndConsumeMana(0.5f, alteration.playerID) is false)
                        return true;
                    WorldGen.KillWall(alteration.x, alteration.y, false);
                    return false;

                //case ActionType.WallReplaced:
                //    if (Main.tile[alteration.x, alteration.y].TileType != TileID.Platforms && Main.tile[alteration.x, alteration.y].HasTile && WorldGen.SolidTile(alteration.x, alteration.y))
                //        return false;
                //    goto case ActionType.TileReplaced;

                case ActionType.WallReplaced:
                case ActionType.ExtendPlacement:
                case ActionType.TileReplaced:
                    {
                        // TODO: Move this outside of modify loop?
                        if (alteration.consumesItemType <= ItemID.None) // Invalid item type?
                            return true;
                        int placeType;
                        Item item = new(alteration.consumesItemType);
                        if (alteration.actionType == ActionType.WallReplaced)
                            placeType = item.createWall;
                        else
                            placeType = item.createTile;

                        if (placeType < TileID.Dirt) // Places invalid thing?
                            return true;

                        if (HasAndConsumeMana(2.0f, alteration.playerID) is false)
                            return true;

                        if (OreExcavator.ServerConfig.creativeMode is false && OreExcavator.puppeting is false)
                            if (Main.mouseItem is not null && Main.mouseItem.Name != "" && Main.mouseItem.netID == alteration.consumesItemType && Main.mouseItem.stack > 0)
                                Main.player[alteration.playerID].HeldItem.stack--;
                            else if (Main.player[alteration.playerID].ConsumeItem(alteration.consumesItemType) is false) // Does the player have items to place?
                                return true; // No inventory item

                        if (alteration.actionType == ActionType.WallReplaced)
                            WorldGen.ReplaceWall(alteration.x, alteration.y, (ushort)placeType);
                        else if (alteration.actionType != ActionType.ExtendPlacement)
                            WorldGen.ReplaceTile(alteration.x, alteration.y, (ushort)placeType, alteration.itemSubtype < 0 ? (int)Main.tile[alteration.x, alteration.y].Slope : alteration.itemSubtype);
                        else if (Main.tile[alteration.x, alteration.y].HasTile is false)
                        {
                            WorldGen.PlaceTile(alteration.x, alteration.y, (ushort)placeType, true, false, alteration.playerID, alteration.itemSubtype <= 0 ? (int)Main.tile[alteration.x, alteration.y].Slope : alteration.itemSubtype);
                            WorldGen.KillTile(alteration.x, alteration.y, true, !OreExcavator.ClientConfig.reducedEffects, true);
                        }
                    }
                    return false;

                case ActionType.TurfPlanted:
                    {
                        if (HasAndConsumeMana(2.5f, alteration.playerID) is false)
                            return true;
                        // TODO: Move this outside of modify loop?
                        if (alteration.consumesItemType <= ItemID.None) // Invalid item type?
                            return true; //goto case ActionType.TileKilled;
                        int placeType = new Item(alteration.consumesItemType).createTile;
                        if (placeType < TileID.Dirt) // Places invalid thing?
                            return true;

                        if (!OreExcavator.ServerConfig.creativeMode && !OreExcavator.puppeting)
                            if (Main.player[alteration.playerID].ConsumeItem(alteration.consumesItemType) is false) // Does the player have items to place?
                                return true;

                        // TODO: Retain paint?
                        WorldGen.KillTile(alteration.x, alteration.y, true, !OreExcavator.ClientConfig.reducedEffects, true);
                        WorldGen.SpreadGrass(alteration.x, alteration.y, Main.tile[alteration.x, alteration.y].TileType, placeType, false, 0);
                    }
                    return false;

                case ActionType.TurfHarvested:
                    {
                        if (HasAndConsumeMana(2.0f, alteration.playerID) is false)
                            return true;

                        if (alteration.consumesItemType < ItemID.None) // Invalid grass type?
                            return true;

                        // TODO: Retain paint?
                        WorldGen.KillTile(alteration.x, alteration.y, false, false, true);
                        WorldGen.PlaceTile(alteration.x, alteration.y, (ushort)alteration.consumesItemType, !OreExcavator.ClientConfig.reducedEffects, true, alteration.playerID, alteration.itemSubtype <= 0 ? (int)Main.tile[alteration.x, alteration.y].Slope : alteration.itemSubtype);
                    }
                    return false;

                case ActionType.TilePainted:
                    {
                        if (HasAndConsumeMana(2.5f, alteration.playerID) is false)
                            return true;

                        if (alteration.consumesItemType <= ItemID.None) // Invalid item type?
                            return true;

                        if (!OreExcavator.ServerConfig.creativeMode && !OreExcavator.puppeting)
                            if (Main.player[alteration.playerID].ConsumeItem(alteration.consumesItemType) is false) // Does the player have items to place?
                                return true;

                        WorldGen.paintTile(alteration.x, alteration.y, (byte)alteration.itemSubtype, false);
                    }
                    return false;

                case ActionType.WallPainted:
                    {
                        if (HasAndConsumeMana(2.0f, alteration.playerID) is false)
                            return true;

                        if (alteration.consumesItemType <= ItemID.None) // Invalid item type?
                            return true;

                        if (!OreExcavator.ServerConfig.creativeMode && !OreExcavator.puppeting)
                            if (Main.player[alteration.playerID].ConsumeItem(alteration.consumesItemType) is false) // Does the player have items to place?
                                return true;

                        WorldGen.paintWall(alteration.x, alteration.y, (byte)alteration.itemSubtype, false);
                    }
                    return false;

                case ActionType.ClearedPaint:
                    if (HasAndConsumeMana(1.5f, alteration.playerID) is false)
                        return true;

                    WorldGen.paintTile(alteration.x, alteration.y, PaintID.None, false);
                    WorldGen.paintWall(alteration.x, alteration.y, PaintID.None, false);
                    return false;

                default: // Malformed Alter
                    return true;
            }
            return false;
        }
    }
}
