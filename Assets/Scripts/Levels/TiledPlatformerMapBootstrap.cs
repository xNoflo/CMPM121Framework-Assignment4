using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways, DefaultExecutionOrder(-500)]
public class TiledPlatformerMapBootstrap : MonoBehaviour
{
    // Resource paths for the TMX map, TSX tileset metadata, and source texture.
    const string MapPath = "Maps/PlatformerLevel1Map", LegacyMapPath = "Maps/PlatformerLevel1", TilesetPath = "Maps/RoguelikeDungeonTileset", TexturePath = "Tiles/roguelikeDungeon_transparent";
    // Spawn filtering keeps enemies on roughly the same floor and not right on top of the player.
    const float SpawnLevelTolerance = 1.5f, MinSpawnDistanceFromPlayer = 6f;
    const int MaxEnemySpawns = 8;
    // Only bootstrap the scenes that use the platformer map flow.
    static readonly HashSet<string> SupportedScenes = new() { "Main", "FinishPlatformerLevelScene" };
    // These definitions drive tilemap creation instead of hard-coding each layer separately.
    static readonly (string name, int order, bool hasCollision, bool merged, string alias)[] LayerDefs =
    {
        ("Background", -2, false, false, null),
        ("Ground", 0, true, true, "Tilemap"),
        ("Platforms", 1, true, false, null)
    };
    // Generated spawn points cycle through the existing spawn "colors" the spawner expects.
    static readonly SpawnPoint.SpawnName[] SpawnKinds = { SpawnPoint.SpawnName.RED, SpawnPoint.SpawnName.GREEN, SpawnPoint.SpawnName.BONE };
    // If the TMX does not give enough good enemy spawns, cast near the player to find fallback ground.
    static readonly float[] FallbackOffsets = { -18f, -12f, -8f, 8f, 12f, 18f };

    // Runtime lookup from TMX gid -> Unity Tile.
    readonly Dictionary<int, TileBase> tiles = new();
    // Candidate enemy spawn positions discovered from the top surface of the ground layer.
    readonly List<Vector3> groundSpawns = new();

    // Create the hidden bootstrap automatically after scene load if the scene needs it.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void CreateBootstrap() => EnsureBootstrapExists();
    // In edit mode, rebuild immediately so the tilemap stays visible in the scene.
    void OnEnable() { if (!Application.isPlaying) TryBuildMap(false); }
    // In play mode, wait one frame so scene objects exist before wiring spawns and moving the player.
    IEnumerator Start() { if (!Application.isPlaying) yield break; yield return null; TryBuildMap(true); Destroy(gameObject); }

    void TryBuildMap(bool applySpawns)
    {
        // Load the TMX and parse the root <map> element.
        TextAsset mapAsset = LoadResource<TextAsset>(MapPath, LegacyMapPath);
        XElement map = mapAsset == null ? null : XDocument.Parse(mapAsset.text).Element("map");
        if (mapAsset == null || map == null) { Debug.LogError(mapAsset == null ? $"Could not load {MapPath} or {LegacyMapPath} from Resources." : "Platformer TMX file is missing its root map element."); return; }

        // TMX tile/object positions are stored in tile-space, so we need the map dimensions first.
        int w = ReadInt(map, "width"), h = ReadInt(map, "height"), tw = ReadInt(map, "tilewidth", 16), th = ReadInt(map, "tileheight", 16);
        if (!BuildTiles(map, tw, th)) return;

        // Reuse or create Unity Tilemap layers, then clear them before painting fresh TMX data.
        Dictionary<string, Tilemap> tilemaps = PrepareTilemaps();
        foreach (Tilemap tilemap in tilemaps.Values) tilemap.ClearAllTiles();
        groundSpawns.Clear();

        // Paint each named TMX layer into its matching Unity Tilemap.
        foreach (XElement layer in map.Elements("layer"))
        {
            string name = (string)layer.Attribute("name") ?? string.Empty;
            int[] gids = ParseLayer(layer);
            // Spawn discovery only cares about exposed top surfaces on the ground layer.
            if (name == "Ground") CollectGroundSpawns(gids, w, h);
            PaintLayer(name, gids, w, h, tilemaps);
        }

        // Runtime spawns and player placement are only needed in play mode.
        if (applySpawns) StartCoroutine(AssignMapSpawns(map, h, tw, th));
    }

