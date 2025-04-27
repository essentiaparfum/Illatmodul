using System;
using System.Collections.Generic;
using Xunit;

namespace Illamdul.Dnn.Illatmodul.Tests.Controllers
{
    public class ItemControllerTests
    {
        [Fact]
        public void Eredmeny_WithMappedAnswers_ReturnsTopTwoCategories()
        {
            // Arrange
            var answers = new List<string>
            {
                "Virágos",
                "Romantikus",
                "Édes (vanília, karamell)",
                "Valami a kettő között lenne jó",
                "Fontos, hogy egész nap tartson"
            };

            var map = new Dictionary<string, string[]>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "Friss, citrusos", new[] { "Friss illatok" } },
                { "Virágos", new[] { "Virágos illatok" } },
                { "Édes (vanília, karamell)", new[] { "Édes illatok" } },
                { "Meleg, fűszeres", new[] { "Fűszeres illatok" } },
                { "Sportos", new[] { "Friss illatok", "Fás illatok" } },
                { "Elegáns", new[] { "Púderes illatok", "Orientális illatok" } },
            };

            var categoryScores = new Dictionary<string, int>();

            foreach (var answer in answers)
            {
                foreach (var kvp in map)
                {
                    if (kvp.Key.Contains(answer, StringComparison.InvariantCultureIgnoreCase))
                    {
                        foreach (var cat in kvp.Value)
                        {
                            if (!categoryScores.ContainsKey(cat))
                                categoryScores[cat] = 0;
                            categoryScores[cat]++;
                        }
                    }
                }
            }

            var topCategories = new List<string>(categoryScores.Keys);
            topCategories.Sort((a, b) => categoryScores[b].CompareTo(categoryScores[a]));

            var top1 = topCategories.Count > 0 ? topCategories[0] : null;
            var top2 = topCategories.Count > 1 ? topCategories[1] : null;

            // Assert
            // ÚJ ELVÁRÁS:
            Assert.Equal("Virágos illatok", top1);
            Assert.Equal("Édes illatok", top2);
        }
    }
}
