using Microsoft.Xna.Framework;

namespace Vodyanitsa.Common.Swimming;

/// <summary>Deterministic movement rules shared by the game and isolated native QA.</summary>
public static class SwimPhysics
{
    public const float DefaultSpeed = 5.5f;
    public const float DefaultAcceleration = 0.35f;
    public const float DefaultDrag = 0.80f;
    public const int VisualGraceTicks = 10;

    public static bool IsOrdinaryWater(bool wet, bool lavaWet, bool honeyWet, bool shimmerWet)
        => wet && !lavaWet && !honeyWet && !shimmerWet;

    public static Vector2 GetInput(bool left, bool right, bool up, bool down, bool jump)
    {
        var input = new Vector2((right ? 1 : 0) - (left ? 1 : 0), (down ? 1 : 0) - (up || jump ? 1 : 0));
        if (input.LengthSquared() > 1f) input.Normalize();
        return input;
    }

    public static Vector2 StepVelocity(Vector2 previous, Vector2 input, float speed = DefaultSpeed,
        float acceleration = DefaultAcceleration, float drag = DefaultDrag)
    {
        if (input.LengthSquared() > 1f) input.Normalize();
        if (input.LengthSquared() < 0.0001f) {
            Vector2 slowed = previous * drag;
            return slowed.LengthSquared() < 0.0025f ? Vector2.Zero : slowed;
        }
        Vector2 delta = input * speed - previous;
        float distance = delta.Length();
        return distance <= acceleration ? input * speed : previous + delta * (acceleration / distance);
    }
}
