using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using GemforgeCascade.UI;

namespace GemforgeCascade.Core
{
    public sealed class BoardManager : MonoBehaviour
    {
        [SerializeField] private BoardConfig config = new BoardConfig();
        [SerializeField, Min(0.01f)] private float animationDuration = 0.2f;
        private BoardModel model;
        private Piece[,] pieces;
        private GameManager game;
        private Camera boardCamera;
        private Sprite sprite;
        private Texture2D texture;
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
            game = new GameObject("GameManager").AddComponent<GameManager>();
            game.RestartRequested += Restart;
            new GameObject("UIManager").AddComponent<UIManager>().Initialize(game);
            CreateSprite();
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

        private void Restart()
        {
            StopAllCoroutines();
            busy = true;
            ClearSelection();
            pressed = null;
            model = new BoardModel(config.width, config.height, config.pieceTypes);
            RebuildVisuals();
            game.Begin(config);
            busy = false;
        }

        private void RebuildVisuals()
        {
            if (pieces != null)
                foreach (var piece in pieces)
                    if (piece != null) { piece.gameObject.SetActive(false); Destroy(piece.gameObject); }
            pieces = new Piece[model.Width, model.Height];
            for (int y = 0; y < model.Height; y++)
                for (int x = 0; x < model.Width; x++) Spawn(x, y, y, true);
        }

        private void Update()
        {
            if (boardCamera == null) return;
            // Reserve the upper fifth of the screen for the HUD on any aspect ratio.
            boardCamera.rect = new Rect(0, 0, 1, 0.80f);
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height * 0.80f);
            boardCamera.orthographicSize = Mathf.Max(config.height * config.cellSize / 2 + 0.5f,
                (config.width * config.cellSize / 2 + 0.5f) / aspect);
            if (busy || game.State != GameState.Playing) { pressed = null; return; }
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current != null && (EventSystem.current.IsPointerOverGameObject() ||
                    (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)))) return;
                pressPosition = Input.mousePosition;
                pressed = Hit(pressPosition);
            }
            if (pressed != null && Input.GetMouseButton(0))
            {
                Vector2 delta = (Vector2)Input.mousePosition - pressPosition;
                if (delta.magnitude >= Mathf.Max(16, Screen.height * 0.025f))
                {
                    var source = pressed;
                    pressed = null;
                    float ax = Mathf.Abs(delta.x), ay = Mathf.Abs(delta.y);
                    // Ambiguous diagonal gestures do not turn into accidental swaps.
                    if (Mathf.Max(ax, ay) < Mathf.Min(ax, ay) * 1.4f) return;
                    int tx = source.GridPosition.x + (ax > ay ? (delta.x > 0 ? 1 : -1) : 0);
                    int ty = source.GridPosition.y + (ay > ax ? (delta.y > 0 ? 1 : -1) : 0);
                    if (model.Contains(tx, ty)) Attempt(source, pieces[tx, ty]);
                }
            }
            if (Input.GetMouseButtonUp(0))
            {
                var clicked = pressed;
                pressed = null;
                if (clicked == null) return;
                if (clicked == selected) { ClearSelection(); return; }
                if (selected != null && Adjacent(selected, clicked)) { Attempt(selected, clicked); return; }
                ClearSelection();
                selected = clicked;
                selected.SetSelected(true);
            }
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
            if (busy || !Adjacent(a, b)) return;
            ClearSelection();
            busy = true;
            StartCoroutine(SwapAndResolve(a, b));
        }

        private IEnumerator SwapAndResolve(Piece a, Piece b)
        {
            var from = a.GridPosition;
            var to = b.GridPosition;
            model.Swap(from.x, from.y, to.x, to.y);
            SwapVisuals(a, b);
            yield return AnimateBoard();
            var matches = model.FindMatches();
            if (matches.Count == 0)
            {
                model.Swap(from.x, from.y, to.x, to.y);
                SwapVisuals(a, b);
                yield return AnimateBoard();
                busy = false;
                yield break;
            }
            game.ConsumeMove();
            int multiplier = 1;
            int preferredCreationCell = to.y * model.Width + to.x;
            while (matches.Count > 0)
            {
                MatchResolution resolution = model.PlanMatchResolution(matches, preferredCreationCell);
                preferredCreationCell = -1;
                for (float elapsed = 0; elapsed < animationDuration; elapsed += Time.deltaTime)
                {
                    foreach (int id in resolution.RemovedCells)
                        pieces[id % model.Width, id / model.Width].Shrink(elapsed / animationDuration);
                    yield return null;
                }
                foreach (int id in resolution.RemovedCells)
                {
                    int x = id % model.Width, y = id / model.Width;
                    Destroy(pieces[x, y].gameObject);
                    pieces[x, y] = null;
                }
                ClearResult clear = model.ApplyMatchResolution(resolution);
                game.Award(clear.PieceCount, multiplier++);
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
                yield return AnimateBoard();
                yield return new WaitForSeconds(0.08f);
                matches = model.FindMatches();
            }
            game.FinishTurn();
            if (game.State == GameState.Playing && !model.HasMove())
            {
                model.Generate();
                RebuildVisuals();
            }
            busy = false;
        }

        private void SwapVisuals(Piece a, Piece b)
        {
            var first = a.GridPosition;
            var second = b.GridPosition;
            pieces[first.x, first.y] = b; pieces[second.x, second.y] = a;
            a.Place(second.x, second.y, Position(second.x, second.y), false);
            b.Place(first.x, first.y, Position(first.x, first.y), false);
        }

        private IEnumerator AnimateBoard()
        {
            for (float elapsed = 0; elapsed < animationDuration; elapsed += Time.deltaTime)
            {
                foreach (var piece in pieces) piece.Animate(elapsed / animationDuration);
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
            piece.Initialize(state, sprite, colors[state.ColorIndex], config.cellSize);
            piece.Place(x, y, Position(x, y), instant);
            pieces[x, y] = piece;
        }

        private void OnDestroy()
        {
            if (game != null) game.RestartRequested -= Restart;
            if (sprite != null) Destroy(sprite);
            if (texture != null) Destroy(texture);
        }
    }
}
