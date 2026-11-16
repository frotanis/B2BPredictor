namespace B2BPredictor.Infrastructure.ML;

public static class SyntheticDatasetGenerator
{
    private static readonly string[] Industries = ["IT", "Retail", "Manufacturing", "Logistics", "Healthcare", "Construction"];

    public static List<OrderInputData> Generate(int count = 1500, int seed = 42)
    {
        var random = new Random(seed);
        var dataset = new List<OrderInputData>(count);

        for (int i = 0; i < count; i++)
        {
            var industry = Industries[random.Next(Industries.Length)];
            var creditRating = random.Next(300, 851); // 300 - поганий, 850 - ідеальний
            var paymentTermsDays = random.Next(0, 91); // від 0 до 90 днів відстрочки
            var itemsCount = random.Next(1, 100);
            var previousOrders = random.Next(0, 25);
            var amount = (float)Math.Round(random.NextDouble() * 49500 + 500, 2); // від 500 до 50000

            // Логіка розрахунку ймовірності успіху для навчання моделі реалістичним патернам
            double score = 0.5;
            score += (creditRating - 550) / 600.0; // високий рейтинг підвищує шанс
            score -= (paymentTermsDays - 30) / 120.0; // велика відстрочка знижує шанс
            score += Math.Min(previousOrders * 0.02, 0.25); // постійні клієнти надійніші
            if (amount > 35000 && creditRating < 600) score -= 0.25; // велика сума + низький рейтинг = ризик
            if (industry is "IT" or "Healthcare") score += 0.08;
            if (industry is "Construction") score -= 0.08;

            // Додаємо трохи природного шуму
            score += (random.NextDouble() - 0.5) * 0.15;

            bool isSuccessful = score >= 0.5;

            dataset.Add(new OrderInputData
            {
                Amount = amount,
                Industry = industry,
                CreditRating = creditRating,
                PaymentTermsDays = paymentTermsDays,
                ItemsCount = itemsCount,
                PreviousSuccessfulOrders = previousOrders,
                IsSuccessful = isSuccessful
            });
        }

        return dataset;
    }
}