    bool BuildTiles(XElement map, int tileWidth, int tileHeight)
    {
        tiles.Clear();
        // The texture provides the actual pixels; the tileset provides the slicing metadata.
        Texture2D texture = LoadTileTexture();
        XElement tileset = ResolveTileset(map);
        int count = ReadInt(tileset, "tilecount"), columns = ReadInt(tileset, "columns");
        if (texture == null || count <= 0 || columns <= 0) { Debug.LogError(texture == null ? $"Could not load tile texture from Resources/{TexturePath}." : "Platformer TMX tileset metadata is incomplete."); return false; }

        // Slice the tileset texture into runtime Tile assets that Tilemap.SetTile can use.
        for (int id = 0; id < count; id++)
        {
            Rect rect = new(id % columns * tileWidth, texture.height - ((id / columns + 1) * tileHeight), tileWidth, tileHeight);
            if (rect.xMax > texture.width || rect.yMin < 0) continue;
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), tileWidth);
            tile.colliderType = Tile.ColliderType.Grid;
            tile.name = $"runtime_tile_{id}";
            tiles[id] = tile;
        }

        if (tiles.Count > 0) return true;
        Debug.LogError("Could not build runtime tiles from the platformer tileset texture.");
        return false;
    }

    Dictionary<string, Tilemap> PrepareTilemaps()
    {
        // Reuse an existing Grid if one exists; otherwise create the parent object for all tilemaps.
        Grid grid = FindFirstObjectByType<Grid>() ?? new GameObject("Grid").AddComponent<Grid>();
        return LayerDefs.ToDictionary(def => def.name, def => GetOrCreateTilemap(grid.transform, def.name, def.order, def.hasCollision, def.merged, def.alias));
    }

    Tilemap GetOrCreateTilemap(Transform parent, string name, int order, bool hasCollision, bool merged, string alias)
    {
        // Support both the current "Ground" name and the older default "Tilemap" name.
        Tilemap tilemap = parent.GetComponentsInChildren<Tilemap>(true).FirstOrDefault(t => t.name == name || t.name == alias);
        GameObject go = tilemap != null ? tilemap.gameObject : new GameObject(name);
        if (tilemap == null) { go.transform.SetParent(parent, false); tilemap = go.AddComponent<Tilemap>(); }
        go.name = name;
        GetOrAdd<TilemapRenderer>(go).sortingOrder = order;
        CompositeCollider2D composite = go.GetComponent<CompositeCollider2D>();

        if (hasCollision)
        {
            // Ground uses merged collision; platforms keep separate colliders.
            GetOrAdd<TilemapCollider2D>(go).compositeOperation = merged ? Collider2D.CompositeOperation.Merge : Collider2D.CompositeOperation.None;
            if (merged) GetOrAdd<CompositeCollider2D>(go); else if (composite != null) DestroyImmediate(composite);
            // Static tilemaps still need a kinematic Rigidbody2D for composite collisions to work correctly.
            Rigidbody2D body = GetOrAdd<Rigidbody2D>(go);
            body.bodyType = RigidbodyType2D.Kinematic; body.simulated = true; body.gravityScale = 0f;
        }
        else
        {
            TilemapCollider2D tilemapCollider = go.GetComponent<TilemapCollider2D>();
            Rigidbody2D body = go.GetComponent<Rigidbody2D>();
            if (tilemapCollider != null) DestroyImmediate(tilemapCollider);
            if (composite != null) DestroyImmediate(composite);
            if (body != null) DestroyImmediate(body);
        }

        return tilemap;
    }

    void PaintLayer(string name, int[] gids, int w, int h, IReadOnlyDictionary<string, Tilemap> tilemaps)
    {
        if (!tilemaps.TryGetValue(name, out Tilemap tilemap)) return;
        // TMX gid 0 means empty; other gids are 1-based, so subtract 1 for our runtime lookup.
        for (int row = 0; row < h; row++)
        for (int col = 0; col < w; col++)
        {
            int gid = gids[row * w + col];
            // Flip the y-axis because TMX rows start at the top while Unity tilemaps grow upward.
            if (gid > 0 && tiles.TryGetValue(gid - 1, out TileBase tile)) tilemap.SetTile(new Vector3Int(col, h - row - 1, 0), tile);
        }
    }

    void CollectGroundSpawns(int[] gids, int w, int h)
    {
        // Only use tiles with open air above them so enemies spawn on walkable surfaces, not inside terrain.
        for (int row = 0; row < h; row++)
        for (int col = 2; col < w - 2; col++)
        {
            int i = row * w + col;
            if (gids[i] > 0 && (row == 0 || gids[(row - 1) * w + col] <= 0)) groundSpawns.Add(new Vector3(col + 0.5f, h - row + 0.5f, 0f));
        }
    }

    IEnumerator AssignMapSpawns(XElement map, int mapHeight, int tileWidth, int tileHeight)
    {
        // Read the player spawn from the TMX object layer and derive enemy spawns from ground geometry.
        Vector3? playerSpawn = map.Elements("objectgroup").Where(g => (string)g.Attribute("name") == "PlayerSpawn").SelectMany(g => g.Elements("object")).Select(o => (Vector3?)ToWorld(o, mapHeight, tileWidth, tileHeight)).FirstOrDefault();
        List<Vector3> enemySpawns = BuildEnemySpawns(playerSpawn);
        yield return WaitForPlayer();

        // Move the spawned player to the map's start point and clear leftover movement.
        if (playerSpawn.HasValue && GameManager.Instance.player != null)
        {
            GameManager.Instance.player.transform.position = playerSpawn.Value;
            Unit unit = GameManager.Instance.player.GetComponent<Unit>();
            if (unit != null) unit.movement = Vector2.zero;
        }

        // Replace any old generated spawn points with the ones inferred from this map.
        EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner == null) yield break;
        Transform root = spawner.transform.Find("GeneratedSpawnPoints") ?? new GameObject("GeneratedSpawnPoints").transform;
        if (root.parent == null) root.SetParent(spawner.transform, false);
        for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);
        spawner.SpawnPoints = enemySpawns.Select((pos, i) => CreateSpawn(root, pos, i)).ToArray();
    }

    SpawnPoint CreateSpawn(Transform parent, Vector3 position, int index)
    {
        // SpawnPoint is the component EnemySpawner already understands, so build those dynamically.
        GameObject go = new($"MapSpawn{index + 1}");
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        SpawnPoint spawn = go.AddComponent<SpawnPoint>();
        spawn.kind = SpawnKinds[index % SpawnKinds.Length];
        return spawn;
    }

    List<Vector3> BuildEnemySpawns(Vector3? playerSpawn)
    {
        if (groundSpawns.Count == 0) return new();
        IEnumerable<Vector3> candidates = groundSpawns;
        if (playerSpawn.HasValue)
        {
            Vector3 player = playerSpawn.Value;
            // Prefer spawns on the same floor as the player, but far enough away to avoid instant contact.
            candidates = candidates.Where(p => Mathf.Abs(p.y - player.y) <= SpawnLevelTolerance && Mathf.Abs(p.x - player.x) >= MinSpawnDistanceFromPlayer).OrderBy(p => Mathf.Abs(p.x - player.x));
        }

        List<Vector3> spawns = DistinctSpawns(candidates).Take(MaxEnemySpawns).ToList();
        // If filtering got too strict, probe nearby ground with raycasts to recover usable spawn points.
        if (playerSpawn.HasValue) AddFallbackSpawns(playerSpawn.Value, spawns);
        if (spawns.Count == 0) spawns.AddRange(groundSpawns.Take(6));
        return DistinctSpawns(spawns).Take(MaxEnemySpawns).ToList();
    }

    // Round to quarter-tile precision so near-identical spawn positions collapse into one spot.
    IEnumerable<Vector3> DistinctSpawns(IEnumerable<Vector3> source) => source.GroupBy(p => new Vector2Int(Mathf.RoundToInt(p.x * 4f), Mathf.RoundToInt(p.y * 4f))).Select(g => g.First());

    void AddFallbackSpawns(Vector3 playerSpawn, ICollection<Vector3> spawns)
    {
        foreach (float offset in FallbackOffsets)
        {
            // Cast downward from several offsets around the player to find valid standing positions.
            Vector2 origin = new(playerSpawn.x + offset, playerSpawn.y + 8f);
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 20f);
            if (hit.collider != null) spawns.Add(new Vector3(origin.x, hit.point.y + 0.6f, 0f));
        }
    }

    IEnumerator WaitForPlayer()
    {
        // The GameManager may spawn/register the player a frame or two later than this bootstrap starts.
        float timeoutAt = Time.realtimeSinceStartup + 2f;
        while (GameManager.Instance.player == null && Time.realtimeSinceStartup < timeoutAt) yield return null;
    }

    XElement ResolveTileset(XElement map)
    {
        // Some TMX files inline tileset info, while others reference an external TSX file.
        XElement tileset = map.Element("tileset");
        if (tileset == null || ReadInt(tileset, "tilecount") > 0 && ReadInt(tileset, "columns") > 0) return tileset;
        string source = (string)tileset.Attribute("source");
        TextAsset asset = string.IsNullOrWhiteSpace(source) ? null : Resources.Load<TextAsset>(TilesetPath);
        return asset == null ? tileset : XDocument.Parse(asset.text).Element("tileset") ?? tileset;
    }

    Texture2D LoadTileTexture()
    {
        // Runtime loads from Resources; the editor fallback helps while assets are being reorganized.
        Texture2D texture = Resources.Load<Texture2D>(TexturePath);
        if (texture != null) return texture;
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Tiles/roguelikeDungeon_transparent.png") ?? AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/Tiles/roguelikeDungeon_transparent.png");
#else
        return null;
