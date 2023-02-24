using Microsoft.Xna.Framework;
using OreExcavator.Enumerations;
using Terraria;
using Terraria.ID;

namespace OreExcavator
{
    internal class Alteration
    {

        public readonly ushort? threadID;
        private readonly ActionType actionType;
        private readonly byte playerID;
        private readonly bool puppeting;
        private readonly int consumesItemType = -1;
        private readonly sbyte consumesItemSubtype = -1;
        private bool? done;

        public Alteration(ushort? threadID, ActionType actionType, byte playerID, bool puppeting = false, int consumesItemType = -1, sbyte consumesItemSubtype = -1)
        {
            this.threadID = threadID; 
            this.actionType = actionType;
            this.playerID = playerID;
            this.puppeting = puppeting;
            this.consumesItemType = consumesItemType;
            this.consumesItemSubtype = consumesItemSubtype;
        }

        internal bool? Finished()
        {
            return done;
        }

        internal static float GetManaCost(ActionType actionType)
        {
            return actionType switch
            {
                ActionType.TileKilled => 1f,
                ActionType.TilePlaced => 1.5f,
                ActionType.WallKilled => 0.5f,
                ActionType.WallPlaced => 1f,
                ActionType.TileReplaced => 1.5f,
                ActionType.WallReplaced => 1f,
                ActionType.SeedPlanted => 1.5f,
                ActionType.SeedHarvested => 2f,
                ActionType.TilePainted => 2f,
                ActionType.WallPainted => 1.5f,
                ActionType.ClearedPaint => 1f,
                ActionType.ExtendPlacement => 2f,
                _ => 0f,
            };
        }

        internal static bool HasAndConsumeMana(float manaCost, int player = -1)
        {
            if (OreExcavator.ServerConfig.creativeMode is true)
                return true;
            if (manaCost <= 0)
                return true;
            if (player < 0)
                return false;
            return Main.player[player].CheckMana((int)(OreExcavator.ServerConfig.manaConsumption * manaCost), true, OreExcavator.ClientConfig.refillMana is false);
        }

        internal bool HasAndConsumeItem(int item, int player = -1, short amount = 1)
        {
            if (item <= ItemID.None || player < 0)
                return false;
            if (OreExcavator.ServerConfig.creativeMode is true || puppeting is true)
                return true;

            if (Main.mouseItem is not null && Main.mouseItem.netID == item && Main.mouseItem.stack >= 0 + amount)
                Main.player[player].HeldItem.stack -= amount;
            else if (amount > 0)
            {
                if (Main.player[player].ConsumeItem(consumesItemType) is false) // Does the player have items to place?
                    return false; // No inventory item
            }
            else if (amount < 0)
                Main.player[player].QuickSpawnItem(Main.player[player].GetSource_FromThis(), consumesItemType, amount * -1);

            return true;
        }

