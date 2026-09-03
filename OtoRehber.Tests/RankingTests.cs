using System.Collections.Generic;
using System.Linq;
using OtoRehber.Domain.Ranking;
using Xunit;

namespace OtoRehber.Tests;

/// <summary>Session 3 diversity / presentation ranking (PRD v5 §3.1–3.3) birim testleri.</summary>
public class RankingTests
{
    [Theory]
    [InlineData("Toyota", "Corolla (E170, 2013-2016)", "toyota|corolla")]
    [InlineData("Toyota", "Corolla (E120, 2002-2007)", "toyota|corolla")]
    [InlineData("Volkswagen", "Golf (7, 2012-2016)", "volkswagen|golf")]
    [InlineData("Volkswagen", "Golf", "volkswagen|golf")]
    [InlineData("BMW", "520d (F10, 2010-2013)", "bmw|520d")]
    public void MainModel_KeyIgnoresGenerationAndEngine(string brand, string model, string expected)
        => Assert.Equal(expected, MainModel.Key(brand, model));

    [Fact]
    public void MainModel_DifferentModels_DifferentKeys()
        => Assert.NotEqual(MainModel.Key("Renault", "Clio (5, 2019-)"), MainModel.Key("Renault", "Symbol (2, 2013-2019)"));

    private static List<string> Presentation(IEnumerable<string> canonical, int max)
        => DiversityRanker.Presentation(canonical.ToList(), s => s.Split('#')[0], max);

    [Fact]
    public void Diversity_CapsSameMainModel_DefersRestToEnd()
    {
        // canonical sıra: skora göre — hepsi "corolla" hariç birkaç farklı model
        var canonical = new[]
        {
            "corolla#E170", "corolla#E150", "corolla#E120",  // aynı ana model x3
            "golf#7", "civic#FD6", "focus#Mk3"
        };
        var result = Presentation(canonical, 2);

        // İlk 4'te en fazla 2 corolla; 3. corolla sona ötelenmiş
        Assert.Equal(new[] { "corolla#E170", "corolla#E150", "golf#7", "civic#FD6", "focus#Mk3", "corolla#E120" }, result);
    }

    [Fact]
    public void Diversity_NothingDropped()
    {
        var canonical = Enumerable.Range(0, 10).Select(i => "x#" + i).ToArray();
        var result = Presentation(canonical, 2);
        Assert.Equal(10, result.Count);
        Assert.Equal(canonical.OrderBy(x => x), result.OrderBy(x => x));
    }

    [Fact]
    public void Diversity_ZeroOrNegative_IsNoOp()
    {
        var canonical = new[] { "a#1", "a#2", "a#3" };
        Assert.Equal(canonical, Presentation(canonical, 0));
        Assert.Equal(canonical, Presentation(canonical, -1));
    }

    [Fact]
    public void Diversity_IsDeterministic()
    {
        var canonical = new[] { "a#1", "b#1", "a#2", "a#3", "b#2", "c#1" };
        Assert.Equal(Presentation(canonical, 2), Presentation(canonical, 2));
    }

    [Fact]
    public void Diversity_DifferentGenerationsSurviveWithinCap()
    {
        // §3.1 önemli sınır: farklı nesiller birleştirilmez, cap içinde ikisi de görünür
        var canonical = new[] { "golf#1.6TDI", "golf#2.0TDI", "golf#GTI" };
        var result = Presentation(canonical, 2);
        Assert.Equal(2, result.Take(2).Count(x => x.StartsWith("golf")));
        Assert.Contains("golf#GTI", result); // kaybolmadı
    }
}
