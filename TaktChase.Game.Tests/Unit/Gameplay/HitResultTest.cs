using NUnit.Framework;
using TaktChase.Game.Gameplay.Judgements;

namespace TaktChase.Game.Tests.Unit.Gameplay;

[TestFixture]
public class HitResultTest
{
    [TestCase(HitResult.Great, 300, "300", true)]
    [TestCase(HitResult.Ok, 100, "100", true)]
    [TestCase(HitResult.Meh, 50, "50", true)]
    [TestCase(HitResult.Miss, 0, "Miss", false)]
    public void TestResultProperties(HitResult result, int points, string name, bool isHit)
    {
        Assert.That(result.BasePoints(), Is.EqualTo(points));
        Assert.That(result.DisplayName(), Is.EqualTo(name));
        Assert.That(result.IsHit(), Is.EqualTo(isHit));
    }

    [Test]
    public void TestQualityOrder()
    {
        Assert.That(HitResult.Miss, Is.LessThan(HitResult.Meh));
        Assert.That(HitResult.Meh, Is.LessThan(HitResult.Ok));
        Assert.That(HitResult.Ok, Is.LessThan(HitResult.Great));
    }

    [Test]
    public void TestMaxBasePointsIsTheBestResult()
        => Assert.That(HitResultExtensions.MAX_BASE_POINTS, Is.EqualTo(HitResult.Great.BasePoints()));
}
