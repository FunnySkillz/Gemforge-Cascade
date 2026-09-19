using System;
using System.Collections.Generic;

namespace GemforgeCascade.Core
{
    public enum PieceColor
    {
        Red,
        Blue,
        Green,
        Yellow,
        Purple,
        Orange
    }

    public enum SpecialKind
    {
        None,
        RowClear,
        ColumnClear,
        Blast,
        ColorClear
    }

    public enum MatchDirection
    {
        Horizontal,
        Vertical
    }

    public enum CellLayerKind
    {
        None,
        Crystal
    }

    public struct CellLayer : IEquatable<CellLayer>
    {
        public static readonly CellLayer Empty = new CellLayer(CellLayerKind.None, 0);

        public CellLayerKind Kind { get; }
        public int Durability { get; }
        public bool IsEmpty => Kind == CellLayerKind.None || Durability <= 0;

        public CellLayer(CellLayerKind kind, int durability)
        {
            if (durability < 0)
                throw new ArgumentOutOfRangeException(nameof(durability));
            if (kind == CellLayerKind.None || durability == 0)
            {
                kind = CellLayerKind.None;
                durability = 0;
            }
            Kind = kind;
            Durability = durability;
        }

        public CellLayer Damage(out bool cleared)
        {
            cleared = false;
            if (IsEmpty)
                return Empty;
            if (Durability == 1)
            {
                cleared = true;
                return Empty;
            }
            return new CellLayer(Kind, Durability - 1);
        }

        public bool Equals(CellLayer other) => Kind == other.Kind && Durability == other.Durability;
        public override bool Equals(object obj) => obj is CellLayer other && Equals(other);
        public override int GetHashCode() => ((int)Kind * 397) ^ Durability;
        public static bool operator ==(CellLayer left, CellLayer right) => left.Equals(right);
        public static bool operator !=(CellLayer left, CellLayer right) => !left.Equals(right);
    }

    public sealed class ClearResult
    {
        private readonly int[] removedByColor;

        public int PieceCount { get; internal set; }
        public int LayersCleared { get; internal set; }

        internal ClearResult(int colorCount)
        {
            removedByColor = new int[colorCount];
        }

        internal void AddColor(int colorIndex)
        {
            removedByColor[colorIndex]++;
        }

        public int RemovedColor(int colorIndex) =>
            colorIndex >= 0 && colorIndex < removedByColor.Length ? removedByColor[colorIndex] : 0;
    }

    public struct BoardPiece : IEquatable<BoardPiece>
    {
        public static readonly BoardPiece Empty = new BoardPiece(-1, SpecialKind.None);

        public int ColorIndex { get; }
        public SpecialKind Special { get; }
        public bool IsEmpty => ColorIndex < 0;

        public BoardPiece(int colorIndex, SpecialKind special = SpecialKind.None)
        {
            ColorIndex = colorIndex;
            Special = special;
        }

        public bool Equals(BoardPiece other) => ColorIndex == other.ColorIndex && Special == other.Special;
        public override bool Equals(object obj) => obj is BoardPiece other && Equals(other);
        public override int GetHashCode() => (ColorIndex * 397) ^ (int)Special;
        public static bool operator ==(BoardPiece left, BoardPiece right) => left.Equals(right);
        public static bool operator !=(BoardPiece left, BoardPiece right) => !left.Equals(right);
    }

    public sealed class MatchGroup
    {
        public int ColorIndex { get; }
        public MatchDirection Direction { get; }
        public IReadOnlyList<int> CellIds { get; }
        public int Length => CellIds.Count;

        public MatchGroup(int colorIndex, MatchDirection direction, IReadOnlyList<int> cellIds)
        {
            ColorIndex = colorIndex;
            Direction = direction;
            CellIds = cellIds;
        }
    }

    public sealed class MatchResult
    {
        private readonly HashSet<int> cells = new HashSet<int>();
        private readonly List<MatchGroup> groups = new List<MatchGroup>();

        public IReadOnlyCollection<int> Cells => cells;
        public IReadOnlyList<MatchGroup> Groups => groups;
        public int Count => cells.Count;

