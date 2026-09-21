using HealthyRace.Audio;
using HealthyRace.Core;
using HealthyRace.Puzzles;
using UnityEngine;

namespace HealthyRace.Level
{
    public sealed partial class Phase1Bootstrap
    {
        private static readonly Color GroundGreen = new(0.12f, 0.48f, 0.36f);
        private static readonly Color PlatformGreen = new(0.16f, 0.63f, 0.47f);
        private static readonly Color StoneGreen = new(0.09f, 0.34f, 0.31f);

        // The redesign keeps the six teaching beats from the LDD, but composes them
        // as connected rooms with three vertical routes instead of six flat strips.
        private void CreateZone1_Spawn(Transform parent)
        {
            var zone = NewZone("Z1_Spawn", parent);

            CreateStaticBlock(
                "Z1_Floor",
                zone,
                new Vector2(12f, -3.4f),
                new Vector2(35f, 0.8f),
                GroundGreen
            );
            CreateStaticBlock(
                "Z1_LeftBoundary",
                zone,
                new Vector2(-5.5f, 0f),
                new Vector2(1f, 10f),
                StoneGreen,
                StaticBlockKind.Wall
            );
            CreateSceneryProp(
                "Z1_StartMarker",
                zone,
                new Vector2(0f, -2.55f),
                new Vector2(0.12f, 0.9f),
                PlaceholderSpriteFactory.Square,
                1
            );

            // A low, readable loop teaches jumps while always retaining a safe floor.
            CreateStaticBlock(
                "Z1_LowStep",
                zone,
                new Vector2(8f, -2.15f),
                new Vector2(4.5f, 0.45f),
                PlatformGreen,
                StaticBlockKind.Platform
            );
            CreateStaticBlock(
                "Z1_MiddleStep",
                zone,
                new Vector2(15f, -0.95f),
                new Vector2(5f, 0.45f),
                PlatformGreen,
                StaticBlockKind.Platform
            );
            CreateStaticBlock(
                "Z1_ReturnStep",
                zone,
                new Vector2(22f, -2.05f),
                new Vector2(4.5f, 0.45f),
                PlatformGreen,
                StaticBlockKind.Platform
            );

            SpawnFood("apple", new Vector2(8f, -1.35f), zone);
            SpawnFood("banana", new Vector2(15f, -0.15f), zone);
            SpawnFood("water", new Vector2(22f, -1.25f), zone);

            CreateWorldLabel("LARGADA", zone, new Vector2(0f, -1.7f), Color.white);
            CreateSceneryProp(
                "Z1_Landmark_StartTree",
                zone,
                new Vector2(27f, -0.9f),
                new Vector2(3.5f, 5.4f),
                GetScenerySprite(SceneryKind.Tree),
                -1
            );
            CreateSceneryProp(
                "Z1_Shrub",
                zone,
                new Vector2(4f, -2.55f),
                new Vector2(1.5f, 1.1f),
                GetScenerySprite(SceneryKind.Shrub)
            );
        }

