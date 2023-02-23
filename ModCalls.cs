using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OreExcavator.Enumerations;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace OreExcavator
{
    partial class OreExcavator
    {
		// This allows mod developers to access OreExcavator's data without having to set it a reference.
		public override object Call(params object[] args)
		{
			// Make sure the call doesn't include anything that could potentially cause exceptions.
			if (args is null)
				throw new ArgumentNullException(nameof(args), "Expected at least one argument specifying case: getWhitelist, whitelistTile, whitelistWall, whitelistItem; Got null");

			if (args.Length < 0)
				throw new ArgumentException("Expected at least one argument specifying case: getWhitelist, whitelistTile, whitelistWall, whitelistItem; Got none");
			

			// This check makes sure that the argument is a string using pattern matching.
			// Since we only need one parameter, we'll take only the first item in the array..
			if (args[0] is not string callType)
				throw new ArgumentOutOfRangeException("Malformed call request; please read the documentation on our Discord: https://discord.gg/FtrsRtPe6h");

			// ..And treat it as a command type.
			switch (callType.ToLower())
			{
				case "iswhitelistedtile":
					{
						// Returns true or false if a specified Tile ID is whitelisted AND not blacklisted - or null if it's an invalid Tile ID
						if (args.Length < 2)
							throw new ArgumentException("Missing second argument: (int) Tile id");
						if (args[1] is null || args[1] is not int tileID)
							throw new Exception($"Expected an argument of type (int) when getting a Tile ID, but got type ({args[1].GetType().Name}) instead.");
						return CheckIfAllowed(tileID, ActionType.TileWhiteListed, args.Length > 2 && args[2] is int subtype ? subtype : -1);
					}

				case "iswhitelistedwall":
					{
						// Returns true or false if a specified Wall ID is whitelisted - or null if it's an invalid Wall ID
						if (args.Length < 2)
							throw new ArgumentException("Missing second argument: (int) Wall id");
						if (args[1] is null || args[1] is not int wallID)
							throw new Exception($"Expected an argument of type (int) when getting a Wall ID, but got type ({args[1].GetType().Name}) instead.");
						string name = GetFullNameById(wallID, ActionType.WallWhiteListed);
						return CheckIfAllowed(wallID, ActionType.WallWhiteListed);
					}

				case "iswhitelisteditem":
					{
						// Returns true or false if a specified Item ID is whitelisted - or null if it's an invalid Item ID
						if (args.Length < 2)
							throw new ArgumentException("Missing second argument: (int) Item id");
						if (args[1] is null || args[1] is not int itemID)
							throw new Exception($"Expected an argument of type (int) when getting a Item ID, but got type ({args[1].GetType().Name}) instead.");
						string name = GetFullNameById(itemID, ActionType.ItemWhiteListed);
						return CheckIfAllowed(itemID, ActionType.ItemWhiteListed);
					}

				case "addwhitelistedtile":
					{
						if (Main.netMode is NetmodeID.Server)
							return null;
						// Returns true or false if a specified Tile ID has been added successfully or not - false if it exists already, or null if it is an invalid Tile ID
						if (args.Length < 2)
							throw new ArgumentException("Missing second argument: (int) Tile id");
						if (args[1] is null || args[1] is not int tileID)
							throw new Exception($"Expected an argument of type (int) when adding a Tile ID, but got type ({args[1].GetType().Name}) instead.");
						string name = GetFullNameById(tileID, ActionType.TileWhiteListed, args.Length > 2 && args[2] is int subtype ? subtype : -1);
						if (ClientConfig.tileWhitelist.Contains(name))
							return false;
						if (name is null)
							return null;
						ClientConfig.tileWhitelist.Add(name);
						return ClientConfig.tileWhitelist.Contains(name);
					}

				case "addwhitelistedwall":
					{
						if (Main.netMode is NetmodeID.Server)
							return null;
						// Returns true or false if a specified Wall ID has been added successfully or not - false if it exists already, or null if it is an invalid Wall ID
						if (args.Length < 2)
							throw new ArgumentException("Missing second argument: (int) Wall id");
						if (args[1] is null || args[1] is not int wallID)
							throw new Exception($"Expected an argument of type (int) when adding a Wall ID, but got type ({args[1].GetType().Name}) instead.");
						string name = GetFullNameById(wallID, ActionType.WallWhiteListed);
						if (ClientConfig.wallWhitelist.Contains(name))
							return false;
						if (name is null)
							return null;
						ClientConfig.wallWhitelist.Add(name);
						return ClientConfig.wallWhitelist.Contains(name);
					}

				case "addwhitelisteditem":
					{
						if (Main.netMode is NetmodeID.Server)
							return null;
						// Returns true or false if a specified Item ID has been added successfully or not - false if it exists already, or null if it is an invalid Item ID
						if (args.Length < 2)
							throw new ArgumentException("Missing second argument: (int) Item id");
						if (args[1] is null || args[1] is not int itemID)
							throw new Exception($"Expected an argument of type (int) when adding a Item ID, but got type ({args[1].GetType().Name}) instead.");
						string name = GetFullNameById(itemID, ActionType.ItemWhiteListed);
						if (ClientConfig.itemWhitelist.Contains(name))
							return false;
						if (name is null)
							return null;
						ClientConfig.itemWhitelist.Add(name);
						return ClientConfig.itemWhitelist.Contains(name);
					}

				case "addblacklistedtile":
					{
						if (Main.netMode is NetmodeID.MultiplayerClient)
							return null;
						// Returns true or false if a specified Tile ID has been banned successfully or not - false if it exists already, or null if it is an invalid Tile ID
						if (args.Length < 2)
							throw new ArgumentException("Missing second argument: (int) Tile id");
						if (args[1] is null || args[1] is not int tileID)
							throw new Exception($"Expected an argument of type (int) when banning a Tile ID, but got type ({args[1].GetType().Name}) instead.");
						string name = GetFullNameById(tileID, ActionType.TileBlackListed, args.Length > 2 && args[2] is int subtype ? subtype : -1);
						if (ServerConfig.tileBlacklist.Contains(name))
							return false;
						if (name is null)
							return null;
						ServerConfig.tileBlacklist.Add(name);
						return ServerConfig.tileBlacklist.Contains(name);
					}

				case "addblacklistedwall":
					{
						if (Main.netMode is NetmodeID.MultiplayerClient)
							return null;
						// Returns true or false if a specified Wall ID has been banned successfully or not - false if it exists already, or null if it is an invalid Wall ID
						if (args.Length < 2)
							throw new ArgumentException("Missing second argument: (int) Wall id");
						if (args[1] is null || args[1] is not int wallID)
							throw new Exception($"Expected an argument of type (int) when banning a Wall ID, but got type ({args[1].GetType().Name}) instead.");
						string name = GetFullNameById(wallID, ActionType.WallBlackListed);
						if (name is null ? true : ServerConfig.wallBlacklist.Contains(name))
							return null;
						ServerConfig.wallBlacklist.Add(name);
						return ServerConfig.wallBlacklist.Contains(name);
					}

				case "addblacklisteditem":
					{
						if (Main.netMode is NetmodeID.MultiplayerClient)
							return null;
						// Returns true or false if a specified Item ID has been banned successfully or not - false if it exists already, or null if it is an invalid Item ID
						if (args.Length < 2)
							throw new ArgumentException("Missing second argument: (int) Item id");
						if (args[1] is null || args[1] is not int itemID)
							throw new Exception($"Expected an argument of type (int) when banning a Item ID, but got type ({args[1].GetType().Name}) instead.");
						string name = GetFullNameById(itemID, ActionType.ItemBlackListed);
						if (name is null ? true : ServerConfig.itemBlacklist.Contains(name))
							return null;
						ServerConfig.itemBlacklist.Add(name);
						return ServerConfig.itemBlacklist.Contains(name);
					}

				case "excavatetile":
					throw new NotImplementedException("Work in progress!");

				case "excavatewall":
					throw new NotImplementedException("Work in progress!");

				case "chainswap":
					throw new NotImplementedException("Work in progress!");

				case "savewhitelist":
					// Forces client changes committed to RAM to be written to storage - so future reloads will use updated data. Returns true/false based on success.
					if (Main.netMode is NetmodeID.Server)
						return null;
					try { ExcavatorSystem.SaveConfig(ClientConfig); } catch { return false; }
					return true;

				case "saveblacklist": // Capital makes this uncallable while we develop blacklist calls
					// throw new NotImplementedException("Work in progress!");
					// Forces server changes committed to RAM to be written to storage - so future reloads will use updated data. Returns true/false based on success.
					// Warning: Use with care! You CAN corrupt configs this way if abused. Use sparingly and with caution.
					// Warning: This will only work if called using a server instance of the game - clients cannot force a server to make changes! Duh!
					if (Main.netMode is NetmodeID.MultiplayerClient)
						return null;
					try { ExcavatorSystem.SaveConfig(ServerConfig); } catch { return false; }
					return true;

				case "getexcavationkey":
					return (ExcavateHotkey.GetAssignedKeys(PlayerInput.UsingGamepad ? InputMode.XBoxGamepad : InputMode.Keyboard)[0] ?? null) as string;

				default:
					throw new ArgumentOutOfRangeException("Call argument does not match any defined cases; please read the documentation on our Discord: https://discord.gg/FtrsRtPe6h");
			}
		}
	}
}