using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System.Collections.Generic;
using Steam.Models.SteamPlayer;
using Steam.Models.SteamCommunity;

namespace steamachievements
{
    class Program
    {
        static readonly HttpClient client = new HttpClient();
        static async Task Main(string[] args)
        {
            // Use command line arguments to create, read or modify apikey using argument -apikey
            setapiKey(args);

            // Use API key to prepare connection to Steam
            SteamWebInterfaceFactory webInterfaceFactory = initiateAPI();

            // Create interfaces to obtain information from Steam Web API
            var steamUserInterface = webInterfaceFactory.CreateSteamWebInterface<SteamUser>(client);
            var steamPlayerInterface = webInterfaceFactory.CreateSteamWebInterface<PlayerService>();
            var steamUserStats = webInterfaceFactory.CreateSteamWebInterface<SteamUserStats>();

            // Get userID from either vanity URL or direct input
            ulong userID = 0;
            try
            {
                userID = await getUserID(steamUserInterface);
            }
            catch (System.Net.Http.HttpRequestException)
            {
                Console.WriteLine("Unable to establish a connection to Steam. Please check your internet and try again. This may also mean you are using an invalid API key.");
                System.Environment.Exit(1);
            }

            // Write basic information about the user to the console
            var playerSummaryResponse = await steamUserInterface.GetPlayerSummaryAsync(userID);
            await getPlayerStatus(steamUserInterface, userID);

            // Obtain games owned by the user and ensure their library is not private
            var games = await getOwnedGames(steamPlayerInterface, userID);
            checkLibraryPublicity(playerSummaryResponse, games);

            // Ask for user input on what other information they would like
            await getInput(playerSummaryResponse, games, steamUserStats, userID);

            Console.WriteLine("\nThank you for using twdly's Steam Achievement Tracker.");
        }

        private static async Task getInput(ISteamWebResponse<PlayerSummaryModel> playerSummaryResponse, OwnedGamesResultModel games, SteamUserStats steamUserStats, ulong userID)
        {
            while (true)
            {
                var input = prompt("What information would you like?");
                switch (input.ToLower())
                {
                    case "games":
                        checkGamesAmount(playerSummaryResponse, games);
                        return;
                    case "playtime":
                        getTotalPlaytime(playerSummaryResponse, games);
                        return;
                    case "random game":
                        selectRandomGame(games);
                        return;
                    case "achievements":
                        await GameAchievements.analyseAchievements(playerSummaryResponse, games, steamUserStats, userID);
                        return;
                    case "help":
                        Console.WriteLine("Valid options are \"games,\" \"playtime,\" \"random game\" and \"achievements\".");
                        continue;
                    default:
                        Console.WriteLine("Invalid selection. Please type \"help\" for a list of valid selections.");
                        continue;
                }
            }
        }

        public static string prompt(string message)
        {
            Console.Write($"\n{message}\n> ");
            return Console.ReadLine();
        }

        private static void getTotalPlaytime(ISteamWebResponse<PlayerSummaryModel> playerSummaryResponse, OwnedGamesResultModel games)
        {
            var totalPlaytime = new TimeSpan();
            foreach (var game in games.OwnedGames)
            {
                totalPlaytime += game.PlaytimeForever;
            }
            var (playtime, unit) = getPlaytimeUnits(totalPlaytime);
            Console.WriteLine($"{playerSummaryResponse.Data.Nickname} has a total playtime of {Math.Round(playtime, 2)} {unit}.");
        }

        private static (double, string) getPlaytimeUnits(TimeSpan totalPlaytime)
        {
            while (true)
            {
                var input = prompt("What unit would you like the playtime to be in?\n (y)ears, (d)ays, (h)ours, (m)inutes.");
                switch (input.ToLower())
                {
                    case "y":
                        return (totalPlaytime.TotalDays / 365.25, "years");
                    case "d":
                        return (totalPlaytime.TotalDays, "days");
                    case "h":
                        return (totalPlaytime.TotalHours, "hours");
                    case "m":
                        return (totalPlaytime.TotalMinutes, "minutes");
                    default:
                        Console.WriteLine("Invalid input. Please try again, vexo.");
                        continue;
                }
            }
        }

        private static void selectRandomGame(OwnedGamesResultModel games)
        {
            var random = new Random();
            int gameCount = Convert.ToInt32(games.GameCount);
            int number = random.Next(maxValue: gameCount);
            List<string> gameList = new List<string>();
            foreach (var game in games.OwnedGames)
            {
                gameList.Add(game.Name);
            }
            Console.WriteLine($"Tæj has decided that you will play {gameList[number]}");
        }

