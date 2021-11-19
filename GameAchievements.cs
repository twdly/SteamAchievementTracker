using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System.Collections.Generic;
using Steam.Models.SteamPlayer;
using Steam.Models.SteamCommunity;
using System.Linq;

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
                    continue;
                }
                if (achievements.ContentLength != 0)
                {
                    var achievementCount = 0;
                    foreach (var achievement in achievements.Data)
                    {
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
                    var ownedAchievements = personalAchievements.Data.Achievements.Count();
                    gameStats.OwnedAchievements = ownedAchievements;
                    gameAchievements.Add(gameStats);
                }
            }
            getAchievementInput(gameAchievements);
        }

        private static void getAchievementInput(List<GameAchievements> gameAchievements)
        {
            while (true)
            {
                string input = Program.prompt("What would you like about your achievements?");
                switch (input)
                {
                    case "stats":
                        CalculateAccountStats(gameAchievements);
                        return;
                    case "advice":
                        Console.WriteLine("Vex y and z");
                        return;
                    default:
                        Console.WriteLine("Option not found. Valid options are \"stats\" and \"advice\".");
                        continue;
                }
            }
        }

        public static void CalculateAccountStats(List<GameAchievements> gameAchievements)
        {
            var achievementTotal = 0;
            int earnedAchievementTotal = 0;
            List<Decimal> gamePercentages = new List<decimal>();
            foreach (var gameAndAchievements in gameAchievements)
            {
                earnedAchievementTotal += gameAndAchievements.OwnedAchievements;
                achievementTotal += gameAndAchievements.TotalAchievements;
                decimal gamePercentage = (earnedAchievementTotal / achievementTotal) * 100;
                gamePercentages.Add(gamePercentage);
            }
            decimal totalPercentage = gamePercentages.Sum();
            decimal averagePercentage = Math.Round((totalPercentage / gamePercentages.Count()) * 100, 2);
            Console.WriteLine($"Vexo has {earnedAchievementTotal} achievements out of a possible {achievementTotal} achievements");
            Console.WriteLine($"This comes to an average completion percentage of {averagePercentage}");
        }
    }
}