#endif
    }

    // Convert TMX object coordinates into Unity world/tilemap space.
    static Vector3 ToWorld(XElement obj, int mapHeight, int tileWidth, int tileHeight) => new(ReadFloat(obj, "x") / tileWidth + 0.5f, mapHeight - ReadFloat(obj, "y") / tileHeight + 0.5f, 0f);
    // TMX CSV data is a flat gid array ordered row-by-row.
    static int[] ParseLayer(XElement layer) => layer.Element("data")?.Value.Split(',').Select(v => v.Trim()).Where(v => v.Length > 0).Select(v => int.Parse(v, CultureInfo.InvariantCulture)).ToArray() ?? System.Array.Empty<int>();
    // Reuse components when possible so repeated rebuilds do not keep adding duplicates.
    static T GetOrAdd<T>(GameObject go) where T : Component => go.GetComponent<T>() ?? go.AddComponent<T>();
    static T LoadResource<T>(params string[] paths) where T : Object { foreach (string path in paths) { T asset = Resources.Load<T>(path); if (asset != null) return asset; } return null; }
    static int ReadInt(XElement e, string name, int fallback = 0) => int.TryParse(e?.Attribute(name)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;
    static float ReadFloat(XElement e, string name, float fallback = 0f) => float.TryParse(e?.Attribute(name)?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : fallback;
    static bool IsSupportedScene() => SupportedScenes.Contains(SceneManager.GetActiveScene().name);

    static void EnsureBootstrapExists()
    {
        // Keep this helper hidden and unique so scenes do not need a manually placed bootstrap object.
        if (!IsSupportedScene() || FindFirstObjectByType<TiledPlatformerMapBootstrap>() != null) return;
        GameObject go = new(nameof(TiledPlatformerMapBootstrap)) { hideFlags = HideFlags.HideAndDontSave };
        go.AddComponent<TiledPlatformerMapBootstrap>();
    }

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    static void RegisterEditorBootstrap()
    {
        // Recreate the hidden bootstrap automatically when scenes open in the editor.
        EditorApplication.delayCall += EnsureEditorBootstrapExists;
        EditorSceneManager.sceneOpened += (_, __) => EnsureEditorBootstrapExists();
    }

    static void EnsureEditorBootstrapExists() { if (!Application.isPlaying) EnsureBootstrapExists(); }
#endif
}
