using System;
using GemforgeCascade.Core;

// Host types for testing the real game-state code without the Unity Editor.
namespace UnityEngine { public class MonoBehaviour { } }
namespace GemforgeCascade.Core
{
    public class BoardConfig { public int moves = 25; public int targetScore = 1000; }
}

internal static class Program
{
    private static int assertions;
    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception(message);
    }
    private static void Equal(int[,] a, int[,] b)
    {
        for (int x = 0; x < a.GetLength(0); x++)
            for (int y = 0; y < a.GetLength(1); y++) Check(a[x, y] == b[x, y], "Unexpected mutation");
    }
    private static void Main()
    {
        for (int seed = 0; seed < 200; seed++)
        {
            var board = new BoardModel(3 + seed % 14, 3 + seed / 14 % 14, 3 + seed % 4, seed);
            Check(board.FindMatches().Count == 0, "Starting match");
            var before = (int[,])board.Cells.Clone();
            Check(board.HasMove(), "Dead start");
            Equal(before, board.Cells);
            Check(!board.TrySwap(0, 0, 1, 1), "Diagonal allowed");
            Check(!board.TrySwap(0, 0, -1, 0), "Out of bounds allowed");
            for (int turn = 0; turn < 10; turn++)
            {
                bool moved = false;
                for (int y = 0; y < board.Height && !moved; y++)
                    for (int x = 0; x < board.Width && !moved; x++)
                        for (int d = 0; d < 2 && !moved; d++)
                        {
                            before = (int[,])board.Cells.Clone();
                            moved = board.TrySwap(x, y, x + (d == 0 ? 1 : 0), y + (d == 1 ? 1 : 0));
                            if (!moved) Equal(before, board.Cells);
                        }
                Check(moved, "No legal move");
                int cascade = 0;
                while (board.FindMatches().Count > 0)
                {
                    Check(cascade++ < 1000, "Cascade failed to settle");
                    foreach (int id in board.FindMatches()) board.Cells[id % board.Width, id / board.Width] = -1;
                    before = (int[,])board.Cells.Clone();
                    var sources = board.CollapseAndRefill();
                    for (int x = 0; x < board.Width; x++)
                    {
                        int last = -1;
                        for (int y = 0; y < board.Height; y++)
                        {
                            Check(board.Cells[x, y] >= 0 && board.Cells[x, y] < 3 + seed % 4, "Invalid refill");
                            if (sources[x, y] < 0) continue;
                            Check(sources[x, y] > last && sources[x, y] >= y, "Gravity order");
                            Check(board.Cells[x, y] == before[x, sources[x, y]], "Lost piece");
                            last = sources[x, y];
                        }
                    }
                }
                if (!board.HasMove()) board.Generate();
                Check(board.FindMatches().Count == 0 && board.HasMove(), "Unplayable board");
            }
        }
        var shape = new BoardModel(5, 5, 6, 1);
        for (int length = 3; length <= 5; length++)
        {
            for (int x = 0; x < 5; x++) for (int y = 0; y < 5; y++) shape.Cells[x, y] = -1;
            for (int x = 0; x < length; x++) shape.Cells[x, 0] = 0;
            Check(shape.FindMatches().Count == length, "Run size");
        }
        for (int x = 0; x < 5; x++) for (int y = 0; y < 5; y++) shape.Cells[x, y] = -1;
        for (int i = 0; i < 3; i++) { shape.Cells[i, 2] = 1; shape.Cells[1, i] = 1; }
        Check(shape.FindMatches().Count == 5, "T overlap");
        shape.Cells[1, 0] = -1; shape.Cells[1, 1] = -1;
        shape.Cells[0, 0] = 1; shape.Cells[0, 1] = 1;
        Check(shape.FindMatches().Count == 5, "L match");
        var game = new GameManager();
        var config = new BoardConfig { moves = 1, targetScore = 90 };
        game.Begin(config); game.ConsumeMove(); game.Award(3, 1); game.Award(3, 2);
        Check(game.State == GameState.Playing, "Premature ending");
        game.FinishTurn();
        Check(game.Score == 90 && game.Moves == 0 && game.State == GameState.Won, "Last move win");
        game.Begin(config); game.ConsumeMove(); game.Award(3, 1); game.FinishTurn();
        Check(game.State == GameState.Lost, "Loss");
        game.Begin(config);
        Check(game.Score == 0 && game.Moves == 1 && game.State == GameState.Playing, "Restart");
        Console.WriteLine($"PASS: {assertions:N0} assertions; 200 seeded boards, 2,000 turns, gravity, matches and game state.");
    }
}
