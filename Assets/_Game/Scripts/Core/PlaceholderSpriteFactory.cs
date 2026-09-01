using UnityEngine;

namespace HealthyRace.Core
{
    /// <summary>
    /// Creates the temporary sprites used by the first playable slice. The sprites are
    /// generated at runtime so placeholder art never becomes a production dependency.
    /// </summary>
    public static class PlaceholderSpriteFactory
    {
        private const int TextureSize = 64;

        private static Sprite _circleSprite;
        private static Sprite _squareSprite;

        public static Sprite Circle => _circleSprite ??= CreateCircleSprite();

        public static Sprite Square => _squareSprite ??= CreateSquareSprite();

        private static Sprite CreateCircleSprite()
        {
            var texture = CreateTexture("PlaceholderCircle");
            var pixels = new Color32[TextureSize * TextureSize];
            var radius = TextureSize * 0.5f - 1f;
            var center = (TextureSize - 1) * 0.5f;

            for (var y = 0; y < TextureSize; y++)
            {
                for (var x = 0; x < TextureSize; x++)
                {
                    var distance = new Vector2(x - center, y - center).magnitude;
                    pixels[y * TextureSize + x] =
                        distance <= radius
                            ? new Color32(255, 255, 255, 255)
                            : new Color32(255, 255, 255, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f),
                TextureSize
            );
            sprite.name = "PlaceholderCircle";
            return sprite;
        }

        private static Sprite CreateSquareSprite()
        {
            var texture = CreateTexture("PlaceholderSquare");
            var pixels = new Color32[TextureSize * TextureSize];

            for (var index = 0; index < pixels.Length; index++)
            {
                pixels[index] = new Color32(255, 255, 255, 255);
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f),
                TextureSize
            );
            sprite.name = "PlaceholderSquare";
            return sprite;
        }

        private static Texture2D CreateTexture(string name)
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            return texture;
        }
    }
}
