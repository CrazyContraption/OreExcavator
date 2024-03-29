﻿namespace OreExcavator.Enumerations
{
    public enum ActionType : byte
    {
        None = 0,
        HaltExcavation,
        HaltExcavations,
        ResetExcavations,
        TileKilled,
        TilePlaced,
        WallKilled,
        WallPlaced,
        TileReplaced,
        WallReplaced,
        SeedPlanted,
        SeedHarvested,
        TilePainted,
        WallPainted,
        ClearedPaint,
        TileWhiteListed,
        WallWhiteListed,
        ItemWhiteListed,
        TileBlackListed,
        WallBlackListed,
        ItemBlackListed,
        ExtendPlacement
    }

    public enum LogType : byte
    {
        None = 0,
        Info,
        Debug,
        Warn,
        Error,
        Fatal
    }

    public enum Direction : byte
    {
        None = 0,
        Vertical,
        Horizontal
    }
}