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
    class GameAchievements
    {
        public string Name { get; set; }
        public uint GameId { get; set; }
        public int TotalAchievements { get; set; }
        public int OwnedAchievements { get; set; }

        public static async Task analyseAchievements(ISteamWebResponse<PlayerSummaryModel> playerSummaryResponse, OwnedGamesResultModel games, SteamUserStats steamUserStats, ulong userID)
        {
            List<GameAchievements> gameAchievements = new List<GameAchievements>();
            foreach (var game in games.OwnedGames)
            {
                var gameStats = new GameAchievements();
                gameStats.GameId = game.AppId;
                gameStats.Name = game.Name;
                ISteamWebResponse<IReadOnlyCollection<GlobalAchievementPercentageModel>> achievements;
                try
                {
                    achievements = await steamUserStats.GetGlobalAchievementPercentagesForAppAsync(game.AppId);
                }
                catch (System.Net.Http.HttpRequestException)
                {
                    Console.WriteLine($"{game.Name} does not have any achievements");
                    continue;
                }
                Console.WriteLine($"Achievements in {game.Name} are:");
                if (achievements.ContentLength != 0)
                {
                    var achievementCount = 0;
                    foreach (var achievement in achievements.Data)
                    {
                        Console.WriteLine($"{achievement.Name} has a roilo of {achievement.Percent}");
                        achievementCount++;
                    }
                    Console.WriteLine($"{game.Name} has {achievementCount} achievements.");
                    gameStats.TotalAchievements = achievementCount;
                    ISteamWebResponse<PlayerAchievementResultModel> personalAchievements;
                    try
                    {
                        personalAchievements = await steamUserStats.GetPlayerAchievementsAsync(game.AppId, userID);
                    }
                    catch (System.Net.Http.HttpRequestException)
                    {
                        Console.WriteLine($"{game.Name} has no achievements");
                        continue;
                    }
                    var ownedAchievements = 0;
                    foreach (var vexo in personalAchievements.Data.Achievements)
                    {
                        ownedAchievements++;
                        Console.WriteLine($"This vexo has achieved {vexo.APIName}");
                    }
                    gameStats.OwnedAchievements = ownedAchievements;
                    gameAchievements.Add(gameStats);
                }
            }
            getAchievementInput();
        }

        private static void getAchievementInput()
        {
            while (true)
            {
                string input = Program.prompt("What would you like about your achievements?");
                switch (input)
                {
                    case "stats":
                        return;
                    case "advice":
                        return;
                    default:
                        Console.WriteLine("Option not found. Valid options are \"stats\" and \"advice\".");
                        continue;
                }
            }
        }
    }
}