        private void CreateZone2_Caixa(Transform parent)
        {
            var zone = NewZone("Z2_Caixa", parent);

            CreateStaticBlock(
                "Z2_LowerCourtyard",
                zone,
                new Vector2(39f, -3.4f),
                new Vector2(20f, 0.8f),
                GroundGreen
            );
            CreateStaticBlock(
                "Z2_UpperGarden",
                zone,
                new Vector2(54f, 0.1f),
                new Vector2(18f, 0.7f),
                GroundGreen
            );
            CreateStaticBlock(
                "Z2_GardenWall",
                zone,
                new Vector2(45.5f, -1.65f),
                new Vector2(1f, 2.9f),
                StoneGreen,
                StaticBlockKind.Wall
            );

            var box = CreatePushableBox("Z2_TeachingBox", zone, new Vector2(35f, -2.25f));
            var button = CreatePressureButtonObject(
                "Z2_GardenButton",
                zone,
                new Vector2(41f, -2.84f)
            );
            var lift = CreateActivatedPlatformObject(
                "Z2_GardenLift",
                zone,
                new Vector2(43.2f, -2.55f),
                new Vector2(3.6f, 0.45f),
                new Vector2(0f, 2.7f)
            );
            var reset = CreateLeverObject(
                "Z2_BoxReset",
                zone,
                new Vector2(31.5f, -2.35f),
                "RECUPERAR CAIXA",
                false
            );
            var introControllerObject = new GameObject("Z2_BoxLessonController");
            introControllerObject.transform.SetParent(zone, false);
            var introController = introControllerObject.AddComponent<CooperativePuzzleController>();
            introController.Bind(button, null, box, null, lift, reset, requireAll: false);

            SpawnFood("orange", new Vector2(48.5f, 1f), zone);
            SpawnFood("carrot", new Vector2(54f, 1f), zone);
            SpawnFood("broccoli", new Vector2(60f, 1f), zone);

            CreateSceneryProp(
                "Z2_Landmark_GreatTree",
                zone,
                new Vector2(58f, 2.15f),
                new Vector2(3.8f, 5.2f),
                GetScenerySprite(SceneryKind.Tree),
                -2
            );
            CreateWorldLabel(
                "JARDIM DA GRANDE ÁRVORE",
                zone,
                new Vector2(54f, 3.7f),
                new Color(0.95f, 1f, 0.72f)
            );
        }

        private void CreateZone3_Rotas(Transform parent)
        {
            var zone = NewZone("Z3_Rotas", parent);
            var checkpoint = CreateCheckpointObject(
                "Checkpoint_1",
                zone,
                1,
                new Vector2(65f, 0.8f)
            );
            _checkpoints.Add(checkpoint);

            CreateStaticBlock(
                "Z3_EntryHub",
                zone,
                new Vector2(66f, -0.1f),
                new Vector2(8f, 0.7f),
                GroundGreen
            );
            CreateStaticBlock(
                "Z3_UpperEntry",
                zone,
                new Vector2(69f, 1.65f),
                new Vector2(4.5f, 0.45f),
                PlatformGreen,
                StaticBlockKind.Platform
            );
            CreateStaticBlock(
                "Z3_LowerEntry",
                zone,
                new Vector2(69f, -2.7f),
                new Vector2(5f, 0.6f),
                GroundGreen
            );

            BuildUpperCanopyRoute(zone);
            BuildCentralBridgeRoute(zone);
            BuildLowerCreekRoute(zone);

            // Frequent connectors echo the reference without allowing extreme vertical separation.
            CreateStaticBlock(
                "Z3_ConnectorA_Low",
                zone,
                new Vector2(86f, -1.65f),
                new Vector2(3.5f, 0.4f),
                PlatformGreen,
                StaticBlockKind.Platform
            );
            CreateStaticBlock(
                "Z3_ConnectorA_High",
                zone,
                new Vector2(88.8f, 1.65f),
                new Vector2(3.5f, 0.4f),
                PlatformGreen,
                StaticBlockKind.Platform
            );
            CreateStaticBlock(
                "Z3_ConnectorB_Low",
                zone,
                new Vector2(111f, -1.55f),
                new Vector2(3.8f, 0.4f),
                PlatformGreen,
                StaticBlockKind.Platform
            );
            CreateStaticBlock(
                "Z3_ConnectorB_High",
                zone,
                new Vector2(113.5f, 1.55f),
                new Vector2(3.8f, 0.4f),
                PlatformGreen,
                StaticBlockKind.Platform
            );
            CreateStaticBlock(
                "Z3_ExitHub",
                zone,
                new Vector2(138f, 0f),
                new Vector2(9f, 0.8f),
                GroundGreen
            );

            BuildStoneArchLandmark(zone, new Vector2(91f, 0.15f));
            BuildSpringLandmark(zone, new Vector2(119f, -2.2f));
        }