        internal void Add(MatchGroup group)
        {
            groups.Add(group);
            foreach (int cellId in group.CellIds)
                cells.Add(cellId);
        }
    }

    public sealed class MatchResolution
    {
        private readonly HashSet<int> affectedCells;
        private readonly HashSet<int> removedCells;
        private readonly Dictionary<int, BoardPiece> createdPieces;

        public IReadOnlyCollection<int> AffectedCells => affectedCells;
        public IReadOnlyCollection<int> RemovedCells => removedCells;
        public IReadOnlyDictionary<int, BoardPiece> CreatedPieces => createdPieces;

        internal MatchResolution(
            HashSet<int> affectedCells,
            HashSet<int> removedCells,
            Dictionary<int, BoardPiece> createdPieces)
        {
            this.affectedCells = affectedCells;
            this.removedCells = removedCells;
            this.createdPieces = createdPieces;
        }
    }

    // Pure board rules, independent of scene objects and animation timing.
    public sealed class BoardModel
    {
        public int Width { get; }
        public int Height { get; }
        public int ColorCount => colorCount;
        public BoardPiece[,] Cells { get; }
        public CellLayer[,] Layers { get; }
        private readonly int colorCount;
        private readonly DeterministicRandom random;

        public uint RandomState => random.State;

        public BoardModel(int width, int height, int colorCount, int? seed = null)
            : this(width, height, colorCount, CreateSeed(seed), true)
        {
        }

        private BoardModel(int width, int height, int colorCount, uint seed, bool generate)
        {
            if (width < 3 || height < 3)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (colorCount < 3 || colorCount > 6)
                throw new ArgumentOutOfRangeException(nameof(colorCount));

            Width = width;
            Height = height;
            this.colorCount = colorCount;
            random = new DeterministicRandom(seed);
            Cells = new BoardPiece[width, height];
            Layers = new CellLayer[width, height];
            if (generate)
                Generate();
        }

        private static uint CreateSeed(int? seed) => seed.HasValue
            ? unchecked((uint)seed.Value)
            : unchecked((uint)(Environment.TickCount ^ Guid.NewGuid().GetHashCode()));

        public static BoardModel FromLevel(LevelDefinition source)
        {
            LevelDefinition level = GameDataMigrations.Upgrade(source);
            var board = new BoardModel(
                level.width,
                level.height,
                level.colorCount,
                unchecked((uint)level.seed),
                false);

            if (level.startingPieces == null || level.startingPieces.Length == 0)
            {
                board.Generate();
            }
            else
            {
                if (level.startingPieces.Length != level.width * level.height)
                    throw new InvalidOperationException("Starting piece count must match level dimensions.");

                for (int id = 0; id < level.startingPieces.Length; id++)
                {
                    BoardPiece piece = level.startingPieces[id].ToBoardPiece();
                    if (piece.IsEmpty || piece.ColorIndex >= level.colorCount)
                        throw new InvalidOperationException($"Invalid starting piece at cell {id}.");
                    board.Cells[id % level.width, id / level.width] = piece;
                }
            }

            if (level.startingLayers.Length != 0 && level.startingLayers.Length != level.width * level.height)
                throw new InvalidOperationException("Starting layer count must match level dimensions.");
            for (int id = 0; id < level.startingLayers.Length; id++)
                board.Layers[id % level.width, id / level.width] = level.startingLayers[id].ToCellLayer();

            return board;
        }

        public static BoardModel FromSnapshot(BoardSnapshot source)
        {
            BoardSnapshot snapshot = GameDataMigrations.Upgrade(source);
            if (snapshot.randomState == 0)
                throw new InvalidOperationException("Snapshot random state must be non-zero.");
            if (snapshot.pieces == null || snapshot.pieces.Length != snapshot.width * snapshot.height)
                throw new InvalidOperationException("Snapshot piece count must match board dimensions.");
            if (snapshot.layers.Length != 0 && snapshot.layers.Length != snapshot.width * snapshot.height)
                throw new InvalidOperationException("Snapshot layer count must match board dimensions.");

            var board = new BoardModel(
                snapshot.width,
                snapshot.height,
                snapshot.colorCount,
                snapshot.randomState,
                false);
            for (int id = 0; id < snapshot.pieces.Length; id++)
            {
                BoardPiece piece = snapshot.pieces[id].ToBoardPiece();
                if (!piece.IsEmpty && piece.ColorIndex >= snapshot.colorCount)
                    throw new InvalidOperationException($"Invalid snapshot piece at cell {id}.");
                board.Cells[id % snapshot.width, id / snapshot.width] = piece;
            }
            for (int id = 0; id < snapshot.layers.Length; id++)
                board.Layers[id % snapshot.width, id / snapshot.width] = snapshot.layers[id].ToCellLayer();

            return board;
        }

