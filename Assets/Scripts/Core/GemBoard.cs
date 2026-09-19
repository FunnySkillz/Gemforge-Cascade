using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GemforgeCascade.Core
{
    public sealed class GemBoard : MonoBehaviour
    {
        [Header("Board")]
        [SerializeField] private int width = 8;
        [SerializeField] private int height = 8;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Gem gemPrefab;

        [Header("Flow")]
        [SerializeField] private float cascadeDelay = 0.08f;

        public UnityEvent<int> ScoreChanged = new UnityEvent<int>();
        public UnityEvent<int> MovesChanged = new UnityEvent<int>();

        private readonly Color[] palette =
        {
            new Color(0.95f, 0.23f, 0.20f),
            new Color(0.12f, 0.66f, 0.95f),
            new Color(0.98f, 0.86f, 0.18f),
            new Color(0.20f, 0.82f, 0.40f),
            new Color(0.70f, 0.90f, 1.00f),
            new Color(0.50f, 0.24f, 0.82f)
        };

        private Gem[,] gems;
        private Camera mainCamera;
        private Gem selectedGem;
        private int score;
        private int moves;
        private bool resolving;

        private void Awake()
        {
            mainCamera = Camera.main;
            gems = new Gem[width, height];
        }

        private void Start()
        {
            BuildBoard();
            ScoreChanged.Invoke(score);
            MovesChanged.Invoke(moves);
        }

        private void Update()
        {
            if (resolving || !Input.GetMouseButtonDown(0))
            {
                return;
            }

            TrySelectGem();
        }

        private void BuildBoard()
        {
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    SpawnGem(x, y, PickStartingType(x, y));
                }
            }
        }

        private GemType PickStartingType(int x, int y)
        {
            var type = RandomType();
            var guard = 0;
            while (guard < 20 && WouldCreateMatch(x, y, type))
            {
                type = RandomType();
                guard++;
            }

            return type;
        }

        private bool WouldCreateMatch(int x, int y, GemType type)
        {
            var horizontal = x >= 2 && gems[x - 1, y].Type == type && gems[x - 2, y].Type == type;
            var vertical = y >= 2 && gems[x, y - 1].Type == type && gems[x, y - 2].Type == type;
            return horizontal || vertical;
        }

        private void TrySelectGem()
        {
            var world = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            var hit = Physics2D.OverlapPoint(world);
            if (hit == null || !hit.TryGetComponent(out Gem gem))
            {
                ClearSelection();
                return;
            }

            if (selectedGem == null)
            {
                Select(gem);
                return;
            }

            if (selectedGem == gem)
            {
                ClearSelection();
                return;
            }

            if (AreAdjacent(selectedGem.GridPosition, gem.GridPosition))
            {
                StartCoroutine(TrySwap(selectedGem, gem));
                ClearSelection();
                return;
            }

            Select(gem);
        }

        private IEnumerator TrySwap(Gem a, Gem b)
        {
            resolving = true;
            Swap(a, b);
            var matches = FindMatches();
            if (matches.Count == 0)
            {
                yield return new WaitForSeconds(cascadeDelay);
                Swap(a, b);
                resolving = false;
                yield break;
            }

            moves++;
            MovesChanged.Invoke(moves);
            yield return ResolveMatches(matches);
            resolving = false;
        }

        private IEnumerator ResolveMatches(HashSet<Gem> matches)
        {
            while (matches.Count > 0)
            {
                score += matches.Count * 10;
                ScoreChanged.Invoke(score);

                foreach (var gem in matches)
                {
                    gems[gem.GridPosition.x, gem.GridPosition.y] = null;
                    Destroy(gem.gameObject);
                }

                yield return new WaitForSeconds(cascadeDelay);
                CollapseColumns();
                FillBoard();
                yield return new WaitForSeconds(cascadeDelay);
                matches = FindMatches();
            }
        }

        private void CollapseColumns()
        {
            for (var x = 0; x < width; x++)
            {
                var writeY = 0;
                for (var y = 0; y < height; y++)
                {
                    var gem = gems[x, y];
                    if (gem == null)
                    {
                        continue;
                    }

                    gems[x, y] = null;
                    gems[x, writeY] = gem;
                    gem.SetGridPosition(new Vector2Int(x, writeY), GridToWorld(x, writeY));
                    writeY++;
                }
            }
        }

        private void FillBoard()
        {
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (gems[x, y] == null)
                    {
                        SpawnGem(x, y, RandomType());
                    }
                }
            }
        }

        private HashSet<Gem> FindMatches()
        {
            var matches = new HashSet<Gem>();

            for (var y = 0; y < height; y++)
            {
                var runStart = 0;
                for (var x = 1; x <= width; x++)
                {
                    if (x < width && gems[x, y].Type == gems[runStart, y].Type)
                    {
                        continue;
                    }

                    AddRun(matches, runStart, y, x - runStart, true);
                    runStart = x;
                }
            }

            for (var x = 0; x < width; x++)
            {
                var runStart = 0;
                for (var y = 1; y <= height; y++)
                {
                    if (y < height && gems[x, y].Type == gems[x, runStart].Type)
                    {
                        continue;
                    }

                    AddRun(matches, x, runStart, y - runStart, false);
                    runStart = y;
                }
            }

            return matches;
        }

        private void AddRun(HashSet<Gem> matches, int x, int y, int length, bool horizontal)
        {
            if (length < 3)
            {
                return;
            }

            for (var i = 0; i < length; i++)
            {
                matches.Add(horizontal ? gems[x + i, y] : gems[x, y + i]);
            }
        }

        private void SpawnGem(int x, int y, GemType type)
        {
            var gem = Instantiate(gemPrefab, GridToWorld(x, y), Quaternion.identity, transform);
            gem.Initialize(type, new Vector2Int(x, y), palette[(int)type]);
            gems[x, y] = gem;
        }

        private void Swap(Gem a, Gem b)
        {
            var aPos = a.GridPosition;
            var bPos = b.GridPosition;
            gems[aPos.x, aPos.y] = b;
            gems[bPos.x, bPos.y] = a;
            a.SetGridPosition(bPos, GridToWorld(bPos.x, bPos.y));
            b.SetGridPosition(aPos, GridToWorld(aPos.x, aPos.y));
        }

        private Vector3 GridToWorld(int x, int y)
        {
            return new Vector3((x - (width - 1) * 0.5f) * cellSize, (y - (height - 1) * 0.5f) * cellSize, 0f);
        }

        private GemType RandomType()
        {
            return (GemType)Random.Range(0, palette.Length);
        }

        private static bool AreAdjacent(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
        }

        private void Select(Gem gem)
        {
            ClearSelection();
            selectedGem = gem;
            selectedGem.SetSelected(true);
        }

        private void ClearSelection()
        {
            if (selectedGem != null)
            {
                selectedGem.SetSelected(false);
            }

            selectedGem = null;
        }
    }
}