        private void BuildUpperCanopyRoute(Transform zone)
        {
            CreateStaticBlock(
                "Upper_Canopy1",
                zone,
                new Vector2(76f, 2.8f),
                new Vector2(6f, 0.45f),
                PlatformGreen,
                StaticBlockKind.Platform
            );
            CreateStaticBlock(
                "Upper_Canopy2",
                zone,
                new Vector2(84f, 4f),
                new Vector2(6f, 0.45f),
                PlatformGreen,
                StaticBlockKind.Platform
            );
            var fallingA = CreateFallingPlatformObject(
                "Upper_FallingA",
                zone,
                new Vector2(92f, 3.35f),
                new Vector2(3.6f, 0.45f)
            );
            _fallingPlatforms.Add(fallingA);
            CreateStaticBlock(
                "Upper_Canopy3",
                zone,
                new Vector2(99.5f, 4.25f),
                new Vector2(7f, 0.45f),
                PlatformGreen,
                StaticBlockKind.Platform
            );
            CreateStaticBlock(
                "Upper_Canopy4",
                zone,
                new Vector2(109f, 3.05f),
                new Vector2(5.5f, 0.45f),
                PlatformGreen,
                StaticBlockKind.Platform
            );
            CreateStaticBlock(
                "Upper_Canopy5",
                zone,
                new Vector2(118f, 4.15f),
                new Vector2(6f, 0.45f),
                PlatformGreen,
                StaticBlockKind.Platform
            );
            var fallingB = CreateFallingPlatformObject(
                "Upper_FallingB",
                zone,
                new Vector2(126f, 3.1f),
                new Vector2(3.6f, 0.45f)
            );
            _fallingPlatforms.Add(fallingB);
            CreateStaticBlock(
                "Upper_CanopyExit",
                zone,
                new Vector2(134f, 2f),
                new Vector2(6f, 0.45f),
                PlatformGreen,
                StaticBlockKind.Platform
            );

            SpawnFood("strawberry", new Vector2(76f, 3.55f), zone);
            SpawnFood("watermelon", new Vector2(84f, 4.85f), zone);
            SpawnFood("avocado", new Vector2(99.5f, 5f), zone);
            SpawnFood("fish", new Vector2(118f, 4.9f), zone);
            SpawnFood("water", new Vector2(134f, 2.75f), zone);
        }

        private void BuildCentralBridgeRoute(Transform zone)
        {
            var positions = new[]
            {
                new Vector2(74f, -0.35f),
                new Vector2(83f, 0.45f),
                new Vector2(92f, -0.25f),
                new Vector2(101f, 0.55f),
                new Vector2(110f, -0.15f),
                new Vector2(119f, 0.55f),
                new Vector2(128f, -0.15f),
                new Vector2(136f, 0.35f),
            };
            for (var i = 0; i < positions.Length; i++)
            {
                CreateStaticBlock(
                    $"Central_Bridge{i + 1}",
                    zone,
                    positions[i],
                    new Vector2(7f, 0.5f),
                    PlatformGreen,
                    StaticBlockKind.Platform
                );
            }

            SpawnFood("banana", new Vector2(74f, 0.45f), zone);
            SpawnFood("donut", new Vector2(83f, 1.25f), zone);
            SpawnFood("apple", new Vector2(92f, 0.55f), zone);
            SpawnFood("pizza", new Vector2(101f, 1.35f), zone);
            SpawnFood("carrot", new Vector2(110f, 0.65f), zone);
            SpawnFood("orange", new Vector2(119f, 1.35f), zone);
            SpawnFood("water", new Vector2(136f, 1.15f), zone);
        }

        private void BuildLowerCreekRoute(Transform zone)
        {
            var segments = new[]
            {
                new Vector4(76f, -3.4f, 9f, 0.8f),
                new Vector4(88f, -3.4f, 11f, 0.8f),
                new Vector4(101f, -3.4f, 11f, 0.8f),
                new Vector4(115f, -3.4f, 12f, 0.8f),
                new Vector4(130f, -3.4f, 14f, 0.8f),
            };
            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                CreateStaticBlock(
                    $"Lower_CreekBank{i + 1}",
                    zone,
                    new Vector2(segment.x, segment.y),
                    new Vector2(segment.z, segment.w),
                    GroundGreen
                );
            }

