using Microsoft.Xna.Framework;       // Color
using Terraria.ModLoader;

namespace OreExcavator.Commands
{
    public class VersionCommand : ModCommand
    {
        public override CommandType Type
            => CommandType.Chat;

        public override string Command
            => "version";

        public override string Usage
            => $"\n /{Command}" +
               $"\n /{Command} [ModInternalName]" +
               $"\n /{Command} [ModInternalName1] [ModInternalName2] [ModInternalName3]...";

        public override string Description
            => "Displays the installed version of a specified mod, such as OreExcavator.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            caller.Reply($"\n{input}{(args == null || args.Length <= 0 ? " - No mods specified" : "")}");
            if (args == null || args.Length <= 0)
                caller.Reply($"[tModLoader (v{ModLoader.LastLaunchedTModLoaderVersion})]", Color.Aqua);
            else
                for (ushort index = 0; index < args.Length; index++)
                    if (ModLoader.TryGetMod(args[index], out Mod mod))
                        caller.Reply($"[{mod.DisplayName} (v{mod.Version})] - Built with tML v{mod.TModLoaderVersion}", Color.GreenYellow);
                    else
                        caller.Reply($"[{args[index]}] - Unknown mod", Color.Orange);
        }
    }
}