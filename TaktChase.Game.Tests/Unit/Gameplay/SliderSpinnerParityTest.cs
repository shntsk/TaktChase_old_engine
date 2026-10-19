using NUnit.Framework;
using osuTK;
using TaktChase.Game.Gameplay.Beatmaps;

namespace TaktChase.Game.Tests.Unit.Gameplay;

[TestFixture]
public class SliderSpinnerParityTest
{
    [Test]
    public void TestSliderHasExpectedBasicGeometry()
    {
        var slider = new Slider(1000, new Vector2(10, 10), new Vector2(200, 50), duration: 500, repeatCount: 2);

        Assert.That(slider.StartTime, Is.EqualTo(1000));
        Assert.That(slider.Duration, Is.EqualTo(500));
        Assert.That(slider.RepeatCount, Is.EqualTo(2));
        Assert.That(slider.EndPosition, Is.EqualTo(new Vector2(200, 50)));
    }

    [Test]
    public void TestSpinnerRejectsInvalidValues()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new Spinner(1000, new Vector2(50, 50), -1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new Spinner(1000, new Vector2(50, 50), 1000, 0));
    }

    [Test]
    public void TestSliderContainsTheStartOrEndPositionWithinRadius()
    {
        var difficulty = new BeatmapDifficulty(overallDifficulty: 5, approachRate: 5, circleSize: 5);
        var slider = new Slider(1000, new Vector2(32, 32), new Vector2(64, 32), 500);

        Assert.That(slider.Contains(new Vector2(40, 32), difficulty), Is.True);
        Assert.That(slider.Contains(new Vector2(64, 32), difficulty), Is.True);
    }
}