        /// <summary>
        /// Handles the individual manipulations based on the action type desired when called.
        /// This allows us to use the same floodfill for everything, but perform unique actions.
        /// </summary>
        /// 
        /// <param name="x">X coordinate of the alteration</param>
        /// <param name="y">Y coordinate of the alteration</param>
        public bool DoAlteration(int x, int y)
        {
            done = false;

            if (WorldGen.InWorld(x, y) is false)
                return false;

#if DEBUG
            OreExcavator.Log($"AlterHandler ({x},{y}) - {actionType} = {consumesItemType}:{consumesItemSubtype}", Color.Aquamarine, LogType.None);
#endif

            done = false;
            float manaCost = GetManaCost(actionType);

            switch (actionType)
            {
                case ActionType.TileKilled:
                    if (puppeting is false && HasAndConsumeMana(manaCost, playerID) is false)
                        return true;
                    WorldGen.KillTile(x, y, Main.tile[x, y].HasTile is false, false, OreExcavator.ServerConfig.creativeMode);
                    return false;

                case ActionType.WallKilled:
                    if (puppeting is false && HasAndConsumeMana(manaCost, playerID) is false)
                        return true;
                    WorldGen.KillWall(x, y, false);
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
                        if (consumesItemType <= ItemID.None) // Invalid item type?
                            return true;
                        int placeType;
                        Item item = new(consumesItemType);
                        if (actionType == ActionType.WallReplaced)
                            placeType = item.createWall;
                        else
                            placeType = item.createTile;

                        if (placeType < TileID.Dirt) // Places invalid thing?
                            return true;

                        if (puppeting is false & HasAndConsumeMana(manaCost, playerID) is false)
                            return true;

                        if (HasAndConsumeItem(consumesItemType, playerID) is false)
                            return true;

                        if (actionType == ActionType.WallReplaced)
                        {
                            if (WorldGen.ReplaceWall(x, y, (ushort)placeType) is false)
                                { }//_ = HasAndConsumeItem(consumesItemType, playerID, -1);
                        }
                        else if (actionType is not ActionType.ExtendPlacement)
                        {
                            if (WorldGen.ReplaceTile(x, y, (ushort)placeType, consumesItemSubtype < 0 ? (int)Main.tile[x, y].Slope : consumesItemSubtype) is false)
                                { }//_ = HasAndConsumeItem(consumesItemType, playerID, -1);
                        }
                        else if (Main.tile[x, y].HasTile is false)
                        {
                            if (WorldGen.PlaceTile(x, y, (ushort)placeType, true, false, playerID, consumesItemSubtype <= 0 ? (int)Main.tile[x, y].Slope : consumesItemSubtype) is false)
                            {
                                _ = HasAndConsumeItem(consumesItemType, playerID, -1);
                                return true;
                            }
                            WorldGen.KillTile(x, y, true, OreExcavator.ClientConfig.reducedEffects is false, true);
                        }
                        return false;
                    }
                    

                case ActionType.SeedPlanted:
                    {
                        if (puppeting is false && HasAndConsumeMana(manaCost, playerID) is false)
                            return true;
                        // TODO: Move this outside of modify loop?
                        if (consumesItemType <= ItemID.None) // Invalid item type?
                            return true; //goto case ActionType.TileKilled;
                        int placeType = new Item(consumesItemType).createTile;
                        if (placeType < TileID.Dirt) // Places invalid thing?
                            return true;

                        if (HasAndConsumeItem(consumesItemType, playerID) is false)
                            return true;

                        WorldGen.KillTile(x, y, true, OreExcavator.ClientConfig.reducedEffects is false, true);
                        WorldGen.SpreadGrass(x, y, Main.tile[x, y].TileType, placeType, false, 0); // cannot use repeat because we want to consume items
                    }
                    return false;

                case ActionType.SeedHarvested:
                    {
                        if (puppeting is false && HasAndConsumeMana(manaCost, playerID) is false)
                            return true;

                        if (consumesItemType < ItemID.None) // Invalid grass type?
                            return true;

                        // TODO: Retain paint?
                        WorldGen.KillTile(x, y, false, false, true);
                        WorldGen.PlaceTile(x, y, (ushort)consumesItemType, OreExcavator.ClientConfig.reducedEffects is false, true, playerID, consumesItemSubtype <= 0 ? (int)Main.tile[x, y].Slope : consumesItemSubtype);
                    }
                    return false;

                case ActionType.TilePainted:
                    {
                        if (puppeting is false && HasAndConsumeMana(manaCost, playerID) is false)
                            return true;

                        if (consumesItemType <= ItemID.None) // Invalid item type?
                            return true;

                        if (HasAndConsumeItem(consumesItemType, playerID) is false)
                            return true;

                        WorldGen.paintTile(x, y, (byte)consumesItemSubtype, false);
                    }
                    return false;

                case ActionType.WallPainted:
                    {
                        if (puppeting is false && HasAndConsumeMana(manaCost, playerID) is false)
                            return true;

                        if (consumesItemType <= ItemID.None) // Invalid item type?
                            return true;

                        if (HasAndConsumeItem(consumesItemType, playerID) is false)
                            return true;

                        WorldGen.paintWall(x, y, (byte)consumesItemSubtype, false);
                    }
                    return false;

                case ActionType.ClearedPaint:
                    if (puppeting is false && HasAndConsumeMana(manaCost, playerID) is false)
                        return true;

                    WorldGen.paintTile(x, y, PaintID.None, false);
                    WorldGen.paintWall(x, y, PaintID.None, false);
                    return false;

                default: // Malformed Alter
                    return true;
            }
        }
    }
}
