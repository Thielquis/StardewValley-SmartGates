using Microsoft.Xna.Framework;

namespace SmartGates.Framework {
    public static class Utils {
        public static readonly Vector2[] FacingOffsets = new Vector2[]{
            new Vector2(0, -1), // Up
            new Vector2(1, 0),  // Right
            new Vector2(0, 1),  // Down
            new Vector2(-1, 0)  // Left
        };
    }
}
