using Shouldly;
using Tankbuch.Cli.Infrastructure;

namespace Tankbuch.UnitTests;

// Toleranter Zahlen-Parser der CLI (Fmt.Dec) – insbesondere die Tausenderpunkt-Regression:
// "54.696" (de-Gruppierung ohne Komma) muss 54696 ergeben, nicht 54,696.
public class CliFmtTests
{
    [Theory]
    [InlineData("45,50", 45.50)]
    [InlineData("1.234,56", 1234.56)]
    [InlineData("45.50", 45.50)]
    [InlineData("1,589", 1.589)]
    [InlineData("54.696", 54696)]      // Tausenderpunkt ohne Komma → Gruppierung
    [InlineData("1.234.567", 1234567)]
    [InlineData("48230", 48230)]
    [InlineData("54\u00A0696", 54696)] // Gruppierung mit geschütztem Leerzeichen (de-AT, neuere ICU)
    [InlineData("54\u202F696", 54696)] // … bzw. schmalem geschützten Leerzeichen
    public void Dec_parses_german_and_invariant_numbers(string input, decimal expected)
        => Fmt.Dec(input).ShouldBe(expected);

    [Fact]
    public void Dec_roundtrips_formatted_kilometre_reading()
        => Fmt.Dec(Fmt.N(54696L)).ShouldBe(54696m); // fmt → parse muss verlustfrei sein

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("abc")]
    public void Dec_returns_null_for_invalid_input(string? input)
        => Fmt.Dec(input).ShouldBeNull();

    [Fact]
    public void Long_rounds_grouped_kilometre_reading()
        => Fmt.Long("54.696").ShouldBe(54696L);
}
