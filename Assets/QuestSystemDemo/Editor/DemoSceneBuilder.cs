using System.Collections.Generic;
using System.IO;
using MochoIndieStudio.QuestSystem;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TileColliderType = UnityEngine.Tilemaps.Tile.ColliderType;

namespace MochoIndieStudio.QuestSystemDemo.EditorTools
{
    /// <summary>
    /// Builds (or rebuilds) the "Water from the Well" demo scene from the data assets and
    /// <see cref="DemoSpriteMap"/>. Everything the demo needs -- tilemap, player, pickups, well,
    /// full uGUI HUD, systems -- is created here in code, so the scene is disposable: tune the
    /// sprite map or the constants below and run <c>Tools ▸ Quest Demo ▸ Build Scene</c> again.
    /// </summary>
    public static class DemoSceneBuilder
    {
        private const string Root = "Assets/QuestSystemDemo";
        private const string ScenePath = Root + "/Scenes/QuestDemo.unity";
        private const string TilesFolder = Root + "/Art/Tiles";
        private const string PalettesFolder = Root + "/Art/Palettes";
        private const string DataFolder = Root + "/Data";

        // Sorting: everything that Y-sorts against the player shares order 0.
        private const int GroundOrder = -20;
        private const int PathsOrder = -10;
        private const int WorldOrder = 0;

        // --- layout (tilemap cells; cell size 1 = 1 world unit = one 16px sprite) ---
        private const int MapWidth = 34;
        private const int MapHeight = 24;
        private static readonly Vector2Int PlayerCell = new Vector2Int(17, 10);
        private static readonly Vector2Int BucketCell = new Vector2Int(5, 18);
        private static readonly Vector2Int RopeCell = new Vector2Int(28, 5);
        private static readonly Vector2Int WellTopLeftCell = new Vector2Int(16, 18); // 1x2 vertical

        [MenuItem("Tools/Quest Demo/Build Scene")]
        public static void Build()
        {
            Directory.CreateDirectory(TilesFolder);

            // The new scene must exist BEFORE assets are loaded and assigned: assigning an asset
            // reference to a component created in a scene that was itself created after the load
            // does not persist through the first save. No AssetDatabase.StartAssetEditing() either,
            // for the same reason.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            DemoSpriteMap sprites = LoadOrWarn<DemoSpriteMap>(DataFolder + "/DemoSpriteMap.asset");
            QuestList questList = LoadOrWarn<QuestList>(DataFolder + "/WaterFromTheWell.asset");
            ItemDefinition bucketItem = LoadOrWarn<ItemDefinition>(DataFolder + "/Items/Bucket.asset");
            ItemDefinition ropeItem = LoadOrWarn<ItemDefinition>(DataFolder + "/Items/Rope.asset");
            ItemDefinition bucketOnRope = LoadOrWarn<ItemDefinition>(DataFolder + "/Items/BucketOnRope.asset");
            ItemDefinition bucketOfWater = LoadOrWarn<ItemDefinition>(DataFolder + "/Items/BucketOfWater.asset");
            if (sprites == null || questList == null)
            {
                return;
            }

            var grid = new GameObject("Grid").AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            ApplyYSort();

            TileSet tiles = BuildTileAssets(sprites);
            DeleteOrphanTiles(tiles);
            BuildGround(grid, sprites, tiles);
            BuildPathsLayer(grid);
            Tilemap decor = BuildDecorLayer(grid, tiles);
            Transform well = BuildWell(grid, decor, sprites, tiles);
            BuildPalette(tiles);

            Inventory inventory;
            Transform player = BuildPlayer(sprites, out _);
            player.position = CellCenter(PlayerCell);

            BuildPickup("Bucket", bucketItem, sprites.bucket, CellCenter(BucketCell));
            BuildPickup("Rope", ropeItem, sprites.rope, CellCenter(RopeCell));

            Camera camera = BuildCamera(player);

            Hud hud = BuildHud();

            GameObject systemsGo = new GameObject("DemoSystems");
            inventory = systemsGo.AddComponent<Inventory>();
            DemoInput input = systemsGo.AddComponent<DemoInput>();
            DemoGame game = systemsGo.AddComponent<DemoGame>();
            DemoHud demoHud = systemsGo.AddComponent<DemoHud>();

            // --- wire everything ---
            Set(game, "questList", questList);
            Set(game, "inventory", inventory);
            Set(game, "bucketOnRope", bucketOnRope);
            Set(game, "bucketOfWater", bucketOfWater);

            foreach (Interactable it in Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None))
            {
                Set(it, "inventory", inventory);
            }

