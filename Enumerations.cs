namespace OreExcavator.Enumerations
{
    public enum ActionType : byte
    {
        None = 0,
        HaltExcavations,
        ResetExcavations,
        TileKilled,
        WallKilled,
        TileReplaced,
        WallReplaced,
        TileWhiteListed,
        WallWhiteListed,
        ItemWhiteListed,
        TileBlackListed,
        WallBlackListed,
        ItemBlackListed,
        SeedPlanted,
        ItemPainted
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