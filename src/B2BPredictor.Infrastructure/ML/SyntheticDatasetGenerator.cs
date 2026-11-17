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
            var creditRating = random.Next(1, 101); // Шкала 1-100 згідно зі спільними контрактами
            var paymentTermsDays = random.Next(0, 91); // від 0 до 90 днів відстрочки
            var itemsCount = random.Next(1, 100);
            var previousOrders = random.Next(0, 25);
            var amount = (float)Math.Round(random.NextDouble() * 49500 + 500, 2);

            // Реалістичний розрахунок ймовірності успіху під шкалу рейтингу 1-100
            double score = 0.5;
            score += (creditRating - 50) / 100.0; // рейтинг > 50 підвищує шанс, < 50 знижує
            score -= (paymentTermsDays - 30) / 120.0; // велика відстрочка знижує шанс
            score += Math.Min(previousOrders * 0.02, 0.25); // постійні клієнти надійніші
            if (amount > 35000 && creditRating < 45) score -= 0.25; // велика сума + низький рейтинг = ризик
            if (industry is "IT" or "Healthcare") score += 0.08;
            if (industry is "Construction") score -= 0.08;

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