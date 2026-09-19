using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using GemforgeCascade.UI;

namespace GemforgeCascade.Core
{
    public sealed class BoardManager : MonoBehaviour
    {
        [SerializeField] private BoardConfig config = new BoardConfig();
        [SerializeField] private TextAsset levelAsset;
        [SerializeField, Min(0.01f)] private float animationDuration = 0.16f;
        [SerializeField, Min(0.01f)] private float fallDuration = 0.24f;
        [SerializeField, Min(0.01f)] private float clearDuration = 0.12f;
        public bool Paused { get; private set; }
        public bool ReducedMotion { get; private set; }
        public bool HintsEnabled { get; private set; } = true;
        public bool IsBusy => busy;
        public GameManager Game => game;
        public BoardModel Model => model;
        public LevelDefinition Level => level;
        public int LevelIndex => levelIndex;
        public int LevelCount => chapter.Length;
        private LevelDefinition level;
        private TextAsset[] chapter = new TextAsset[0];
        private int levelIndex;
        private UIManager ui;
        private float idleTime;
        private int pointerId = -2;
        private Piece hintA, hintB;
        private BoardModel model;
        private Piece[,] pieces;
        private GameManager game;
        private Camera boardCamera;
        private Sprite sprite;
        private Texture2D texture;
        private readonly Sprite[] gemSprites = new Sprite[6];
        private readonly Texture2D[] gemTextures = new Texture2D[6];
        private SpriteRenderer[,] sockets;
        private Transform socketRoot;
        private Piece selected;
        private Piece pressed;
        private Vector2 pressPosition;
        private bool busy;
        private readonly Color[] colors = {
            new Color(0.96f, 0.25f, 0.32f), new Color(0.18f, 0.65f, 1f),
            new Color(0.20f, 0.85f, 0.47f), new Color(1f, 0.85f, 0.18f),
            new Color(0.73f, 0.36f, 0.98f), new Color(1f, 0.52f, 0.16f)
        };

        private void Start()
        {
            config.width = Mathf.Clamp(config.width, 3, 16);
            config.height = Mathf.Clamp(config.height, 3, 16);
            config.pieceTypes = Mathf.Clamp(config.pieceTypes, 3, 6);
            config.cellSize = Mathf.Max(0.25f, config.cellSize);
            config.moves = Mathf.Max(1, config.moves);
            config.targetScore = Mathf.Max(1, config.targetScore);
            animationDuration = Mathf.Max(0.01f, animationDuration);
            boardCamera = Camera.main;
            if (boardCamera == null)
            {
                boardCamera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
                boardCamera.tag = "MainCamera";
            }
            boardCamera.orthographic = true;
            boardCamera.transform.position = new Vector3(0, 0, -10);
            boardCamera.clearFlags = CameraClearFlags.SolidColor;
            boardCamera.backgroundColor = new Color(0.065f, 0.075f, 0.085f);
            var background = new GameObject("Backdrop Camera").AddComponent<Camera>();
            background.clearFlags = CameraClearFlags.SolidColor;
            background.backgroundColor = boardCamera.backgroundColor;
            background.cullingMask = 0;
            background.depth = -10;
            game = new GameObject("GameManager").AddComponent<GameManager>();
            game.RestartRequested += Restart;
            ui = new GameObject("UIManager").AddComponent<UIManager>();
            ui.Initialize(game, this);
            chapter = LevelCatalog.LoadAll();
            ReducedMotion = PlayerPrefs.GetInt("gemforge.reducedMotion", 0) == 1;
            HintsEnabled = PlayerPrefs.GetInt("gemforge.hints", 1) == 1;
            CreateSprite();
            CreateGemSprites();
            Restart();
        }

