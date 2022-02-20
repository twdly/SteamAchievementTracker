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
        public decimal gamePercentage { get; set; }
        public int sortScore { get; set; }

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
                    gameStats.TotalAchievements = personalAchievements.Data.Achievements.Count;
                    var count = 0;
                    foreach (var achievement in personalAchievements.Data.Achievements)
                    {
                        if (achievement.Achieved == 1)
                        {
                            count++;
                        }
                    }
                    gameStats.OwnedAchievements = count;
                    try
                    {
                        gameStats.gamePercentage = (decimal)gameStats.OwnedAchievements / (decimal)gameStats.TotalAchievements * 100;
                    }
                    catch (System.DivideByZeroException)
                    {
                        gameStats.gamePercentage = 0;
                    }
                    gameAchievements.Add(gameStats);
                }
            }
            CalculateAccountStats(gameAchievements);
        }

        public static void CalculateAccountStats(List<GameAchievements> gameAchievements)
        {
            var gamePercentages = new List<decimal>();
            decimal totalPercentage = 0;
            var earnedAchievementTotal = 0;
            var achievementTotal = 0;
            foreach (var gameAndAchievements in gameAchievements)
            {
                earnedAchievementTotal += gameAndAchievements.OwnedAchievements;
                achievementTotal += gameAndAchievements.TotalAchievements;
                totalPercentage += gameAndAchievements.gamePercentage;
            }
            var eligibleGames = gameAchievements.Count(game => game.OwnedAchievements != 0 && game.TotalAchievements != 0);
            var averagePercentage = totalPercentage / eligibleGames;
            var roundedPercentage = Math.Round(averagePercentage, 2);
            Console.WriteLine($"This user has {earnedAchievementTotal} achievements out of a possible {achievementTotal} achievements");
            Console.WriteLine($"This comes to an average completion percentage of {roundedPercentage}");

            SelectAlgorithm(gameAchievements);
        }

        private static void SelectAlgorithm(List<GameAchievements> gameAchievements)
        {
            while (true)
            {
                var input = Program.prompt("How would you like your achievements analysed?");
                switch (input.ToLower())
                {
                    case "unowned":
                        findClosetoCompletion(gameAchievements);
                        return;
                    default:
                        Console.WriteLine("Invalid selection. Please type \"help\" for a list of valid selections.");
                        continue;
                }
            }
        }

        private static void findClosetoCompletion(List<GameAchievements> gameAchievements)
        {
            // Recommend games based on the number of unowned achievements.
            foreach (var game in gameAchievements)
            {
                game.sortScore = game.TotalAchievements - game.OwnedAchievements;
            }
            gameAchievements.RemoveAll(game => game.sortScore == 0);
            gameAchievements = gameAchievements.OrderBy(game => game.sortScore).ToList();
            var showAchievementsCount = 5;
            var count = 0;
            Console.WriteLine("Based on the number of unowned achievements, the following games are close to completion:");
            while (count != showAchievementsCount)
            {
                Console.WriteLine($"{gameAchievements[count].Name} has {gameAchievements[count].sortScore} unowned achievements.");
                count++;
            }
        }
    }
}