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
        SeedPlanted,
        ItemPainted,
        TileWhiteListed,
        WallWhiteListed,
        ItemWhiteListed,
        TileBlackListed,
        WallBlackListed,
        ItemBlackListed
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