using System;
using System.Net.Http;
using System.Threading.Tasks;
using SteamWebAPI2;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using SteamWebAPI2.Models;
using SteamWebAPI2.Exceptions;
using System.IO;


namespace steamachievements
{
    class Program
    {
        static readonly HttpClient client = new HttpClient();
        static async Task Main(string[] args)
        {
            // 76561198184545774
            string apikey = System.IO.File.ReadAllText(@"apikey");
            ulong userID = getUserID();
            var webInterfaceFactory = new SteamWebInterfaceFactory(apikey);
            var steamUserInterface = webInterfaceFactory.CreateSteamWebInterface<SteamUser>(client);
            var steamPlayerInterface = webInterfaceFactory.CreateSteamWebInterface<PlayerService>();
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
            // var playerOwnedGames = await steamPlayerInterface.GetOwnedGamesAsync(76561198184545774, includeAppInfo: true);
            // foreach (var vexo in playerOwnedGames.Data.OwnedGames)
            // {
            //     Console.WriteLine(vexo.Name);
            // }
        }

        private static ulong getUserID()
        {
            Console.WriteLine("Please enter your steam profile ID.");
            var input = Console.ReadLine();
            return Convert.ToUInt64(input);
        }
    }
}