            PlayerController pc = player.GetComponent<PlayerController>();
            Set(pc, "input", input);
            Set(pc, "hud", demoHud);
            PlayerAnimator pa = player.GetComponent<PlayerAnimator>();
            Set(pa, "spriteMap", sprites);
            Set(pa, "player", pc);

            camera.GetComponent<CameraFollow2D>().SetTarget(player);

            Set(hud.questLog, "game", game);
            Set(hud.inventory, "inventory", inventory);
            Set(hud.inventory, "game", game);

            Set(demoHud, "input", input);
            Set(demoHud, "game", game);
            Set(demoHud, "questLog", hud.questLog);
            Set(demoHud, "inventory", hud.inventory);
            Set(demoHud, "pauseMenu", hud.pauseMenu);
            Set(demoHud, "helpPanel", hud.helpPanel);
            Set(demoHud, "toast", hud.toast);
            Set(demoHud, "promptGroup", hud.promptGroup);
            Set(demoHud, "promptLabel", hud.promptLabel);

            EditorSceneManager.MarkSceneDirty(scene);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);

            EnsureInBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[QuestDemo] Scene built at " + ScenePath);
        }

        // ---------------------------------------------------------------- tilemaps

        /// <summary>The shared <see cref="Tile"/> assets, one per role. Grass edges, trees and rocks
        /// use <c>Sprite</c> collider type so a Custom Physics Shape drawn on the sprite becomes the
        /// collider; interior grass and dirt/path don't block.</summary>
        private sealed class TileSet
        {
            public readonly List<Tile> Palette = new List<Tile>();
            public Tile grassC;
            public readonly Tile[] grass9 = new Tile[9]; // tl,t,tr,l,c,r,bl,b,br
            public readonly Tile[] dirt9 = new Tile[9];
            public Tile[] dirtPath;
            public Tile[] decor;
            public Tile[] bigTree;  // tl,tr,bl,br
            public Tile[] smallTree; // top,bottom
            public Tile rock1;
            public readonly Tile[] well = new Tile[4];
        }

        private static TileSet BuildTileAssets(DemoSpriteMap s)
        {
            Directory.CreateDirectory(TilesFolder);
            var t = new TileSet();

            // Every non-path tile asks for a Sprite collider; SharedTile keeps it only where the
            // sprite has a Custom Physics Shape, so a tile blocks exactly where the artist drew one.
            var g = s.grass;
            Sprite[] gs = { g.topLeft, g.top, g.topRight, g.left, g.center, g.right, g.bottomLeft, g.bottom, g.bottomRight };
            string[] gn = { "TL", "T", "TR", "L", "C", "R", "BL", "B", "BR" };
            for (int i = 0; i < 9; i++)
            {
                t.grass9[i] = SharedTile("Grass_" + gn[i], gs[i] ?? g.center, TileColliderType.Sprite, t.Palette);
            }
            t.grassC = t.grass9[4];

            var d = s.dirt;
            Sprite[] ds = { d.topLeft, d.top, d.topRight, d.left, d.center, d.right, d.bottomLeft, d.bottom, d.bottomRight };
            for (int i = 0; i < 9; i++)
            {
                t.dirt9[i] = SharedTile("Dirt_" + gn[i], ds[i] ?? d.center, TileColliderType.None, t.Palette);
            }

            t.dirtPath = SharedTiles("Path", s.dirtPath, TileColliderType.None, t.Palette);
            t.decor = SharedTiles("Decor", s.decor, TileColliderType.Sprite, t.Palette);
            t.bigTree = SharedTiles("BigTree", s.bigTree, TileColliderType.Sprite, t.Palette);
            t.smallTree = SharedTiles("SmallTree", s.smallTree, TileColliderType.Sprite, t.Palette);
            t.rock1 = SharedTile("Rock1", s.rock1, TileColliderType.Sprite, t.Palette);

            int wellCount = s.well.tiles != null ? Mathf.Min(s.well.tiles.Length, 4) : 0;
            for (int i = 0; i < wellCount; i++)
            {
                t.well[i] = SharedTile("Well_" + i, s.well.tiles[i], TileColliderType.Sprite, t.Palette);
            }

            return t;
        }

        private static void BuildGround(Grid grid, DemoSpriteMap sprites, TileSet tiles)
        {
            var go = new GameObject("Ground");
            go.transform.SetParent(grid.transform, false);
            var tilemap = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>().sortingOrder = GroundOrder;

            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    bool l = x == 0, r = x == MapWidth - 1, b = y == 0, top = y == MapHeight - 1;
                    Tile tile = tiles.grass9[GrassIndex(l, r, top, b)];
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            go.AddComponent<TilemapCollider2D>();
        }

        private static int GrassIndex(bool l, bool r, bool top, bool b)
        {
            if (top && l) return 0;
            if (top && r) return 2;
            if (b && l) return 6;
            if (b && r) return 8;
            if (top) return 1;
            if (b) return 7;
            if (l) return 3;
            if (r) return 5;
            return 4;
        }

        /// <summary>Empty tilemap that renders just below the world layer -- paint the dirt path here.</summary>
        private static void BuildPathsLayer(Grid grid)
        {
            var go = new GameObject("Paths");
            go.transform.SetParent(grid.transform, false);
            go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>().sortingOrder = PathsOrder;
        }

        /// <summary>Decor tilemap: shares sorting order 0 with the player and uses
        /// <see cref="TilemapRenderer.Mode.Individual"/> so tall trees Y-sort against the player.
        /// Scatters a few trees / rocks / props so the map has some life. Colliders come from each
        /// tile's Custom Physics Shape.</summary>
        private static Tilemap BuildDecorLayer(Grid grid, TileSet tiles)
        {
            var go = new GameObject("Decor");
            go.transform.SetParent(grid.transform, false);
            var decor = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = WorldOrder;
            renderer.mode = TilemapRenderer.Mode.Individual;

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            go.AddComponent<TilemapCollider2D>();

            // A few big trees (2x2, top-left cell given) -- kept clear of the player start, pickups and well.
            var bigTreeCells = new[]
            {
                new Vector2Int(4, 5), new Vector2Int(27, 17), new Vector2Int(9, 19), new Vector2Int(24, 8)
            };
            if (tiles.bigTree != null && tiles.bigTree.Length >= 4)
            {
                foreach (Vector2Int c in bigTreeCells)
                {
                    decor.SetTile(new Vector3Int(c.x, c.y + 1, 0), tiles.bigTree[0]);
                    decor.SetTile(new Vector3Int(c.x + 1, c.y + 1, 0), tiles.bigTree[1]);
                    decor.SetTile(new Vector3Int(c.x, c.y, 0), tiles.bigTree[2]);
                    decor.SetTile(new Vector3Int(c.x + 1, c.y, 0), tiles.bigTree[3]);
                }
            }

            // A couple of small trees (1x2).
            if (tiles.smallTree != null && tiles.smallTree.Length >= 2)
            {
                foreach (Vector2Int c in new[] { new Vector2Int(13, 6), new Vector2Int(21, 20) })
                {
                    decor.SetTile(new Vector3Int(c.x, c.y + 1, 0), tiles.smallTree[0]);
                    decor.SetTile(new Vector3Int(c.x, c.y, 0), tiles.smallTree[1]);
                }
            }

            if (tiles.rock1 != null)
            {
                foreach (Vector2Int c in new[] { new Vector2Int(8, 8), new Vector2Int(25, 13), new Vector2Int(15, 15) })
                {
                    decor.SetTile(new Vector3Int(c.x, c.y, 0), tiles.rock1);
                }
            }

            if (tiles.decor != null && tiles.decor.Length > 0)
            {
                var spots = new[]
                {
                    new Vector2Int(11, 12), new Vector2Int(20, 9), new Vector2Int(7, 16), new Vector2Int(29, 12)
                };
                for (int i = 0; i < spots.Length; i++)
                {
                    decor.SetTile(new Vector3Int(spots[i].x, spots[i].y, 0), tiles.decor[i % tiles.decor.Length]);
                }
            }

            return decor;
        }

        private static Transform BuildWell(Grid grid, Tilemap decor, DemoSpriteMap sprites, TileSet tiles)
        {
            Vector2Int size = sprites.well.gridSize;
            if (size.x < 1 || size.y < 1)
            {
                size = new Vector2Int(1, 2);
            }

            for (int row = 0; row < size.y; row++)
            {
                for (int col = 0; col < size.x; col++)
                {
                    int flat = row * Mathf.Max(1, size.x) + col;
                    if (flat >= tiles.well.Length || tiles.well[flat] == null)
                    {
                        continue;
                    }

                    int cx = WellTopLeftCell.x + col;
                    int cy = WellTopLeftCell.y + (size.y - 1 - row);
                    decor.SetTile(new Vector3Int(cx, cy, 0), tiles.well[flat]);
                }
            }

            // No collider here -- the well's solidity comes from the Custom Physics Shape on its
            // tiles (Decor tilemap). This GameObject just carries the interaction zone.
            var go = new GameObject("Well");
            go.transform.position = CellCenter(WellTopLeftCell) + new Vector3((size.x - 1) * 0.5f, (size.y - 1) * 0.5f, 0f);

            var zone = new GameObject("InteractionZone");
            zone.transform.SetParent(go.transform, false);
            var trigger = zone.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = Mathf.Max(size.x, size.y) * 0.5f + 1f;
            zone.AddComponent<WellInteractable>();

            return go.transform;
        }

        // ---------------------------------------------------------------- tile assets + palette

        private static Tile SharedTile(string name, Sprite sprite, TileColliderType collider, List<Tile> palette)
        {
            if (sprite == null)
            {
                return null;
            }

            // A tile is only solid where the artist actually drew a Custom Physics Shape. Asking for
            // ColliderType.Sprite on a sprite with no physics shape would fall back to the full sprite
            // mesh (this sheet imports as FullRect -> a full-tile block), so downgrade those to None.
            if (collider == TileColliderType.Sprite && sprite.GetPhysicsShapeCount() == 0)
            {
                collider = TileColliderType.None;
            }

            string path = TilesFolder + "/QD_" + name + ".asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.colliderType = collider;
                AssetDatabase.CreateAsset(tile, path);
            }
            else
            {
                tile.sprite = sprite;
                tile.colliderType = collider;
                EditorUtility.SetDirty(tile);
            }

            palette?.Add(tile);
            return tile;
        }

        /// <summary>Deletes any <c>QD_*.asset</c> tile under <see cref="TilesFolder"/> that the current
        /// build no longer uses (e.g. left over from a shorter sprite array on a previous run).</summary>
        private static void DeleteOrphanTiles(TileSet tiles)
        {
            var keep = new HashSet<string>();
            foreach (Tile tile in tiles.Palette)
            {
                if (tile != null)
                {
                    keep.Add(AssetDatabase.GetAssetPath(tile));
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Tile", new[] { TilesFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!keep.Contains(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
        }

        private static Tile[] SharedTiles(string prefix, Sprite[] sprites, TileColliderType collider, List<Tile> palette)
        {
            if (sprites == null)
            {
                return System.Array.Empty<Tile>();
            }

            var result = new List<Tile>();
            for (int i = 0; i < sprites.Length; i++)
            {
                Tile tile = SharedTile(prefix + "_" + i, sprites[i], collider, palette);
                if (tile != null)
                {
                    result.Add(tile);
                }
            }

            return result.ToArray();
        }

        /// <summary>Creates a real Tile Palette asset (<c>Art/Palettes/QuestDemo.prefab</c>) the first
        /// time and lays every <see cref="TileSet"/> tile into it, so the user can paint straight
        /// away. On a rebuild it repaints the existing palette to match the current tile set.</summary>
        private static void BuildPalette(TileSet tiles)
        {
            Directory.CreateDirectory(PalettesFolder);
            const string palettePath = PalettesFolder + "/QuestDemo.prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(palettePath) == null && !TryCreatePaletteAsset(palettePath, "QuestDemo"))
            {
                Debug.LogWarning("[QuestDemo] Could not create the Tile Palette automatically -- " +
                                 "make one from the QD_* tiles in " + TilesFolder + ".");
                return;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(palettePath);
            try
            {
                var tilemap = contents.GetComponentInChildren<Tilemap>();
                if (tilemap == null)
                {
                    return;
                }

                tilemap.ClearAllTiles();
                const int perRow = 9;
                int col = 0, row = 0;
                var seen = new HashSet<Tile>();
                foreach (Tile tile in tiles.Palette)
                {
                    if (tile == null || !seen.Add(tile))
                    {
                        continue;
                    }

                    tilemap.SetTile(new Vector3Int(col, -row, 0), tile);
                    if (++col >= perRow)
                    {
                        col = 0;
                        row++;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(contents, palettePath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static bool TryCreatePaletteAsset(string path, string name)
        {
            System.Type util = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                util = asm.GetType("UnityEditor.Tilemaps.GridPaletteUtility");
                if (util != null)
                {
                    break;
                }
            }

            System.Reflection.MethodInfo create = null;
            if (util != null)
            {
                foreach (var m in util.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                {
                    if (m.Name == "CreateNewPalette" && m.GetParameters().Length == 6)
                    {
                        create = m;
                        break;
                    }
                }
            }

            if (create == null)
            {
                return false;
            }

            try
            {
                System.Type cellSizingType = create.GetParameters()[3].ParameterType;
                object automatic = System.Enum.Parse(cellSizingType, "Automatic");
                create.Invoke(null, new object[]
                {
                    PalettesFolder, name, GridLayout.CellLayout.Rectangle, automatic, Vector3.one, GridLayout.CellSwizzle.XYZ
                });
                AssetDatabase.ImportAsset(path);
                return AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[QuestDemo] palette API call failed: " + e.Message);
                return false;
            }
        }

        // ---------------------------------------------------------------- actors

        private static Transform BuildPlayer(DemoSpriteMap sprites, out SpriteRenderer renderer)
        {
            var go = new GameObject("Player");
            go.tag = "Player";
            renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = WorldOrder;
            Sprite[] down = sprites.player.Frames(Facing.Down);
            if (down != null && down.Length > 0 && down[0] != null)
            {
                renderer.sprite = down[0];
            }

            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var collider = go.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.7f, 0.9f);

            go.AddComponent<PlayerController>();
            go.AddComponent<PlayerAnimator>();
            return go.transform;
        }

        private static void BuildPickup(string label, ItemDefinition item, Sprite sprite, Vector3 position)
        {
            var go = new GameObject(label);
            go.transform.position = position;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = WorldOrder;

            var trigger = go.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.8f;

            var pickup = go.AddComponent<ItemPickup>();
            Set(pickup, "item", item);
        }

        /// <summary>Sets the project-wide transparency sort to a custom +Y axis, so the URP renderer
        /// (not just the camera) draws sprites back-to-front by world Y for the tree Y-sorting.</summary>
        private static void ApplyYSort()
        {
            try
            {
                var gs = typeof(UnityEngine.Rendering.GraphicsSettings);
                gs.GetProperty("transparencySortMode")?.SetValue(null, TransparencySortMode.CustomAxis);
                gs.GetProperty("transparencySortAxis")?.SetValue(null, new Vector3(0f, 1f, 0f));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[QuestDemo] Could not set project transparency sort: " + e.Message +
                                 " -- set it manually in Project Settings ▸ Graphics ▸ Camera.");
            }
        }

        private static Camera BuildCamera(Transform target)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8f;
            camera.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
            camera.clearFlags = CameraClearFlags.SolidColor;

            // Y-sorting: sprites sharing a sorting layer + order are drawn back-to-front along -Y,
            // so the player passes behind a tree whose base is above them and in front of one below.
            camera.transparencySortMode = TransparencySortMode.CustomAxis;
            camera.transparencySortAxis = new Vector3(0f, 1f, 0f);

            go.transform.position = new Vector3(target.position.x, target.position.y, -10f);
            go.AddComponent<CameraFollow2D>();
            return camera;
        }

        // ---------------------------------------------------------------- HUD

        private struct Hud
        {
            public QuestLogView questLog;
            public InventoryView inventory;
            public PauseMenuView pauseMenu;
            public GameObject helpPanel;
            public ToastView toast;
            public CanvasGroup promptGroup;
            public TMP_Text promptLabel;
        }

        private static Hud BuildHud()
        {
            var canvasGo = new GameObject("HUD Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>();

            var hud = new Hud();

            // Interaction prompt (bottom-centre).
            RectTransform promptRt = MakePanel(canvas.transform, "InteractionPrompt",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(520f, 60f),
                new Color(0f, 0f, 0f, 0.55f));
            hud.promptGroup = promptRt.gameObject.AddComponent<CanvasGroup>();
            hud.promptGroup.alpha = 0f;
            hud.promptLabel = MakeLabel(promptRt, "Label", "[E] ...", 26, TextAlignmentOptions.Center);

            // Toast (top-centre).
            RectTransform toastRt = MakePanel(canvas.transform, "Toast",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(760f, 64f),
                new Color(0.15f, 0.35f, 0.2f, 0.9f));
            var toastGroup = toastRt.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            TMP_Text toastLabel = MakeLabel(toastRt, "Label", "", 26, TextAlignmentOptions.Center);
            hud.toast = toastRt.gameObject.AddComponent<ToastView>();
            Set(hud.toast, "group", toastGroup);
            Set(hud.toast, "label", toastLabel);

            // Help panel (top-right, shown by default, toggled with F1).
            const float helpHeight = 232f;
            RectTransform helpRt = MakeBox(canvas.transform, "HelpPanel",
                new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(320f, helpHeight),
                new Color(0.05f, 0.05f, 0.08f, 0.92f));
            var helpCol = MakeColumn(helpRt, "Lines");
            StretchToParent(helpCol, 14f);
            MakeLabel(helpCol, "Title", "Controls  (F1)", 24, TextAlignmentOptions.TopLeft).fontStyle = FontStyles.Bold;
            MakeLabel(helpCol, "Keys",
                "WASD / Arrows — Move\nE — Interact\nTAB — Quest Log\nI — Inventory\nESC — Menu",
                20, TextAlignmentOptions.TopLeft);
            hud.helpPanel = helpRt.gameObject;

            // Quest log (top-right, below the help panel; scrolls when objectives overflow).
            RectTransform questRt = MakeBox(canvas.transform, "QuestLogPanel",
                new Vector2(1f, 1f), new Vector2(-24f, -24f - helpHeight - 12f), new Vector2(420f, 340f),
                new Color(0.05f, 0.05f, 0.08f, 0.92f));
            TMP_Text questTitle = MakeLabel(questRt, "Title", "Quest", 26, TextAlignmentOptions.TopLeft);
            questTitle.fontStyle = FontStyles.Bold;
            var titleRt = questTitle.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(-28f, 36f);
            titleRt.anchoredPosition = new Vector2(0f, -12f);

            RectTransform rowContainer = MakeScrollView(questRt, 56f);
            ObjectiveRowView rowTemplate = MakeObjectiveRow(rowContainer);
            hud.questLog = questRt.gameObject.AddComponent<QuestLogView>();
            Set(hud.questLog, "panel", questRt.gameObject);
            Set(hud.questLog, "titleLabel", questTitle);
            Set(hud.questLog, "rowContainer", rowContainer);
            Set(hud.questLog, "rowTemplate", rowTemplate);
            questRt.gameObject.SetActive(false);

            // Inventory (bottom-centre).
            RectTransform invRt = MakePanel(canvas.transform, "InventoryPanel",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(720f, 240f),
                new Color(0.05f, 0.05f, 0.08f, 0.92f), TextAnchor.UpperCenter);
            MakeLabel(invRt, "Header", "Inventory", 26, TextAlignmentOptions.Top).fontStyle = FontStyles.Bold;
            RectTransform slotContainer = MakeGrid(invRt, "Slots");
            InventorySlotView slotTemplate = MakeInventorySlot(slotContainer);
            GameObject emptyLabelGo = MakeLabel(invRt, "Empty", "(empty)", 22, TextAlignmentOptions.Center).gameObject;
            Button craftButton = MakeButton(invRt, "CraftButton", "Craft bucket on a rope");
            hud.inventory = invRt.gameObject.AddComponent<InventoryView>();
            Set(hud.inventory, "panel", invRt.gameObject);
            Set(hud.inventory, "slotContainer", slotContainer);
            Set(hud.inventory, "slotTemplate", slotTemplate);
            Set(hud.inventory, "craftButton", craftButton);
            Set(hud.inventory, "emptyLabel", emptyLabelGo);
            invRt.gameObject.SetActive(false);

            // Pause menu (full-screen dim + centre panel).
            RectTransform dim = MakePanel(canvas.transform, "PauseMenuPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f),
                new Color(0f, 0f, 0f, 0.6f), TextAnchor.MiddleCenter);
            RectTransform menuBox = MakePanel(dim, "Menu",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 280f),
                new Color(0.08f, 0.08f, 0.12f, 0.98f), TextAnchor.UpperCenter);
            MakeLabel(menuBox, "Title", "Paused", 34, TextAlignmentOptions.Top).fontStyle = FontStyles.Bold;
            Button restart = MakeButton(menuBox, "Restart", "Restart Scene");
            Button quit = MakeButton(menuBox, "Quit", "Quit");
            hud.pauseMenu = dim.gameObject.AddComponent<PauseMenuView>();
            Set(hud.pauseMenu, "panel", dim.gameObject);
            Set(hud.pauseMenu, "restartButton", restart);
            Set(hud.pauseMenu, "quitButton", quit);
            dim.gameObject.SetActive(false);

            return hud;
        }

        // ---------------------------------------------------------------- UI helpers

        private static RectTransform MakePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 size, Color color, TextAnchor childAlign = TextAnchor.UpperCenter)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;

            var image = go.AddComponent<Image>();
            image.color = color;

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = childAlign;
            layout.padding = new RectOffset(16, 16, 12, 12);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return rt;
        }

        private static TMP_Text MakeLabel(Transform parent, string name, string text, float size,
            TextAlignmentOptions align, bool sizeFitter = true)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.alignment = align;
            label.color = Color.white;
            label.raycastTarget = false;
            if (sizeFitter)
            {
                go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            return label;
        }

        private static RectTransform MakeColumn(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return (RectTransform)go.transform;
        }

        /// <summary>A plain <see cref="Image"/> box anchored to a single canvas corner (no layout group).</summary>
        private static RectTransform MakeBox(Transform parent, string name, Vector2 corner, Vector2 offset,
            Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = corner;
            rt.anchorMax = corner;
            rt.pivot = corner;
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
            go.AddComponent<Image>().color = color;
            return rt;
        }

        private static void StretchToParent(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>A vertical <see cref="ScrollRect"/> filling <paramref name="parent"/> below
        /// <paramref name="topInset"/>. Returns the Content transform to parent rows into.</summary>
        private static RectTransform MakeScrollView(RectTransform parent, float topInset)
        {
            var scrollGo = new GameObject("Scroll", typeof(RectTransform));
            scrollGo.transform.SetParent(parent, false);
            var scrollRt = (RectTransform)scrollGo.transform;
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.pivot = new Vector2(0.5f, 1f);
            scrollRt.offsetMin = new Vector2(12f, 12f);
            scrollRt.offsetMax = new Vector2(-12f, -topInset);
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRt = (RectTransform)viewportGo.transform;
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.pivot = new Vector2(0f, 1f);
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            viewportGo.AddComponent<RectMask2D>();

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRt = (RectTransform)contentGo.transform;
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0f, 0f);
            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.padding = new RectOffset(2, 2, 2, 2);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRt;
            scroll.content = contentRt;
            return contentRt;
        }

        private static RectTransform MakeGrid(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var grid = go.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(120f, 120f);
            grid.spacing = new Vector2(8f, 8f);
            grid.childAlignment = TextAnchor.MiddleCenter;
            return (RectTransform)go.transform;
        }

        private static ObjectiveRowView MakeObjectiveRow(Transform parent)
        {
            var go = new GameObject("ObjectiveRow", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var group = go.AddComponent<CanvasGroup>();

            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 6f;
            h.childAlignment = TextAnchor.UpperLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;

            var checkGo = new GameObject("Check", typeof(RectTransform));
            checkGo.transform.SetParent(go.transform, false);
            var check = checkGo.AddComponent<Image>();
            check.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
            check.color = new Color(0.35f, 0.8f, 0.35f);
            check.preserveAspect = true;
            check.raycastTarget = false;
            check.enabled = false;
            var checkLe = checkGo.AddComponent<LayoutElement>();
            checkLe.minWidth = 22f;
            checkLe.preferredWidth = 22f;
            checkLe.minHeight = 22f;

            TMP_Text label = MakeLabel(go.transform, "Label", "Objective", 20, TextAlignmentOptions.TopLeft, false);
            label.textWrappingMode = TextWrappingModes.Normal;
            var labelLe = label.gameObject.AddComponent<LayoutElement>();
            labelLe.flexibleWidth = 1f;
            labelLe.minHeight = 24f;

            var row = go.AddComponent<ObjectiveRowView>();
            Set(row, "group", group);
            Set(row, "label", label);
            Set(row, "checkmark", check);
            return row;
        }

        private static InventorySlotView MakeInventorySlot(Transform parent)
        {
            var go = new GameObject("Slot", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(go.transform, false);
            var iconRt = (RectTransform)iconGo.transform;
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(80f, 80f);
            iconRt.anchoredPosition = new Vector2(0f, 12f);
            var icon = iconGo.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            TMP_Text nameLabel = MakeLabel(go.transform, "Name", "", 16, TextAlignmentOptions.Bottom);
            var nameRt = nameLabel.rectTransform;
            nameRt.anchorMin = new Vector2(0f, 0f);
            nameRt.anchorMax = new Vector2(1f, 0f);
            nameRt.sizeDelta = new Vector2(0f, 28f);

            var slot = go.AddComponent<InventorySlotView>();
            Set(slot, "icon", icon);
            Set(slot, "nameLabel", nameLabel);
            return slot;
        }

        private static Button MakeButton(Transform parent, string name, string text)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.2f, 0.35f, 0.55f, 1f);
            var button = go.AddComponent<Button>();
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 48f;
            le.preferredHeight = 48f;
            TMP_Text label = MakeLabel(go.transform, "Label", text, 22, TextAlignmentOptions.Center);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.sizeDelta = Vector2.zero;
            return button;
        }

        // ---------------------------------------------------------------- utilities

        private static Vector3 CellCenter(Vector2Int cell) => new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning($"[QuestDemo] no serialized field '{field}' on {target.GetType().Name}");
                return;
            }

            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T LoadOrWarn<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                Debug.LogError($"[QuestDemo] missing asset: {path}");
            }

            return asset;
        }

        private static void EnsureInBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == scenePath))
            {
                return;
            }

            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
