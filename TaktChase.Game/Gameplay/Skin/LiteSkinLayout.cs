using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;
using TaktChase.Game.Gameplay.Judgements;

namespace TaktChase.Game.Gameplay.Skin;

/// <summary>One digit of a hit-circle number, positioned relative to the centre of the whole number.</summary>
public readonly record struct DigitPlacement(int Digit, float CentreX, float Width);

/// <summary>
/// Geometry of the std::lite hit-circle artwork. The PNGs are 2x rasterisations of the original SVGs; every size here is in the
/// SVG's own units, so the PNG resolution never leaks into gameplay. All functions are pure and covered by unit tests.
/// </summary>
public static class LiteSkinLayout
{
    /// <summary>Texture store names (under Textures/) of the packaged artwork.</summary>
    public const string HIT_CIRCLE_TEXTURE = "Skin/lite/hitcircle";

    public const string APPROACH_CIRCLE_TEXTURE = "Skin/lite/approachcircle";

    public const string CURSOR_TEXTURE = "Skin/lite/cursor";

    public const string CURSOR_TRAIL_TEXTURE = "Skin/lite/cursortrail";

    public static string DigitTexture(int digit)
    {
        if (digit < 0 || digit > 9)
            throw new ArgumentOutOfRangeException(nameof(digit), digit, "A digit must be between 0 and 9.");

        return $"Skin/lite/default-{digit}";
    }

    public static string JudgementTexture(HitResult result) => result switch
    {
        HitResult.Great => "Skin/lite/judgement-green",
        HitResult.Ok => "Skin/lite/judgement-yellow",
        HitResult.Meh => "Skin/lite/judgement-yellow",
        _ => "Skin/lite/judgement-miss",
    };

    /// <summary>Size of hitcircle.svg including its shadow margin.</summary>
    public const float HIT_CIRCLE_IMAGE_SIZE = 300f;

    /// <summary>Diameter of the visible circle (outer edge of the white ring) inside hitcircle.svg.</summary>
    public const float HIT_CIRCLE_VISIBLE_DIAMETER = 236f;

    /// <summary>Size of approachcircle.svg.</summary>
    public const float APPROACH_IMAGE_SIZE = 264f;

    /// <summary>The skin's HitCircleOverlap (numerals.ini): how far consecutive digits overlap, in SVG units.</summary>
    public const float DIGIT_OVERLAP = 7f;

    /// <summary>Height of every numeral image in SVG units.</summary>
    public const float DIGIT_IMAGE_HEIGHT = 96f;

    /// <summary>Fraction of the circle diameter a number may occupy before it is scaled down.</summary>
    public const float MAX_NUMBER_WIDTH_FRACTION = 0.7f;

    /// <summary>Width of each numeral image in SVG units (the "1" is narrower than the others).</summary>
    public static float DigitImageWidth(int digit) => digit == 1 ? 90f : 96f;

    /// <summary>Scale that makes the visible ring of hitcircle.svg exactly <paramref name="diameter"/> wide, so it matches the hit radius.</summary>
    public static float ScaleForDiameter(float diameter)
    {
        if (diameter <= 0)
            throw new ArgumentOutOfRangeException(nameof(diameter), diameter, "The diameter must be positive.");

        return diameter / HIT_CIRCLE_VISIBLE_DIAMETER;
    }

    /// <summary>Lays out the digits of <paramref name="number"/> (>= 0) in SVG units, centred on x = 0, with the skin's digit overlap.</summary>
    public static IReadOnlyList<DigitPlacement> LayoutNumber(int number)
    {
        if (number < 0)
            throw new ArgumentOutOfRangeException(nameof(number), number, "The number must not be negative.");

        string text = number.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var placements = new List<DigitPlacement>(text.Length);

        float total = 0;
        for (int i = 0; i < text.Length; i++)
            total += DigitImageWidth(text[i] - '0') - (i > 0 ? DIGIT_OVERLAP : 0);

        float x = -total / 2;

        for (int i = 0; i < text.Length; i++)
        {
            int digit = text[i] - '0';
            float width = DigitImageWidth(digit);
            if (i > 0)
                x -= DIGIT_OVERLAP;

            placements.Add(new DigitPlacement(digit, x + width / 2, width));
            x += width;
        }

        return placements;
    }

    /// <summary>Total width of the laid-out number in SVG units.</summary>
    public static float NumberWidth(int number)
    {
        var placements = LayoutNumber(number);
        var first = placements[0];
        var last = placements[placements.Count - 1];
        return last.CentreX + last.Width / 2 - (first.CentreX - first.Width / 2);
    }

    /// <summary>Extra scale (<= 1) applied to a number so wide numbers stay inside the circle.</summary>
    public static float NumberFitScale(int number)
    {
        float allowed = HIT_CIRCLE_VISIBLE_DIAMETER * MAX_NUMBER_WIDTH_FRACTION;
        float width = NumberWidth(number);
        return width <= allowed ? 1f : allowed / width;
    }
}