        private static void checkGamesAmount(ISteamWebResponse<PlayerSummaryModel> playerSummaryResponse, OwnedGamesResultModel games)
        {
            int unplayedGames = 0;
            foreach (var game in games.OwnedGames)
            {
                if (game.PlaytimeForever.TotalHours == 0)
                {
                    unplayedGames++;
                }
            }
            decimal percentagePlayed = Math.Round((Convert.ToDecimal(unplayedGames) / Convert.ToDecimal(games.GameCount)) * 100, 2);
            Console.WriteLine($"User {playerSummaryResponse.Data.Nickname} currently owns {games.GameCount} games. {unplayedGames} of which are unplayed.");
            if (percentagePlayed != 0)
            {
                Console.WriteLine($"This means that {playerSummaryResponse.Data.Nickname} has never played {percentagePlayed}% of their library.");
            }
            else
            {
                Console.WriteLine($"{playerSummaryResponse.Data.Nickname} has no unplayed games. What a gamer!");
            }

        }

        private static void checkLibraryPublicity(ISteamWebResponse<PlayerSummaryModel> playerSummaryResponse, OwnedGamesResultModel games)
        {
            if (games.GameCount <= 0)
            {
                Console.WriteLine($"Error: {playerSummaryResponse.Data.Nickname} either does not own any games or has their library private.");
                System.Environment.Exit(1);
            }

        }

        private static async Task<OwnedGamesResultModel> getOwnedGames(PlayerService steamPlayerInterface, ulong userID)
        {
            var games = await steamPlayerInterface.GetOwnedGamesAsync(userID, includeAppInfo: true);
            return games.Data;
        }

        private static async Task getPlayerStatus(SteamUser steamUserInterface, ulong userID)
        {
            var playerSummaryResponse = await steamUserInterface.GetPlayerSummaryAsync(userID);
            var playerSummaryData = playerSummaryResponse.Data;
            if (playerSummaryData.PlayingGameName != null)
            {
                Console.WriteLine($"Steam user {playerSummaryData.Nickname} is currently {playerSummaryData.UserStatus} and is playing {playerSummaryData.PlayingGameName}");
            }
            else
            {
                Console.WriteLine($"Steam user {playerSummaryData.Nickname} is currently {playerSummaryData.UserStatus} and is currently not playing a game.");
            }
        }

        private static SteamWebInterfaceFactory initiateAPI()
        {
            string apikey = System.IO.File.ReadAllText(@"apikey");
            var webInterfaceFactory = new SteamWebInterfaceFactory(apikey);
            return webInterfaceFactory;
        }

        static async Task<ulong> getUserID(SteamUser userInterface)
        {
            while (true)
            {
                var input = prompt("Please enter your steam user link.");
                ulong vexo;
                if (ulong.TryParse(input, out vexo))
                {
                    return vexo;
                }
                try
                {
                    var steamUserID = await userInterface.ResolveVanityUrlAsync(input);
                    return steamUserID.Data;
                }
                catch (SteamWebAPI2.Exceptions.VanityUrlNotResolvedException)
                {
                    Console.WriteLine("That Steam URL could not be found. Please check your spelling and try again.");
                    continue;
                }
            }
        }

        private static void setapiKey(string[] args)
        {
            if (args.Length == 2)
            {
                if (args[0] == "-apikey")
                {
                    if (File.Exists(@"apikey"))
                    {
                        var oldKey = File.ReadAllText(@"apikey");
                        File.WriteAllText(@"apikey", args[1]);
                        Console.WriteLine($"API key successfully changed from {oldKey} to {args[1]}");
                        System.Environment.Exit(0);
                    }
                    else
                    {
                        File.WriteAllText("apikey", args[1]);
                        Console.WriteLine("API key successfully added and the software is ready to use.");
                        System.Environment.Exit(0);
                    }
                }
            }
            else if (args.Length == 1)
            {
                if (args[0] == "-apikey" && File.Exists(@"apikey"))
                {
                    var key = File.ReadAllText(@"apikey");
                    Console.WriteLine($"Current API key is {key}.");
                    System.Environment.Exit(0);
                }
                else if (args[0] == "-apikey")
                {
                    Console.WriteLine("API key file not found.\nPlease use the -apikey argument followed by your key to set your API key.");
                    System.Environment.Exit(1);
                }
            }
            else if (args.Length == 0)
            {
                if (!File.Exists(@"apikey"))
                {
                    Console.WriteLine("API key has not yet been set. Please use the -apikey argument followed by your key to set your API key.");
                    System.Environment.Exit(1);
                }
            }
        }
    }
}
