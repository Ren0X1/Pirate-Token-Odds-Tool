namespace ChestCounter;

/// <summary>
/// The chance of having obtained the Pirate Shop Access token at least once with the chests opened so far.
/// Each chest drops it with the same odds (1 in 137,500 by default, from content.json); what grows with every chest
/// is the cumulative chance: P = 1 − (1 − 1/N)^chests.
/// </summary>
public static class PirateOdds
{
    /// <summary>Per-chest odds used when content.json has no "pirate-shop" entry.</summary>
    public const int DefaultOneIn = 137_500;

    public static int OneIn => TreasureCatalog.Odds.FirstOrDefault(o => o.Kind == TreasureCatalog.PirateShop)?.OneIn ?? DefaultOneIn;

    public sealed record Result(long Chests, int OneIn, double Chance, long HalfAt, long ToHalf, double NextMilestone, long ToNextMilestone);

    /// <summary>Milestones shown as "next goal" (cumulative chance).</summary>
    private static readonly double[] Milestones = { 0.01, 0.025, 0.05, 0.10, 0.25, 0.50, 0.75, 0.90, 0.95, 0.99 };

    public static double ChanceFor(long chests) => TreasureCatalog.AtLeastOnce(OneIn, chests);

    public static Result For(long chests)
    {
        chests = Math.Max(0, chests);
        var oneIn = OneIn;
        var p = TreasureCatalog.AtLeastOnce(oneIn, chests);
        var half = TreasureCatalog.ChestsFor(oneIn, 0.5);
        var next = Milestones.FirstOrDefault(m => m > p + 1e-12, 0.999);
        var toNext = Math.Max(0, TreasureCatalog.ChestsFor(oneIn, next) - chests);
        return new Result(chests, oneIn, p, half, Math.Max(0, half - chests), next, toNext);
    }

    /// <summary>"0.073 %", "1.4 %", "27.5 %": enough decimals for the number to move with every few chests.</summary>
    public static string Percent(double p)
    {
        var v = p * 100;
        var decimals = v >= 10 ? 2 : v >= 1 ? 3 : 4;
        var text = v.ToString("F" + decimals, L.Culture);
        return L.Code == "en" ? text + "%" : text + " %";
    }

    /// <summary>Short form for the overlay: "0.07%", "1.45%", "27.5%".</summary>
    public static string PercentShort(double p)
    {
        var v = p * 100;
        var decimals = v >= 10 ? 1 : v >= 1 ? 2 : 3;
        var text = v.ToString("F" + decimals, L.Culture);
        return L.Code == "en" ? text + "%" : text + " %";
    }

    /// <summary>Milestone label: "1 %", "2.5 %", "50 %".</summary>
    public static string Milestone(double m)
    {
        var text = (m * 100).ToString(m * 100 % 1 == 0 ? "0" : "0.#", L.Culture);
        return L.Code == "en" ? text + "%" : text + " %";
    }
}