            var mudA = CreateMudHazardObject(
                "Lower_MudA",
                zone,
                new Vector2(78f, -2.82f),
                new Vector2(4.5f, 0.35f)
            );
            var mudB = CreateMudHazardObject(
                "Lower_MudB",
                zone,
                new Vector2(116f, -2.82f),
                new Vector2(5f, 0.35f)
            );
            _mudHazards.Add(mudA);
            _mudHazards.Add(mudB);

            var spikeA = CreateSpikeHazardObject(
                "Lower_SpikesA",
                zone,
                new Vector2(94f, -2.72f),
                new Vector2(2.2f, 0.45f),
                5,
                new Vector2(65f, 1.3f)
            );
            var spikeB = CreateSpikeHazardObject(
                "Lower_SpikesB",
                zone,
                new Vector2(129f, -2.72f),
                new Vector2(2.2f, 0.45f),
                5,
                new Vector2(65f, 1.3f)
            );
            _spikes.Add(spikeA);
            _spikes.Add(spikeB);

            SpawnFood("burger", new Vector2(73f, -2.35f), zone);
            SpawnFood("soda", new Vector2(81f, -2.15f), zone);
            SpawnFood("fries", new Vector2(89f, -2.35f), zone);
            SpawnFood("broccoli", new Vector2(98f, -2.1f), zone);
            SpawnFood("hotdog", new Vector2(108f, -2.35f), zone);
            SpawnFood("chips", new Vector2(121f, -2.1f), zone);
            SpawnFood("candy", new Vector2(134f, -2.35f), zone);
        }

        private void CreateZone4_Reencontro(Transform parent)
        {
            var zone = NewZone("Z4_Reencontro", parent);

            CreateStaticBlock(
                "Z4_SafeGarden",
                zone,
                new Vector2(150f, -1.8f),
                new Vector2(20f, 0.8f),
                GroundGreen
            );
            CreateStaticBlock(
                "Z4_UpperReturn",
                zone,
                new Vector2(144f, 0.2f),
                new Vector2(5f, 0.45f),
                PlatformGreen,
                StaticBlockKind.Platform
            );
            CreateStaticBlock(
                "Z4_Descent",
                zone,
                new Vector2(148.5f, -0.65f),
                new Vector2(4f, 0.45f),
                PlatformGreen,
                StaticBlockKind.Platform
            );

            var checkpoint = CreateCheckpointObject(
                "Checkpoint_2",
                zone,
                2,
                new Vector2(148f, -0.75f)
            );
            _checkpoints.Add(checkpoint);
            SpawnFood("water", new Vector2(153f, -0.95f), zone);
            SpawnFood("apple", new Vector2(157f, -0.95f), zone);

            CreateSceneryProp(
                "Z4_Landmark_RallyTree",
                zone,
                new Vector2(152f, -0.15f),
                new Vector2(3.1f, 4.3f),
                GetScenerySprite(SceneryKind.Tree),
                -2
            );
            CreateWorldLabel(
                "CLAREIRA DO REENCONTRO",
                zone,
                new Vector2(152f, 2.8f),
                new Color(0.95f, 1f, 0.72f)
            );
        }

