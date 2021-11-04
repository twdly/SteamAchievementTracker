using System;
using System.Net.Http;
using System.Threading.Tasks;
using SteamWebAPI2;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using SteamWebAPI2.Models;
using SteamWebAPI2.Exceptions;
using System.IO;
using System.Collections.Generic;

namespace steamachievements
{
    class Program
    {
        static readonly HttpClient client = new HttpClient();
        static async Task Main(string[] args)
        {
            SteamWebInterfaceFactory webInterfaceFactory = initiateAPI();

            var steamUserInterface = webInterfaceFactory.CreateSteamWebInterface<SteamUser>(client);
            var steamPlayerInterface = webInterfaceFactory.CreateSteamWebInterface<PlayerService>();

            var userID = await getUserID(steamUserInterface);
            var playerSummaryResponse = await steamUserInterface.GetPlayerSummaryAsync(userID);
            await getPlayerStatus(steamUserInterface, userID);
            var games = await getOwnedGames(steamPlayerInterface, userID);
            checkLibraryPublicity(playerSummaryResponse, games);
            checkGamesAmount(playerSummaryResponse, games);
            selectRandomGame(games);
            getTotalPlaytime(playerSummaryResponse, games);
        }

        private static void getTotalPlaytime(ISteamWebResponse<Steam.Models.SteamCommunity.PlayerSummaryModel> playerSummaryResponse, Steam.Models.SteamCommunity.OwnedGamesResultModel games)
        {
            TimeSpan totalPlaytime = new TimeSpan();
            foreach (var vexo in games.OwnedGames)
            {
                totalPlaytime = vexo.PlaytimeForever + totalPlaytime;
            }
            Console.WriteLine($"{playerSummaryResponse.Data.Nickname} has a total playtime of {totalPlaytime.TotalHours} hours.");
        }

        private static void selectRandomGame(Steam.Models.SteamCommunity.OwnedGamesResultModel games)
        {
            var random = new Random();
            int gameCount = Convert.ToInt32(games.GameCount);
            int number = random.Next(maxValue: gameCount);
            List<string> gameList = new List<string>();
            foreach (var game in games.OwnedGames)
            {
                gameList.Add(game.Name);
            }
            Console.WriteLine($"Tæj mineself has decided that you will play {gameList[number]}");
        }

        private static void checkGamesAmount(ISteamWebResponse<Steam.Models.SteamCommunity.PlayerSummaryModel> playerSummaryResponse, Steam.Models.SteamCommunity.OwnedGamesResultModel games)
        {
            int unplayedGames = 0;
            foreach (var game in games.OwnedGames)
            {
                if (game.PlaytimeForever.TotalHours == 0)
                {
                    // Console.WriteLine(vexo.Name);
                    unplayedGames++;
                }
            }
            Console.WriteLine($"User {playerSummaryResponse.Data.Nickname} currently owns {games.GameCount} games. {unplayedGames} of which are unplayed.");

        }

        private static void checkLibraryPublicity(ISteamWebResponse<Steam.Models.SteamCommunity.PlayerSummaryModel> playerSummaryResponse, Steam.Models.SteamCommunity.OwnedGamesResultModel games)
        {
            if (games.GameCount <= 0)
            {
                Console.WriteLine($"Error: {playerSummaryResponse.Data.Nickname} either does not own any games or has their library private.");
                System.Environment.Exit(0);
            }

        }

        private static async Task<Steam.Models.SteamCommunity.OwnedGamesResultModel> getOwnedGames(PlayerService steamPlayerInterface, ulong userID)
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
            Console.WriteLine("Please enter your steam user link.");
            while (true)
            {
                var input = Console.ReadLine();
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
    }
}
