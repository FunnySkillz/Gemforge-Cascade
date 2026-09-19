using System;
using System.Collections.Generic;

namespace GemforgeCascade.Core
{
    public enum PieceType { Red, Blue, Green, Yellow, Purple, Orange }

    // Pure board rules, independent of scene objects and animation timing.
    public sealed class BoardModel
    {
        public int Width { get; }
        public int Height { get; }
        public int[,] Cells { get; }
        private readonly int types;
        private readonly Random random;

        public BoardModel(int width, int height, int types, int? seed = null)
        {
            if (width < 3 || height < 3 || types < 3 || types > 6)
                throw new ArgumentOutOfRangeException(nameof(width));
            Width = width;
            Height = height;
            this.types = types;
            random = seed.HasValue ? new Random(seed.Value) : new Random();
            Cells = new int[width, height];
            Generate();
        }

        public int RandomType() => random.Next(types);
        public bool Contains(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public void Generate()
        {
            for (int attempt = 0; attempt < 100; attempt++)
            {
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                    {
                        int type = RandomType();
                        // At most two types are forbidden, so a safe type always exists.
                        while ((x >= 2 && Cells[x - 1, y] == type && Cells[x - 2, y] == type) ||
                               (y >= 2 && Cells[x, y - 1] == type && Cells[x, y - 2] == type))
                            type = (type + 1) % types;
                        Cells[x, y] = type;
                    }
                if (HasMove()) return;
            }
            // Bounded fallback with a guaranteed bottom-left swap and no starting runs.
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++) Cells[x, y] = (x + y) % types;
            Cells[0, 0] = 0; Cells[1, 0] = 1; Cells[2, 0] = 0; Cells[1, 1] = 0;
        }

        public void Swap(int x, int y, int tx, int ty)
        {
            int value = Cells[x, y];
            Cells[x, y] = Cells[tx, ty];
            Cells[tx, ty] = value;
        }

        public bool TrySwap(int x, int y, int tx, int ty)
        {
            if (!Contains(x, y) || !Contains(tx, ty) || Math.Abs(x - tx) + Math.Abs(y - ty) != 1)
                return false;
            Swap(x, y, tx, ty);
            if (FindMatches().Count > 0) return true;
            Swap(x, y, tx, ty);
            return false;
        }

        public HashSet<int> FindMatches()
        {
            var matches = new HashSet<int>();
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width;)
                {
                    int end = x + 1;
                    while (end < Width && Cells[end, y] == Cells[x, y]) end++;
                    if (Cells[x, y] >= 0 && end - x >= 3)
                        for (int i = x; i < end; i++) matches.Add(y * Width + i);
                    x = end;
                }
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height;)
                {
                    int end = y + 1;
                    while (end < Height && Cells[x, end] == Cells[x, y]) end++;
                    if (Cells[x, y] >= 0 && end - y >= 3)
                        for (int i = y; i < end; i++) matches.Add(i * Width + x);
                    y = end;
                }
            return matches;
        }

        public bool HasMove()
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    for (int direction = 0; direction < 2; direction++)
                    {
                        int tx = x + (direction == 0 ? 1 : 0);
                        int ty = y + (direction == 1 ? 1 : 0);
                        if (!Contains(tx, ty)) continue;
                        bool valid = TrySwap(x, y, tx, ty);
                        if (valid) { Swap(x, y, tx, ty); return true; }
                    }
            return false;
        }

        // Returns each destination's source row; -1 denotes a newly spawned piece.
        public int[,] CollapseAndRefill()
        {
            var sources = new int[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                int write = 0;
                for (int y = 0; y < Height; y++)
                    if (Cells[x, y] >= 0)
                    {
                        Cells[x, write] = Cells[x, y];
                        sources[x, write++] = y;
                    }
                while (write < Height)
                {
                    Cells[x, write] = RandomType();
                    sources[x, write++] = -1;
                }
            }
            return sources;
        }
    }
}