        private void CreateZone5_PuzzleFinal(Transform parent)
        {
            var zone = NewZone("Z5_PuzzleFinal", parent);

            CreateStaticBlock(
                "Z5_LeftFloor",
                zone,
                new Vector2(167f, -1.8f),
                new Vector2(14f, 0.8f),
                GroundGreen
            );
            CreateStaticBlock(
                "Z5_RightFloor",
                zone,
                new Vector2(188f, -1.8f),
                new Vector2(14f, 0.8f),
                GroundGreen
            );
            CreateStaticBlock(
                "Z5_LeverPerch",
                zone,
                new Vector2(173f, 2.75f),
                new Vector2(7f, 0.55f),
                PlatformGreen,
                StaticBlockKind.Platform
            );
            CreateStaticBlock(
                "Z5_GateLintel",
                zone,
                new Vector2(180f, 3.8f),
                new Vector2(5f, 0.6f),
                StoneGreen,
                StaticBlockKind.Wall
            );

            var button = CreatePressureButtonObject(
                "Z5_PressureButton",
                zone,
                new Vector2(168f, -1.23f)
            );
            var box = CreatePushableBox("Z5_PushableBox", zone, new Vector2(163f, -0.95f));
            var lift = CreateActivatedPlatformObject(
                "Z5_CooperationLift",
                zone,
                new Vector2(173f, -0.85f),
                new Vector2(3.8f, 0.45f),
                new Vector2(0f, 2.5f)
            );
            var lever = CreateLeverObject(
                "Z5_UpperLever",
                zone,
                new Vector2(173f, 3.6f),
                "ALAVANCA DO PORTÃO"
            );
            var door = CreatePuzzleDoorObject(
                "Z5_CooperativeDoor",
                zone,
                new Vector2(180f, 0.25f),
                new Vector2(1.3f, 4f)
            );
            var reset = CreateLeverObject(
                "Z5_ResetSwitch",
                zone,
                new Vector2(160.8f, -0.95f),
                "RECUPERAR CAIXA",
                false
            );

            var liftControllerObject = new GameObject("Z5_LiftController");
            liftControllerObject.transform.SetParent(zone, false);
            var liftController = liftControllerObject.AddComponent<CooperativePuzzleController>();
            liftController.Bind(button, null, box, null, lift, null, requireAll: false);

            var finalControllerObject = new GameObject("Z5_FinalCooperativeController");
            finalControllerObject.transform.SetParent(zone, false);
            _finalPuzzle = finalControllerObject.AddComponent<CooperativePuzzleController>();
            _finalPuzzle.Bind(button, lever, box, door, null, reset, requireAll: true);

            button.OnPressedStateChanged += pressed =>
            {
                if (pressed)
                    AudioService.Instance.PlaySfx(AudioClipId.Sfx_Button);
            };
            lever.OnToggled += active => AudioService.Instance.PlaySfx(AudioClipId.Sfx_Lever);
            door.OnOpened += () => AudioService.Instance.PlaySfx(AudioClipId.Sfx_Door);
            _finalPuzzle.OnPuzzleSolved += () =>
                AudioService.Instance.PlaySfx(AudioClipId.Sfx_PuzzleSolved);

            // Recovery ledges return a fallen player to the mechanism; none crosses the gate.
            CreateStaticBlock(
                "Z5_RecoveryStepA",
                zone,
                new Vector2(163f, 0.25f),
                new Vector2(3f, 0.4f),
                PlatformGreen,
                StaticBlockKind.Platform
            );
            CreateStaticBlock(
                "Z5_RecoveryStepB",
                zone,
                new Vector2(166f, 1.35f),
                new Vector2(3f, 0.4f),
                PlatformGreen,
                StaticBlockKind.Platform
            );

            SpawnFood("carrot", new Vector2(173f, 4.35f), zone);
            SpawnFood("avocado", new Vector2(187f, -0.95f), zone);
            BuildBalanceTowerLandmark(zone, new Vector2(180f, 0.25f));
        }

