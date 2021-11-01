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

            checkGamesAmount(playerSummaryResponse, games);
        }

        private static void checkGamesAmount(ISteamWebResponse<Steam.Models.SteamCommunity.PlayerSummaryModel> playerSummaryResponse, Steam.Models.SteamCommunity.OwnedGamesResultModel games)
        {
            int unplayedGames = 0;
            foreach (var vexo in games.OwnedGames)
            {
                if (vexo.PlaytimeForever.TotalHours == 0)
                {
                    // Console.WriteLine(vexo.Name);
                    unplayedGames++;
                }
            }
            Console.WriteLine($"User {playerSummaryResponse.Data.Nickname} currently owns {games.GameCount} games. {unplayedGames} of which are unplayed.");
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
            var input = Console.ReadLine();
            var steamUserID = await userInterface.ResolveVanityUrlAsync(input);
            return steamUserID.Data;
        }
    }
}
