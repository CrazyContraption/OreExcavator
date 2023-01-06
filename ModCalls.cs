using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OreExcavator.Enumerations;
using Terraria;
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

			if (args.Length == 0)
				throw new ArgumentException("Expected at least one argument specifying case: getWhitelist, whitelistTile, whitelistWall, whitelistItem; Got none");

			// This check makes sure that the argument is a string using pattern matching.
			// Since we only need one parameter, we'll take only the first item in the array..
			if (args[0] is string callType)
			{
				// ..And treat it as a command type.
				switch (callType.ToLower())
				{
					case "iswhitelistedtile":
						{
							// Returns true or false if a specified Tile ID is whitelisted - or null if it's an invalid Tile ID
							if (args.Length < 2)
								throw new ArgumentException("Missing second argument: (int) Tile id");
							if (args[1] is null || args[1] is not ushort tileID)
								throw new Exception($"Expected an argument of type (int) when getting a Tile ID, but got type ({args[1].GetType().Name}) instead.");
							string name = GetFullNameById(tileID, ActionType.TileWhiteListed);
							return ClientConfig.tileWhitelistToggled ? true : (name == null ? null : ClientConfig.tileWhitelist.Contains(name));
						}

					case "addwhitelistedtile":
						{
							// Returns true or false if a specified Tile ID has been added successfully or not - or null if it exists already, or is an invalid Tile ID
							if (args.Length < 2)
								throw new ArgumentException("Missing second argument: (int) Tile id");
							if (args[1] is null || args[1] is not ushort tileID)
								throw new Exception($"Expected an argument of type (int) when adding a Tile ID, but got type ({args[1].GetType().Name}) instead.");
							string name = GetFullNameById(tileID, ActionType.TileWhiteListed);
							if (name == null ? true : ClientConfig.tileWhitelist.Contains(name))
								return null;
							ClientConfig.wallWhitelist.Add(name);
							return ClientConfig.wallWhitelist.Contains(name);
						}

					case "iswhitelistedwall":
						{
							// Returns true or false if a specified Wall ID is whitelisted - or null if it's an invalid Wall ID
							if (args.Length < 2)
								throw new ArgumentException("Missing second argument: (int) Wall id");
							if (args[1] is null || args[1] is not ushort wallID)
								throw new Exception($"Expected an argument of type (int) when getting a Wall ID, but got type ({args[1].GetType().Name}) instead.");
							string name = GetFullNameById(wallID, ActionType.WallWhiteListed);
							return ClientConfig.wallWhitelistToggled ? true : (name == null ? null : ClientConfig.wallWhitelist.Contains(name));
						}

					case "addwhitelistedwall":
						{
							// Returns true or false if a specified Wall ID has been added successfully or not - or null if it exists already, or is an invalid Wall ID
							if (args.Length < 2)
								throw new ArgumentException("Missing second argument: (int) Wall id");
							if (args[1] is null || args[1] is not ushort wallID)
								throw new Exception($"Expected an argument of type (int) when adding a Wall ID, but got type ({args[1].GetType().Name}) instead.");
							string name = GetFullNameById(wallID, ActionType.WallWhiteListed);
							if (name == null ? true : ClientConfig.wallWhitelist.Contains(name))
								return null;
							ClientConfig.wallWhitelist.Add(name);
							return ClientConfig.wallWhitelist.Contains(name);
						}

					case "iswhitelisteditem":
						{
							// Returns true or false if a specified Item ID is whitelisted - or null if it's an invalid Item ID
							if (args.Length < 2)
								throw new ArgumentException("Missing second argument: (int) Item id");
							if (args[1] is null || args[1] is not short itemID)
								throw new Exception($"Expected an argument of type (int) when getting a Item ID, but got type ({args[1].GetType().Name}) instead.");
							string name = GetFullNameById(itemID, ActionType.ItemWhiteListed);
							return ClientConfig.itemWhitelistToggled ? true : (name == null ? null : ClientConfig.itemWhitelist.Contains(name));
						}

					case "addwhitelisteditem":
						{
							// Returns true or false if a specified Item ID has been added successfully or not - or null if it exists already, or is an invalid Item ID
							if (args.Length < 2)
								throw new ArgumentException("Missing second argument: (int) Item id");
							if (args[1] is null || args[1] is not short itemID)
								throw new Exception($"Expected an argument of type (int) when adding a Item ID, but got type ({args[1].GetType().Name}) instead.");
							string name = GetFullNameById(itemID, ActionType.ItemWhiteListed);
							if (name == null ? true : ClientConfig.itemWhitelist.Contains(name))
								return null;
							ClientConfig.itemWhitelist.Add(name);
							return ClientConfig.itemWhitelist.Contains(name);
						}

					case "savewhitelist":
						// Forces client changes committed to RAM to be written to storage - so future reloads will use updated data. Returns true/false based on success.
						try { SaveConfig(ClientConfig); } catch { return false; }
						return true;

					/// W.I.P.
					case "saveblacklist": // Capital makes this uncallable while we develop blacklist calls
						throw new NotImplementedException("Work in progress!");
						// Forces server changes committed to RAM to be written to storage - so future reloads will use updated data. Returns true/false based on success.
						// Warning: Use with care! You CAN corrupt configs this way if abused. Use sparingly and with caution.
						// Warning: This will only work if called using a server instance of the game - clients cannot force a server to make changes! Duh!
						if (Main.netMode != NetmodeID.Server)
							return false;
						try { SaveConfig(ServerConfig); } catch { return false; }
						return true;

					case "excavatetile":
						throw new NotImplementedException("Work in progress!");

					case "excavatewall":
						throw new NotImplementedException("Work in progress!");

					case "chainswap":
						throw new NotImplementedException("Work in progress!");

					default:
						throw new ArgumentOutOfRangeException("Call argument does not match any defined cases; please read the documentation here: https://steamcommunity.com/sharedfiles/filedetails/?id=2565639705&tscn=1665678955");
				}
			}
			throw new ArgumentOutOfRangeException("Malformed call request; please read the documentation here: https://steamcommunity.com/sharedfiles/filedetails/?id=2565639705&tscn=1665678955");
		}
	}
}
