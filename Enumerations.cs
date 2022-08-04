namespace OreExcavator.Enumerations
{
    public enum ActionType : byte
    {
        None = 0,
        HaltExcavations,
        ResetExcavations,
        TileKilled,
        TilePlaced,
        WallKilled,
        WallPlaced,
        TileReplaced,
        WallReplaced,
        TurfPlanted,
        TurfHarvested,
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
}