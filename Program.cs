﻿using System;
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
            ulong userID = 0;
            try
            {
                userID = await getUserID(steamUserInterface);
            }
            catch (System.Net.Http.HttpRequestException)
            {
                Console.WriteLine("Unable to establish a connection to Steam. Please check your internet and try again");
                System.Environment.Exit(1);
            }
            var playerSummaryResponse = await steamUserInterface.GetPlayerSummaryAsync(userID);
            await getPlayerStatus(steamUserInterface, userID);
            var games = await getOwnedGames(steamPlayerInterface, userID);
            checkLibraryPublicity(playerSummaryResponse, games);
            getInput(playerSummaryResponse, games);
            Console.WriteLine("\nThank you for using twdly's Steam Achievement Tracker.");
        }

        private static void getInput(ISteamWebResponse<Steam.Models.SteamCommunity.PlayerSummaryModel> playerSummaryResponse, Steam.Models.SteamCommunity.OwnedGamesResultModel games)
        {
            Console.WriteLine("What information would you like?");
            bool validInputReceived = false;
            while (true)
            {

                var input = Console.ReadLine();
                switch (input.ToLower())
                {
                    case "games":
                        checkGamesAmount(playerSummaryResponse, games);
                        validInputReceived = true;
                        break;
                    case "playtime":
                        getTotalPlaytime(playerSummaryResponse, games);
                        validInputReceived = true;
                        break;
                    case "random game":
                        selectRandomGame(games);
                        validInputReceived = true;
                        break;
                    default:
                        Console.WriteLine("That option cannot be found. Please check your spelling and try again.");
                        continue;
                }
                if (validInputReceived)
                {
                    break;
                }
            }
        }

        private static void getTotalPlaytime(ISteamWebResponse<Steam.Models.SteamCommunity.PlayerSummaryModel> playerSummaryResponse, Steam.Models.SteamCommunity.OwnedGamesResultModel games)
        {
            TimeSpan totalPlaytime = new TimeSpan();
            foreach (var vexo in games.OwnedGames)
            {
                totalPlaytime += vexo.PlaytimeForever;
            }
            var playtimeAndUnit = getPlaytimeUnits(totalPlaytime);
            Console.WriteLine($"{playerSummaryResponse.Data.Nickname} has a total playtime of {Math.Round(playtimeAndUnit.Item1, 2)} {playtimeAndUnit.Item2}.");
        }

        private static (double, string) getPlaytimeUnits(TimeSpan totalPlaytime)
        {
            while (true)
            {
                Console.WriteLine("What unit would you like the playtime to be in?\n (y)ears, (d)ays, (h)ours, (m)inutes.");
                var input = Console.ReadLine();
                (double, string) output;
                switch (input.ToLower())
                {
                    case "y":
                        output = (totalPlaytime.TotalDays / 365.25, "years");
                        return output;
                    case "d":
                        output = (totalPlaytime.TotalDays, "days");
                        return output;
                    case "h":
                        output = (totalPlaytime.TotalHours, "hours");
                        return output;
                    case "m":
                        output = (totalPlaytime.TotalHours * 60, "minutes");
                        return output;
                    default:
                        Console.WriteLine("Invalid input. Please try again, vexo.");
                        continue;
                }
            }
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
            decimal percentagePlayed;
            foreach (var game in games.OwnedGames)
            {
                if (game.PlaytimeForever.TotalHours == 0)
                {
                    unplayedGames++;
                }
            }
            try
            {
                percentagePlayed = Math.Round((Convert.ToDecimal(unplayedGames) / Convert.ToDecimal(games.GameCount)) * 100, 2);
            }
            catch (System.DivideByZeroException)
            {
                percentagePlayed = 0;
            }
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

        private static void checkLibraryPublicity(ISteamWebResponse<Steam.Models.SteamCommunity.PlayerSummaryModel> playerSummaryResponse, Steam.Models.SteamCommunity.OwnedGamesResultModel games)
        {
            if (games.GameCount <= 0)
            {
                Console.WriteLine($"Error: {playerSummaryResponse.Data.Nickname} either does not own any games or has their library private.");
                System.Environment.Exit(1);
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
    }
}