        public BoardSnapshot CreateSnapshot(string levelId)
        {
            var snapshot = new BoardSnapshot
            {
                levelId = levelId ?? string.Empty,
                width = Width,
                height = Height,
                colorCount = colorCount,
                randomState = RandomState,
                pieces = new PieceData[Width * Height],
                layers = new CellLayerData[Width * Height]
            };

            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    int id = y * Width + x;
                    snapshot.pieces[id] = new PieceData(Cells[x, y]);
                    snapshot.layers[id] = new CellLayerData(Layers[x, y]);
                }

            return snapshot;
        }

        public BoardPiece RandomPiece() => new BoardPiece(random.Next(colorCount));
        public void RestoreRandomState(uint state) => random.Restore(state);
        public bool Contains(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public void Generate()
        {
            for (int attempt = 0; attempt < 100; attempt++)
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        BoardPiece piece = RandomPiece();
                        // At most two colors are forbidden, so a safe color always exists.
                        while (WouldCreateStartingMatch(x, y, piece.ColorIndex))
                            piece = new BoardPiece((piece.ColorIndex + 1) % colorCount);
                        Cells[x, y] = piece;
                    }
                }

                if (HasMove())
                    return;
            }

            // Bounded fallback with a guaranteed bottom-left swap and no starting runs.
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    Cells[x, y] = new BoardPiece((x + y) % colorCount);

            Cells[0, 0] = new BoardPiece(0);
            Cells[1, 0] = new BoardPiece(1);
            Cells[2, 0] = new BoardPiece(0);
            Cells[1, 1] = new BoardPiece(0);
        }

        private bool WouldCreateStartingMatch(int x, int y, int colorIndex)
        {
            bool horizontal = x >= 2 &&
                Cells[x - 1, y].ColorIndex == colorIndex && Cells[x - 2, y].ColorIndex == colorIndex;
            bool vertical = y >= 2 &&
                Cells[x, y - 1].ColorIndex == colorIndex && Cells[x, y - 2].ColorIndex == colorIndex;
            return horizontal || vertical;
        }

        public void Swap(int x, int y, int targetX, int targetY)
        {
            BoardPiece value = Cells[x, y];
            Cells[x, y] = Cells[targetX, targetY];
            Cells[targetX, targetY] = value;
        }

        public bool TrySwap(int x, int y, int targetX, int targetY)
        {
            return TrySwap(x, y, targetX, targetY, out _);
        }

        // The first resolution is shared by the player, hints, validator and simulator.
        public bool TrySwap(int x, int y, int targetX, int targetY, out MatchResolution resolution)
        {
            resolution = null;
            if (!Contains(x, y) || !Contains(targetX, targetY) ||
                Math.Abs(x - targetX) + Math.Abs(y - targetY) != 1 ||
                Cells[x, y].IsEmpty || Cells[targetX, targetY].IsEmpty)
                return false;

            Swap(x, y, targetX, targetY);
            resolution = PlanCombination(y * Width + x, targetY * Width + targetX);
            if (resolution != null)
                return true;

            MatchResult matches = FindMatches();
            bool involvesSwap = false;
            foreach (int id in matches.Cells)
                if (id == y * Width + x || id == targetY * Width + targetX) involvesSwap = true;
            if (involvesSwap)
            {
                resolution = PlanMatchResolution(matches, targetY * Width + targetX);
                return true;
            }

            Swap(x, y, targetX, targetY);
            return false;
        }

        public MatchResult FindMatches()
        {
            var result = new MatchResult();

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width;)
                {
                    int end = x + 1;
                    while (end < Width && SameColor(Cells[x, y], Cells[end, y]))
                        end++;
                    AddGroup(result, x, y, end - x, MatchDirection.Horizontal);
                    x = end;
                }
            }

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height;)
                {
                    int end = y + 1;
                    while (end < Height && SameColor(Cells[x, y], Cells[x, end]))
                        end++;
                    AddGroup(result, x, y, end - y, MatchDirection.Vertical);
                    y = end;
                }
            }

            return result;
        }

        public MatchResolution PlanMatchResolution(MatchResult matches, int preferredCreationCell = -1)
        {
            if (matches == null)
                throw new ArgumentNullException(nameof(matches));

            var affected = new HashSet<int>(matches.Cells);
            var created = new Dictionary<int, BoardPiece>();
            var consumedGroups = new HashSet<MatchGroup>();

            for (int firstIndex = 0; firstIndex < matches.Groups.Count; firstIndex++)
            {
                MatchGroup first = matches.Groups[firstIndex];
                for (int secondIndex = firstIndex + 1; secondIndex < matches.Groups.Count; secondIndex++)
                {
                    MatchGroup second = matches.Groups[secondIndex];
                    if (first.Direction == second.Direction || first.ColorIndex != second.ColorIndex)
                        continue;

                    int intersection = FindIntersection(first, second);
                    if (intersection < 0)
                        continue;

                    int creationCell = SelectShapeCreationCell(
                        first,
                        second,
                        intersection,
                        preferredCreationCell,
                        created);
                    if (creationCell >= 0)
                        created.Add(creationCell, new BoardPiece(first.ColorIndex, SpecialKind.Blast));
                    consumedGroups.Add(first);
                    consumedGroups.Add(second);
                }
            }

            foreach (MatchGroup group in matches.Groups)
            {
                if (consumedGroups.Contains(group) || group.Length < 4)
                    continue;

                int creationCell = SelectCreationCell(group, preferredCreationCell, created);
                if (creationCell < 0)
                    continue;

                SpecialKind special = group.Length >= 5
                    ? SpecialKind.ColorClear
                    : group.Direction == MatchDirection.Horizontal
                        ? SpecialKind.RowClear
                        : SpecialKind.ColumnClear;
                created.Add(creationCell, new BoardPiece(group.ColorIndex, special));
            }

            ExpandSpecials(affected);

            var removed = new HashSet<int>(affected);
            foreach (int creationCell in created.Keys)
                removed.Remove(creationCell);

            return new MatchResolution(affected, removed, created);
        }

        private static int FindIntersection(MatchGroup first, MatchGroup second)
        {
            foreach (int firstCell in first.CellIds)
                foreach (int secondCell in second.CellIds)
                    if (firstCell == secondCell)
                        return firstCell;
            return -1;
        }

        private static int SelectShapeCreationCell(
            MatchGroup first,
            MatchGroup second,
            int intersection,
            int preferredCreationCell,
            IReadOnlyDictionary<int, BoardPiece> created)
        {
            if (preferredCreationCell >= 0 && !created.ContainsKey(preferredCreationCell) &&
                (ContainsCell(first, preferredCreationCell) || ContainsCell(second, preferredCreationCell)))
                return preferredCreationCell;
            return created.ContainsKey(intersection) ? -1 : intersection;
        }

        private static bool ContainsCell(MatchGroup group, int cellId)
        {
            foreach (int groupCell in group.CellIds)
                if (groupCell == cellId)
                    return true;
            return false;
        }

        private static int SelectCreationCell(
            MatchGroup group,
            int preferredCreationCell,
            IReadOnlyDictionary<int, BoardPiece> created)
        {
            if (preferredCreationCell >= 0 && !created.ContainsKey(preferredCreationCell))
            {
                foreach (int cellId in group.CellIds)
                    if (cellId == preferredCreationCell)
                        return preferredCreationCell;
            }

            foreach (int cellId in group.CellIds)
                if (!created.ContainsKey(cellId))
                    return cellId;
            return -1;
        }

        private void ExpandSpecials(HashSet<int> affected, HashSet<int> consumed = null,
            Dictionary<int, BoardPiece> converted = null)
        {
            var pending = new Queue<int>(affected);
            var activated = consumed == null ? new HashSet<int>() : new HashSet<int>(consumed);
            while (pending.Count > 0)
            {
                int cellId = pending.Dequeue();
                if (!activated.Add(cellId))
                    continue;

                int x = cellId % Width;
                int y = cellId / Width;
                if (!Contains(x, y))
                    continue;

                BoardPiece piece = converted != null && converted.TryGetValue(cellId, out BoardPiece replacement)
                    ? replacement : Cells[x, y];
                SpecialKind special = piece.Special;
                if (special == SpecialKind.RowClear)
                {
                    for (int rowX = 0; rowX < Width; rowX++)
                        AddAffected(affected, pending, y * Width + rowX);
                }
                else if (special == SpecialKind.ColumnClear)
                {
                    for (int columnY = 0; columnY < Height; columnY++)
                        AddAffected(affected, pending, columnY * Width + x);
                }
                else if (special == SpecialKind.Blast)
                {
                    for (int offsetY = -1; offsetY <= 1; offsetY++)
                        for (int offsetX = -1; offsetX <= 1; offsetX++)
                        {
                            int blastX = x + offsetX;
                            int blastY = y + offsetY;
                            if (Contains(blastX, blastY))
                                AddAffected(affected, pending, blastY * Width + blastX);
                        }
                }
                else if (special == SpecialKind.ColorClear)
                {
                    int color = piece.ColorIndex;
                    for (int colorY = 0; colorY < Height; colorY++)
                        for (int colorX = 0; colorX < Width; colorX++)
                            if (!Cells[colorX, colorY].IsEmpty && Cells[colorX, colorY].ColorIndex == color)
                                AddAffected(affected, pending, colorY * Width + colorX);
                }
            }
        }

        private MatchResolution PlanCombination(int source, int destination)
        {
            if (!Contains(source % Width, source / Width) || !Contains(destination % Width, destination / Width))
                return null;

            BoardPiece first = Cells[destination % Width, destination / Width];
            BoardPiece second = Cells[source % Width, source / Width];
            bool colorClear = first.Special == SpecialKind.ColorClear || second.Special == SpecialKind.ColorClear;
            if (!colorClear && (first.Special == SpecialKind.None || second.Special == SpecialKind.None))
                return null;

            var affected = new HashSet<int> { source, destination };
            var converted = new Dictionary<int, BoardPiece>();
            int centerX = destination % Width;
            int centerY = destination / Width;

            if (first.Special == SpecialKind.ColorClear && second.Special == SpecialKind.ColorClear)
            {
                for (int id = 0; id < Width * Height; id++) affected.Add(id);
            }
            else if (colorClear)
            {
                BoardPiece partner = first.Special == SpecialKind.ColorClear ? second : first;
                int color = partner.ColorIndex;
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                    {
                        BoardPiece piece = Cells[x, y];
                        if (!piece.IsEmpty && piece.ColorIndex == color)
                        {
                            int id = y * Width + x;
                            affected.Add(id);
                            if (partner.Special != SpecialKind.None)
                                converted[id] = new BoardPiece(piece.ColorIndex, partner.Special);
                        }
                    }
            }
            else if (first.Special == SpecialKind.Blast && second.Special == SpecialKind.Blast)
            {
                for (int y = centerY - 2; y <= centerY + 2; y++)
                    for (int x = centerX - 2; x <= centerX + 2; x++)
                        if (Contains(x, y)) affected.Add(y * Width + x);
            }
            else if (first.Special == SpecialKind.Blast || second.Special == SpecialKind.Blast)
            {
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                        if (Math.Abs(x - centerX) <= 1 || Math.Abs(y - centerY) <= 1)
                            affected.Add(y * Width + x);
            }
            else
            {
                for (int x = 0; x < Width; x++) affected.Add(centerY * Width + x);
                for (int y = 0; y < Height; y++)
                    affected.Add(y * Width + centerX);
            }

            // Both swapped specials are consumed; other hit specials activate once.
            ExpandSpecials(affected, new HashSet<int> { source, destination }, converted);
            var removed = new HashSet<int>(affected);
            return new MatchResolution(affected, removed, new Dictionary<int, BoardPiece>());
        }

        public bool TryGetLegalMove(out int source, out int destination)
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    for (int direction = 0; direction < 2; direction++)
                    {
                        int tx = x + (direction == 0 ? 1 : 0), ty = y + (direction == 1 ? 1 : 0);
                        if (!TrySwap(x, y, tx, ty)) continue;
                        Swap(x, y, tx, ty);
                        source = y * Width + x;
                        destination = ty * Width + tx;
                        return true;
                    }
            source = destination = -1;
            return false;
        }

        private static void AddAffected(HashSet<int> affected, Queue<int> pending, int cellId)
        {
            if (affected.Add(cellId))
                pending.Enqueue(cellId);
        }

        private static bool SameColor(BoardPiece first, BoardPiece second) =>
            !first.IsEmpty && !second.IsEmpty && first.ColorIndex == second.ColorIndex;

        private void AddGroup(MatchResult result, int x, int y, int length, MatchDirection direction)
        {
            BoardPiece first = Cells[x, y];
            if (first.IsEmpty || length < 3)
                return;

            var cellIds = new int[length];
            for (int i = 0; i < length; i++)
            {
                int cellX = direction == MatchDirection.Horizontal ? x + i : x;
                int cellY = direction == MatchDirection.Vertical ? y + i : y;
                cellIds[i] = cellY * Width + cellX;
            }

            result.Add(new MatchGroup(first.ColorIndex, direction, cellIds));
        }

        public bool HasMove()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    for (int direction = 0; direction < 2; direction++)
                    {
                        int targetX = x + (direction == 0 ? 1 : 0);
                        int targetY = y + (direction == 1 ? 1 : 0);
                        if (!Contains(targetX, targetY))
                            continue;

                        bool valid = TrySwap(x, y, targetX, targetY);
                        if (valid)
                        {
                            Swap(x, y, targetX, targetY);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public ClearResult Clear(IEnumerable<int> cellIds)
        {
            var result = new ClearResult(colorCount);
            var visited = new HashSet<int>();
            foreach (int cellId in cellIds)
            {
                if (!visited.Add(cellId))
                    continue;
                int x = cellId % Width;
                int y = cellId / Width;
                if (!Contains(x, y) || Cells[x, y].IsEmpty)
                    continue;

                result.PieceCount++;
                result.AddColor(Cells[x, y].ColorIndex);
                Cells[x, y] = BoardPiece.Empty;
                Layers[x, y] = Layers[x, y].Damage(out bool layerCleared);
                if (layerCleared)
                    result.LayersCleared++;
            }

            return result;
        }

        public ClearResult ApplyMatchResolution(MatchResolution resolution)
        {
            if (resolution == null)
                throw new ArgumentNullException(nameof(resolution));

            var result = new ClearResult(colorCount);
            foreach (int cellId in resolution.AffectedCells)
            {
                int x = cellId % Width;
                int y = cellId / Width;
                if (!Contains(x, y) || Cells[x, y].IsEmpty)
                    continue;

                result.PieceCount++;
                result.AddColor(Cells[x, y].ColorIndex);
                if (!resolution.CreatedPieces.ContainsKey(cellId))
                    Cells[x, y] = BoardPiece.Empty;
                Layers[x, y] = Layers[x, y].Damage(out bool layerCleared);
                if (layerCleared)
                    result.LayersCleared++;
            }

            foreach (KeyValuePair<int, BoardPiece> creation in resolution.CreatedPieces)
            {
                int x = creation.Key % Width;
                int y = creation.Key / Width;
                Cells[x, y] = creation.Value;
            }

            return result;
        }

        // Returns each destination's source row; -1 denotes a newly spawned piece.
        public int[,] CollapseAndRefill()
        {
            var sources = new int[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                int write = 0;
                for (int y = 0; y < Height; y++)
                {
                    if (Cells[x, y].IsEmpty)
                        continue;

                    Cells[x, write] = Cells[x, y];
                    sources[x, write] = y;
                    write++;
                }

                while (write < Height)
                {
                    Cells[x, write] = RandomPiece();
                    sources[x, write] = -1;
                    write++;
                }
            }

            return sources;
        }
    }
}
