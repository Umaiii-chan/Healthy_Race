using HealthyRace.Collectibles;
using UnityEditor;
using UnityEngine;

namespace HealthyRace.Tests.EditMode
{
    public static class FoodAssetGenerator
    {
        public struct FoodDefinition
        {
            public string AssetName;
            public string Id;
            public string DisplayName;
            public FoodCategory Category;
            public int BasePoints;
            public int WellbeingDelta;
            public string SpriteName;

            public FoodDefinition(
                string assetName,
                string id,
                string displayName,
                FoodCategory category,
                int basePoints,
                int wellbeingDelta,
                string spriteName
            )
            {
                AssetName = assetName;
                Id = id;
                DisplayName = displayName;
                Category = category;
                BasePoints = basePoints;
                WellbeingDelta = wellbeingDelta;
                SpriteName = spriteName;
            }
        }

        public static readonly FoodDefinition[] Definitions = new[]
        {
            // 10 Healthy foods
            new FoodDefinition(
                "SO_Food_Apple",
                "apple",
                "Apple",
                FoodCategory.Healthy,
                100,
                10,
                "spr_food_apple.png"
            ),
            new FoodDefinition(
                "SO_Food_Banana",
                "banana",
                "Banana",
                FoodCategory.Healthy,
                80,
                8,
                "spr_food_banana.png"
            ),
            new FoodDefinition(
                "SO_Food_Orange",
                "orange",
                "Orange",
                FoodCategory.Healthy,
                100,
                10,
                "spr_food_orange.png"
            ),
            new FoodDefinition(
                "SO_Food_Carrot",
                "carrot",
                "Carrot",
                FoodCategory.Healthy,
                120,
                12,
                "spr_food_carrot.png"
            ),
            new FoodDefinition(
                "SO_Food_Broccoli",
                "broccoli",
                "Broccoli",
                FoodCategory.Healthy,
                150,
                15,
                "spr_food_broccoli.png"
            ),
            new FoodDefinition(
                "SO_Food_Strawberry",
                "strawberry",
                "Strawberry",
                FoodCategory.Healthy,
                80,
                8,
                "spr_food_strawberry.png"
            ),
            new FoodDefinition(
                "SO_Food_Watermelon",
                "watermelon",
                "Watermelon",
                FoodCategory.Healthy,
                100,
                10,
                "spr_food_watermelon.png"
            ),
            new FoodDefinition(
                "SO_Food_Avocado",
                "avocado",
                "Avocado",
                FoodCategory.Healthy,
                140,
                14,
                "spr_food_avocado.png"
            ),
            new FoodDefinition(
                "SO_Food_Fish",
                "fish",
                "Fish",
                FoodCategory.Healthy,
                160,
                16,
                "spr_food_fish.png"
            ),
            new FoodDefinition(
                "SO_Food_Water",
                "water",
                "Water",
                FoodCategory.Healthy,
                150,
                15,
                "spr_food_water.png"
            ),
            // 8 Ultra-Processed foods
            new FoodDefinition(
                "SO_Food_Burger",
                "burger",
                "Burger",
                FoodCategory.UltraProcessed,
                0,
                -18,
                "spr_food_burger.png"
            ),
            new FoodDefinition(
                "SO_Food_Soda",
                "soda",
                "Soda",
                FoodCategory.UltraProcessed,
                0,
                -12,
                "spr_food_soda.png"
            ),
            new FoodDefinition(
                "SO_Food_Fries",
                "fries",
                "Fries",
                FoodCategory.UltraProcessed,
                0,
                -15,
                "spr_food_fries.png"
            ),
            new FoodDefinition(
                "SO_Food_Donut",
                "donut",
                "Donut",
                FoodCategory.UltraProcessed,
                0,
                -14,
                "spr_food_donut.png"
            ),
            new FoodDefinition(
                "SO_Food_HotDog",
                "hotdog",
                "Hot Dog",
                FoodCategory.UltraProcessed,
                0,
                -16,
                "spr_food_hotdog.png"
            ),
            new FoodDefinition(
                "SO_Food_Pizza",
                "pizza",
                "Pizza",
                FoodCategory.UltraProcessed,
                0,
                -18,
                "spr_food_pizza.png"
            ),
            new FoodDefinition(
                "SO_Food_Candy",
                "candy",
                "Candy",
                FoodCategory.UltraProcessed,
                0,
                -10,
                "spr_food_candy.png"
            ),
            new FoodDefinition(
                "SO_Food_Chips",
                "chips",
                "Chips",
                FoodCategory.UltraProcessed,
                0,
                -12,
                "spr_food_chips.png"
            ),
        };

        public static void GenerateAllFoodAssets()
        {
            const string artFolder = "Assets/_Game/Art/Collectibles";
            const string foodFolder = "Assets/_Game/ScriptableObjects/Food";

            foreach (var def in Definitions)
            {
                var spritePath = $"{artFolder}/{def.SpriteName}";
                var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
                if (importer != null)
                {
                    bool changed = false;
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        changed = true;
                    }
                    if (importer.spriteImportMode != SpriteImportMode.Single)
                    {
                        importer.spriteImportMode = SpriteImportMode.Single;
                        changed = true;
                    }
                    if (importer.spritePixelsPerUnit != 32)
                    {
                        importer.spritePixelsPerUnit = 32;
                        changed = true;
                    }
                    if (importer.filterMode != FilterMode.Point)
                    {
                        importer.filterMode = FilterMode.Point;
                        changed = true;
                    }
                    if (changed)
                    {
                        importer.SaveAndReimport();
                    }
                }

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite == null)
                {
                    Debug.LogWarning($"Could not load sprite at {spritePath}");
                }

                var assetPath = $"{foodFolder}/{def.AssetName}.asset";
                var food = AssetDatabase.LoadAssetAtPath<FoodData>(assetPath);
                if (food == null)
                {
                    food = ScriptableObject.CreateInstance<FoodData>();
                    AssetDatabase.CreateAsset(food, assetPath);
                }

                var so = new SerializedObject(food);
                so.FindProperty("_id").stringValue = def.Id;
                so.FindProperty("_displayName").stringValue = def.DisplayName;
                so.FindProperty("_category").enumValueIndex = (int)def.Category;
                so.FindProperty("_basePoints").intValue = def.BasePoints;
                so.FindProperty("_wellbeingDelta").intValue = def.WellbeingDelta;
                so.FindProperty("_sprite").objectReferenceValue = sprite;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(food);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Successfully configured {Definitions.Length} food assets.");
        }
    }
}