        private void CreateSprite()
        {
            const int size = 64;
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - 31.5f) / 30f, dy = (y - 31.5f) / 30f;
                    float radius = Mathf.Sqrt(dx * dx + dy * dy);
                    float shade = Mathf.Clamp01(0.85f + dy * 0.18f - dx * 0.10f);
                    texture.SetPixel(x, y, new Color(shade, shade, shade, Mathf.Clamp01((1 - radius) * 30)));
                }
            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }

        public void Restart()
        {
            StopAllCoroutines();
            Paused = false;
            busy = true;
            ClearSelection();
            pressed = null;
            pointerId = -2;
            ClearHints();
            if (chapter == null || chapter.Length == 0)
            {
                levelIndex = 0;
            }
            else if (levelIndex < 0 || levelIndex >= chapter.Length)
            {
                levelIndex = 0;
            }

            TextAsset asset = levelAsset != null ? levelAsset : chapter.Length > 0 ? chapter[levelIndex] : null;
            level = asset != null ? LevelCatalog.Parse(asset) : new LevelDefinition
            {
                width = config.width, height = config.height, colorCount = config.pieceTypes,
                moves = config.moves, targetScore = config.targetScore, seed = 42
            };
            var validation = LevelValidator.Validate(level);
            if (!validation.IsValid) throw new System.InvalidOperationException("Invalid level: " + level.id);
            config.width = level.width;
            config.height = level.height;
            model = BoardModel.FromLevel(level);
            RebuildVisuals();
            game.Begin(level);
            idleTime = 0;
            busy = false;
        }

        public void NextLevel()
        {
            if (game.State != GameState.Won || levelAsset != null) return;
            if (chapter == null || levelIndex + 1 >= chapter.Length) return;
            levelIndex++;
            Restart();
        }

        public void SetPaused(bool value)
        {
            Paused = value;
            pressed = null;
            pointerId = -2;
            ClearSelection();
            ClearHints();
            ui.Refresh();
        }

        public void SetReducedMotion(bool value)
        {
            ReducedMotion = value;
            PlayerPrefs.SetInt("gemforge.reducedMotion", value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetHints(bool value)
        {
            HintsEnabled = value;
            ClearHints();
            idleTime = 0;
            PlayerPrefs.SetInt("gemforge.hints", value ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void OnApplicationFocus(bool focused) { if (!focused && game != null) SetPaused(true); }
        private void OnApplicationPause(bool paused) { if (paused && game != null) SetPaused(true); }

        private void RebuildVisuals()
        {
            if (pieces != null)
                foreach (var piece in pieces)
                    if (piece != null) { piece.gameObject.SetActive(false); Destroy(piece.gameObject); }
            pieces = new Piece[model.Width, model.Height];
            for (int y = 0; y < model.Height; y++)
                for (int x = 0; x < model.Width; x++) Spawn(x, y, y, true);
            if (socketRoot != null) { socketRoot.gameObject.SetActive(false); Destroy(socketRoot.gameObject); }
            socketRoot = new GameObject("Board Sockets").transform;
            socketRoot.SetParent(transform);
            sockets = new SpriteRenderer[model.Width, model.Height];
            for (int y = 0; y < model.Height; y++)
                for (int x = 0; x < model.Width; x++)
                {
                    var slot = new GameObject("Socket").AddComponent<SpriteRenderer>();
                    slot.transform.SetParent(socketRoot);
                    slot.transform.position = Position(x, y);
                    slot.transform.localScale = Vector3.one * config.cellSize;
                    slot.sprite = sprite;
                    slot.sortingOrder = 0;
                    sockets[x, y] = slot;
                }
            RefreshLayers();
        }

        private void RefreshLayers()
        {
            for (int y = 0; y < model.Height; y++)
                for (int x = 0; x < model.Width; x++)
                {
                    int hits = model.Layers[x, y].Durability;
                    sockets[x, y].color = hits > 1 ? new Color(0.72f, 0.88f, 1f) :
                        hits == 1 ? new Color(0.26f, 0.56f, 0.65f) : new Color(0.16f, 0.19f, 0.21f);
                }
        }

        private void CreateGemSprites()
        {
            for (int type = 0; type < 6; type++)
            {
                const int size = 96;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float px = (x - 47.5f) / 44, py = (y - 47.5f) / 44;
                        float ax = Mathf.Abs(px), ay = Mathf.Abs(py);
                        float edge;
                        switch (type)
                        {
                            case 0: edge = ax + ay; break;
                            case 1: edge = Mathf.Max(ax * 0.866f + ay * 0.5f, ay); break;
                            case 2: edge = Mathf.Max(Mathf.Max(ax, ay), (ax + ay) / 1.65f); break;
                            case 3: edge = Mathf.Max(-py / 0.72f, (ax * 1.73f + py) / 1.05f); break;
                            case 4: edge = Mathf.Max(ax / 0.65f, Mathf.Max(ay, ax + ay * 0.7f)); break;
                            default: edge = Mathf.Sqrt(px * px + py * py); break;
                        }
                        float shade = edge > 0.78f ? 0.55f + py * 0.22f : 0.9f + py * 0.12f - px * 0.1f;
                        if (px + py > 0.4f && edge < 0.78f) shade = 1;
                        tex.SetPixel(x, y, new Color(shade, shade, shade, Mathf.Clamp01((1 - edge) * 44)));
                    }
                tex.Apply();
                gemTextures[type] = tex;
                gemSprites[type] = Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
            }
        }

        private void Update()
        {
            if (boardCamera == null) return;
            Rect safe = Screen.safeArea;
            boardCamera.pixelRect = new Rect(safe.x, safe.y + safe.height * 0.04f, safe.width, safe.height * 0.72f);
            float aspect = boardCamera.pixelRect.width / Mathf.Max(1, boardCamera.pixelRect.height);
            boardCamera.orthographicSize = Mathf.Max(config.height * config.cellSize / 2 + 0.5f,
                (config.width * config.cellSize / 2 + 0.5f) / aspect);
            if (Input.GetKeyDown(KeyCode.Escape)) SetPaused(!Paused);
            if (busy || Paused || game.State != GameState.Playing) { pressed = null; return; }
            idleTime += Time.deltaTime;
            if (HintsEnabled && idleTime > 6 && selected == null && pressed == null && hintA == null &&
                model.TryGetLegalMove(out int hintFrom, out int hintTo))
            {
                hintA = pieces[hintFrom % model.Width, hintFrom / model.Width];
                hintB = pieces[hintTo % model.Width, hintTo / model.Width];
                hintA.SetHint(true); hintB.SetHint(true);
            }

            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    if (touch.phase == TouchPhase.Began && pointerId == -2) BeginPointer(touch.position, touch.fingerId);
                    if (touch.fingerId != pointerId) continue;
                    if (touch.phase == TouchPhase.Canceled) { pressed = null; pointerId = -2; }
                    else if (touch.phase == TouchPhase.Ended) EndPointer(touch.position);
                    else DragPointer(touch.position);
                }
                return;
            }
            if (pointerId >= 0) { pointerId = -2; pressed = null; }
            if (Input.GetMouseButtonDown(0)) BeginPointer(Input.mousePosition, -1);
            if (Input.GetMouseButton(0)) DragPointer(Input.mousePosition);
            if (Input.GetMouseButtonUp(0)) EndPointer(Input.mousePosition);
        }

        private void BeginPointer(Vector2 position, int id)
        {
            idleTime = 0; ClearHints();
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(id)) return;
            pointerId = id;
            pressPosition = position;
            pressed = Hit(position);
            if (pressed == null) ClearSelection();
        }

        private void DragPointer(Vector2 position)
        {
            if (pressed == null) return;
            Vector2 delta = position - pressPosition;
            float pixelsPerCell = config.cellSize * boardCamera.pixelHeight / (boardCamera.orthographicSize * 2);
            if (delta.magnitude < Mathf.Max(12, pixelsPerCell * 0.25f)) return;
            float ax = Mathf.Abs(delta.x), ay = Mathf.Abs(delta.y);
            if (Mathf.Max(ax, ay) < Mathf.Min(ax, ay) * 1.4f) return;
            var source = pressed;
            pressed = null;
            int tx = source.GridPosition.x + (ax > ay ? (delta.x > 0 ? 1 : -1) : 0);
            int ty = source.GridPosition.y + (ay > ax ? (delta.y > 0 ? 1 : -1) : 0);
            if (model.Contains(tx, ty)) Attempt(source, pieces[tx, ty]);
        }

        private void EndPointer(Vector2 position)
        {
            DragPointer(position);
            var clicked = pressed;
            pressed = null; pointerId = -2;
            if (clicked == null || Hit(position) != clicked || (position - pressPosition).magnitude > 12) return;
            if (clicked == selected) { ClearSelection(); return; }
            if (selected != null && Adjacent(selected, clicked)) { Attempt(selected, clicked); return; }
            ClearSelection();
            selected = clicked;
            selected.SetSelected(true);
        }

        private void ClearHints()
        {
            if (hintA != null) hintA.SetHint(false);
            if (hintB != null) hintB.SetHint(false);
            hintA = hintB = null;
        }

        public bool AttemptMove(int from, int to)
        {
            if (busy || Paused || game.State != GameState.Playing || from < 0 || to < 0 ||
                from >= model.Width * model.Height || to >= model.Width * model.Height) return false;
            var a = pieces[from % model.Width, from / model.Width];
            var b = pieces[to % model.Width, to / model.Width];
            if (!Adjacent(a, b)) return false;
            Attempt(a, b);
            return true;
        }

        private Piece Hit(Vector2 screen)
        {
            if (!boardCamera.pixelRect.Contains(screen)) return null;
            Vector3 world = boardCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10));
            int x = Mathf.RoundToInt(world.x / config.cellSize + (model.Width - 1) * 0.5f);
            int y = Mathf.RoundToInt(world.y / config.cellSize + (model.Height - 1) * 0.5f);
            return model.Contains(x, y) ? pieces[x, y] : null;
        }

        private static bool Adjacent(Piece a, Piece b) =>
            Mathf.Abs(a.GridPosition.x - b.GridPosition.x) + Mathf.Abs(a.GridPosition.y - b.GridPosition.y) == 1;

        private void ClearSelection()
        {
            if (selected != null) selected.SetSelected(false);
            selected = null;
        }

        private void Attempt(Piece a, Piece b)
        {
            if (busy || Paused || game.State != GameState.Playing || !Adjacent(a, b)) return;
            ClearHints(); idleTime = 0;
            ClearSelection();
            busy = true;
            StartCoroutine(SwapAndResolve(a, b));
        }

        private IEnumerator SwapAndResolve(Piece a, Piece b)
        {
            var from = a.GridPosition;
            var to = b.GridPosition;
            bool accepted = model.TrySwap(from.x, from.y, to.x, to.y, out MatchResolution resolution);
            if (!accepted) model.Swap(from.x, from.y, to.x, to.y);
            SwapVisuals(a, b);
            yield return AnimateBoard(animationDuration);
            if (!accepted)
            {
                model.Swap(from.x, from.y, to.x, to.y);
                SwapVisuals(a, b);
                yield return AnimateBoard(animationDuration * 0.75f);
                busy = false;
                yield break;
            }
            game.ConsumeMove();
            int multiplier = 1;
            while (resolution != null && resolution.AffectedCells.Count > 0)
            {
                float duration = ReducedMotion ? 0.05f : clearDuration;
                for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
                {
                    while (Paused) yield return null;
                    foreach (int id in resolution.RemovedCells)
                        pieces[id % model.Width, id / model.Width].Shrink(elapsed / duration);
                    yield return null;
                }
                foreach (int id in resolution.RemovedCells)
                {
                    int x = id % model.Width, y = id / model.Width;
                    Destroy(pieces[x, y].gameObject);
                    pieces[x, y] = null;
                }
                ClearResult clear = model.ApplyMatchResolution(resolution);
                game.Award(clear, multiplier++);
                foreach (var creation in resolution.CreatedPieces)
                {
                    int x = creation.Key % model.Width;
                    int y = creation.Key / model.Width;
                    pieces[x, y].SetState(creation.Value);
                }
                int[,] sources = model.CollapseAndRefill();
                var previous = pieces;
                pieces = new Piece[model.Width, model.Height];
                for (int x = 0; x < model.Width; x++)
                {
                    int spawnRow = model.Height;
                    for (int y = 0; y < model.Height; y++)
                    {
                        if (sources[x, y] < 0) Spawn(x, y, spawnRow++, false);
                        else
                        {
                            pieces[x, y] = previous[x, sources[x, y]];
                            pieces[x, y].Place(x, y, Position(x, y), false);
                        }
                    }
                }
                RefreshLayers();
                yield return AnimateBoard(fallDuration);
                var matches = model.FindMatches();
                resolution = matches.Count > 0 ? model.PlanMatchResolution(matches) : null;
            }
            game.FinishTurn();
            if (game.State == GameState.Playing && !model.HasMove())
            {
                model.Generate();
                RebuildVisuals();
            }
            busy = false;
            idleTime = 0;
        }

        private void SwapVisuals(Piece a, Piece b)
        {
            var first = a.GridPosition;
            var second = b.GridPosition;
            pieces[first.x, first.y] = b; pieces[second.x, second.y] = a;
            a.Place(second.x, second.y, Position(second.x, second.y), false);
            b.Place(first.x, first.y, Position(first.x, first.y), false);
        }

        private IEnumerator AnimateBoard(float duration)
        {
            duration = ReducedMotion ? 0.05f : Mathf.Max(0.01f, duration);
            for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
            {
                while (Paused) yield return null;
                foreach (var piece in pieces) piece.Animate(elapsed / duration);
                yield return null;
            }
            foreach (var piece in pieces) piece.Animate(1);
        }

        private Vector3 Position(int x, int y) => new Vector3(
            (x - (model.Width - 1) * 0.5f) * config.cellSize,
            (y - (model.Height - 1) * 0.5f) * config.cellSize, 0);

        private void Spawn(int x, int y, int startY, bool instant)
        {
            var piece = new GameObject("Piece").AddComponent<Piece>();
            piece.transform.SetParent(transform);
            piece.transform.position = Position(x, startY);
            BoardPiece state = model.Cells[x, y];
            piece.Initialize(state, gemSprites[state.ColorIndex], colors[state.ColorIndex], config.cellSize);
            piece.Place(x, y, Position(x, y), instant);
            pieces[x, y] = piece;
        }

        private void OnDestroy()
        {
            if (game != null) game.RestartRequested -= Restart;
            if (sprite != null) Destroy(sprite);
            if (texture != null) Destroy(texture);
            for (int i = 0; i < gemSprites.Length; i++)
            {
                if (gemSprites[i] != null) Destroy(gemSprites[i]);
                if (gemTextures[i] != null) Destroy(gemTextures[i]);
            }
        }
    }
}