        private void CreateZone6_Chegada(Transform parent)
        {
            var zone = NewZone("Z6_Chegada", parent);

            CreateStaticBlock(
                "Z6_Approach",
                zone,
                new Vector2(201f, -1.8f),
                new Vector2(12f, 0.8f),
                GroundGreen
            );
            CreateStaticBlock(
                "Z6_FinalStep",
                zone,
                new Vector2(209f, -0.75f),
                new Vector2(5f, 0.45f),
                PlatformGreen,
                StaticBlockKind.Platform
            );
            CreateStaticBlock(
                "Z6_FinishTerrace",
                zone,
                new Vector2(218f, -1.8f),
                new Vector2(15f, 0.8f),
                GroundGreen
            );
            CreateStaticBlock(
                "Z6_RightBoundary",
                zone,
                new Vector2(228f, 0f),
                new Vector2(1f, 10f),
                StoneGreen,
                StaticBlockKind.Wall
            );

            var finalSpikes = CreateSpikeHazardObject(
                "Z6_FinalSpikes",
                zone,
                new Vector2(204f, -1.22f),
                new Vector2(2.4f, 0.45f),
                5,
                new Vector2(148f, -0.3f)
            );
            _spikes.Add(finalSpikes);
            SpawnFood("strawberry", new Vector2(209f, 0.05f), zone);

            _finishLine = CreateFinishLineObject("FinishLine", zone, new Vector2(FinishX, 0.2f));
            CreateStaticBlock(
                "Z6_Podium",
                zone,
                new Vector2(222f, -1.05f),
                new Vector2(4f, 0.45f),
                new Color(0.85f, 0.7f, 0.2f),
                StaticBlockKind.Platform
            );
            CreateWorldLabel(
                "PAVILHÃO DO EQUILÍBRIO",
                zone,
                new Vector2(215f, 3.8f),
                new Color(1f, 0.9f, 0.3f)
            );
            CreateSceneryProp(
                "Z6_Landmark_CelebrationTree",
                zone,
                new Vector2(224.5f, -0.05f),
                new Vector2(3.2f, 4.5f),
                GetScenerySprite(SceneryKind.Tree),
                -2
            );
        }

        private static Transform NewZone(string name, Transform parent)
        {
            var zone = new GameObject(name).transform;
            zone.SetParent(parent, false);
            return zone;
        }

        private void BuildStoneArchLandmark(Transform parent, Vector2 position)
        {
            var landmark = NewZone("Landmark_StoneArch", parent);
            CreateStaticBlock(
                "LeftPillar",
                landmark,
                position + new Vector2(-2.5f, 1.2f),
                new Vector2(0.8f, 3f),
                StoneGreen,
                StaticBlockKind.Wall
            );
            CreateStaticBlock(
                "RightPillar",
                landmark,
                position + new Vector2(2.5f, 1.2f),
                new Vector2(0.8f, 3f),
                StoneGreen,
                StaticBlockKind.Wall
            );
            CreateStaticBlock(
                "Lintel",
                landmark,
                position + new Vector2(0f, 2.7f),
                new Vector2(5.8f, 0.7f),
                StoneGreen,
                StaticBlockKind.Wall
            );
        }

        private void BuildSpringLandmark(Transform parent, Vector2 position)
        {
            var spring = NewZone("Landmark_CrystalSpring", parent);
            var water = new GameObject("WaterGlow");
            water.transform.SetParent(spring, false);
            water.transform.position = position;
            water.transform.localScale = new Vector3(4.5f, 0.55f, 1f);
            var renderer = water.AddComponent<SpriteRenderer>();
            renderer.sprite = PlaceholderSpriteFactory.Square;
            renderer.color = new Color(0.25f, 0.78f, 0.95f, 0.55f);
            renderer.sortingOrder = -1;
            CreateSceneryProp(
                "SpringShrubA",
                spring,
                position + new Vector2(-2.5f, 0.35f),
                new Vector2(1.4f, 1f),
                GetScenerySprite(SceneryKind.Shrub)
            );
            CreateSceneryProp(
                "SpringShrubB",
                spring,
                position + new Vector2(2.5f, 0.35f),
                new Vector2(1.4f, 1f),
                GetScenerySprite(SceneryKind.Shrub)
            );
        }

        private void BuildBalanceTowerLandmark(Transform parent, Vector2 position)
        {
            var tower = NewZone("Landmark_BalanceTower", parent);
            CreateStaticBlock(
                "TowerLeft",
                tower,
                position + new Vector2(-2.8f, 1.5f),
                new Vector2(0.55f, 6.2f),
                StoneGreen,
                StaticBlockKind.Wall
            );
            CreateStaticBlock(
                "TowerRight",
                tower,
                position + new Vector2(2.8f, 1.5f),
                new Vector2(0.55f, 6.2f),
                StoneGreen,
                StaticBlockKind.Wall
            );
        }
    }
}
