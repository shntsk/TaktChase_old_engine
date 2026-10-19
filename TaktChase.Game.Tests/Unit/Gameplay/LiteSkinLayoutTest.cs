using NUnit.Framework;
using TaktChase.Game.Gameplay.Skin;

namespace TaktChase.Game.Tests.Unit.Gameplay;

[TestFixture]
public class LiteSkinLayoutTest
{
    [Test]
    public void TestScaleMakesVisibleRingMatchDiameter()
        => Assert.That(LiteSkinLayout.ScaleForDiameter(236f) * LiteSkinLayout.HIT_CIRCLE_VISIBLE_DIAMETER, Is.EqualTo(236f).Within(1e-4));

    [Test]
    public void TestSingleDigitIsCentred()
    {
        var placements = LiteSkinLayout.LayoutNumber(7);
        Assert.That(placements, Has.Count.EqualTo(1));
        Assert.That(placements[0].Digit, Is.EqualTo(7));
        Assert.That(placements[0].CentreX, Is.EqualTo(0).Within(1e-4));
    }

    [Test]
    public void TestTwoDigitsOverlapAndAreSymmetric()
    {
        var placements = LiteSkinLayout.LayoutNumber(12);
        Assert.That(placements, Has.Count.EqualTo(2));
        Assert.That(placements[0].CentreX + placements[1].CentreX, Is.EqualTo(0).Within(1e-4));

        float distance = placements[1].CentreX - placements[0].CentreX;
        float expected = (placements[0].Width + placements[1].Width) / 2 - LiteSkinLayout.DIGIT_OVERLAP;
        Assert.That(distance, Is.EqualTo(expected).Within(1e-4));
    }

    [Test]
    public void TestZeroIsLaidOutAsOneDigit()
        => Assert.That(LiteSkinLayout.LayoutNumber(0)[0].Digit, Is.EqualTo(0));

    [Test]
    public void TestWideNumbersAreScaledDownToFit()
    {
        Assert.That(LiteSkinLayout.NumberFitScale(5), Is.EqualTo(1f));
        float scale = LiteSkinLayout.NumberFitScale(1234);
        Assert.That(scale, Is.LessThan(1f));
        Assert.That(LiteSkinLayout.NumberWidth(1234) * scale,
            Is.LessThanOrEqualTo(LiteSkinLayout.HIT_CIRCLE_VISIBLE_DIAMETER * LiteSkinLayout.MAX_NUMBER_WIDTH_FRACTION + 1e-3));
    }

    [TestCase(-1)]
    public void TestNegativeNumbersAreRejected(int number)
        => Assert.Throws<System.ArgumentOutOfRangeException>(() => LiteSkinLayout.LayoutNumber(number));

    [TestCase(10)]
    [TestCase(-1)]
    public void TestInvalidDigitsAreRejected(int digit)
        => Assert.Throws<System.ArgumentOutOfRangeException>(() => LiteSkinLayout.DigitTexture(digit));
}
