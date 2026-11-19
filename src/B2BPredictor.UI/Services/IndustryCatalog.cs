namespace B2BPredictor.UI.Services;

/// <summary>
/// Галузі, на яких навчена ML-модель (SyntheticDatasetGenerator) та які є в демо-даних (DatabaseSeeder).
/// </summary>
public static class IndustryCatalog
{
    public static IReadOnlyList<string> All { get; } =
    [
        "IT",
        "Retail",
        "Manufacturing",
        "Logistics",
        "Healthcare",
        "Construction",
        "Agriculture"
    ];
}
