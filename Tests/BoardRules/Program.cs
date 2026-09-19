using System;
using System.Text.Json;
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
        if (!condition)
            throw new Exception(message);
    }

    private static void Equal(BoardPiece[,] first, BoardPiece[,] second)
    {
        for (int x = 0; x < first.GetLength(0); x++)
            for (int y = 0; y < first.GetLength(1); y++)
                Check(first[x, y] == second[x, y], "Unexpected board mutation");
    }

    private static void Empty(BoardModel board)
    {
        for (int x = 0; x < board.Width; x++)
            for (int y = 0; y < board.Height; y++)
                board.Cells[x, y] = BoardPiece.Empty;
    }

    private static void Main()
    {
        ExerciseGeneratedBoards();
        ExerciseDeterministicRandom();
        ExerciseVersionedData();
        ExerciseStructuredMatches();
        ExerciseSpecialState();
        ExerciseLineSpecials();
        ExerciseLayersAndObjectives();
        ExerciseLevelValidation();
        ExerciseGameState();

        Console.WriteLine(
            $"PASS: {assertions:N0} assertions; 200 seeded boards, 2,000 turns, " +
            "deterministic saves, structured matches, line specials, layers, objectives and game state.");
    }

    private static void ExerciseGeneratedBoards()
    {
        for (int seed = 0; seed < 200; seed++)
        {
            int colors = 3 + seed % 4;
            var board = new BoardModel(3 + seed % 14, 3 + seed / 14 % 14, colors, seed);
            Check(board.FindMatches().Count == 0, "Starting board contains a match");

            var before = (BoardPiece[,])board.Cells.Clone();
            Check(board.HasMove(), "Starting board has no legal move");
            Equal(before, board.Cells);
            Check(!board.TrySwap(0, 0, 1, 1), "Diagonal swap was accepted");
            Check(!board.TrySwap(0, 0, -1, 0), "Out-of-bounds swap was accepted");

            for (int turn = 0; turn < 10; turn++)
            {
                bool moved = false;
                for (int y = 0; y < board.Height && !moved; y++)
                {
                    for (int x = 0; x < board.Width && !moved; x++)
                    {
                        for (int direction = 0; direction < 2 && !moved; direction++)
                        {
                            before = (BoardPiece[,])board.Cells.Clone();
                            moved = board.TrySwap(
                                x,
                                y,
                                x + (direction == 0 ? 1 : 0),
                                y + (direction == 1 ? 1 : 0));
                            if (!moved)
                                Equal(before, board.Cells);
                        }
                    }
                }

                Check(moved, "Playable board reported a move but none was found");
                int cascade = 0;
                MatchResult matches = board.FindMatches();
                while (matches.Count > 0)
                {
                    Check(cascade++ < 1000, "Cascade failed to settle");
                    board.Clear(matches.Cells);
                    before = (BoardPiece[,])board.Cells.Clone();
                    int[,] sources = board.CollapseAndRefill();

                    for (int x = 0; x < board.Width; x++)
                    {
                        int lastSource = -1;
                        for (int y = 0; y < board.Height; y++)
                        {
                            BoardPiece piece = board.Cells[x, y];
                            Check(!piece.IsEmpty && piece.ColorIndex < colors, "Invalid refill piece");
                            Check(piece.Special == SpecialKind.None, "Random refill created a special");
                            if (sources[x, y] < 0)
                                continue;

                            Check(sources[x, y] > lastSource && sources[x, y] >= y, "Gravity order is invalid");
                            Check(piece == before[x, sources[x, y]], "Gravity lost or changed a piece");
                            lastSource = sources[x, y];
                        }
                    }

                    matches = board.FindMatches();
                }

                if (!board.HasMove())
                    board.Generate();
                Check(board.FindMatches().Count == 0 && board.HasMove(), "Settled board is unplayable");
            }
        }
    }

    private static void ExerciseStructuredMatches()
    {
        var board = new BoardModel(5, 5, 6, 1);

        for (int length = 3; length <= 5; length++)
        {
            Empty(board);
            for (int x = 0; x < length; x++)
                board.Cells[x, 0] = new BoardPiece(0);

            MatchResult match = board.FindMatches();
            Check(match.Count == length, "Straight run has the wrong clear count");
            Check(match.Groups.Count == 1, "Straight run has the wrong group count");
            Check(match.Groups[0].Length == length, "Straight run has the wrong group length");
            Check(match.Groups[0].Direction == MatchDirection.Horizontal, "Straight run has wrong direction");
            Check(match.Groups[0].ColorIndex == 0, "Straight run has wrong color");
        }

        Empty(board);
        for (int i = 0; i < 3; i++)
        {
            board.Cells[i, 2] = new BoardPiece(1);
            board.Cells[1, i] = new BoardPiece(1);
        }
        MatchResult tMatch = board.FindMatches();
        Check(tMatch.Count == 5, "T overlap was counted more than once");
        Check(tMatch.Groups.Count == 2, "T match must contain two directional groups");
        Check(tMatch.Groups[0].Direction != tMatch.Groups[1].Direction, "T groups need distinct directions");

        Empty(board);
        for (int i = 0; i < 3; i++)
        {
            board.Cells[i, 0] = new BoardPiece(2);
            board.Cells[0, i] = new BoardPiece(2);
        }
        MatchResult lMatch = board.FindMatches();
        Check(lMatch.Count == 5, "L overlap was counted more than once");
        Check(lMatch.Groups.Count == 2, "L match must contain two directional groups");
    }

    private static void ExerciseVersionedData()
    {
        var source = new BoardModel(8, 8, 6, 314159);
        source.Cells[0, 0] = new BoardPiece(4, SpecialKind.ColumnClear);
        source.Layers[0, 0] = new CellLayer(CellLayerKind.Crystal, 2);
        BoardSnapshot snapshot = source.CreateSnapshot("chapter-1-level-3");

        var jsonOptions = new JsonSerializerOptions { IncludeFields = true };
        string json = JsonSerializer.Serialize(snapshot, jsonOptions);
        BoardSnapshot decoded = JsonSerializer.Deserialize<BoardSnapshot>(json, jsonOptions);
        var restored = BoardModel.FromSnapshot(decoded);

        Check(restored.Width == source.Width && restored.Height == source.Height, "Snapshot dimensions changed");
        Check(restored.ColorCount == source.ColorCount, "Snapshot color count changed");
        Check(restored.RandomState == source.RandomState, "Snapshot random state changed");
        Equal(source.Cells, restored.Cells);
        Check(restored.Layers[0, 0] == source.Layers[0, 0], "Snapshot layer state changed");

        source.Clear(new[] { 0, 1, 2, 8, 9 });
        restored.Clear(new[] { 0, 1, 2, 8, 9 });
        source.CollapseAndRefill();
        restored.CollapseAndRefill();
        Equal(source.Cells, restored.Cells);
        Check(source.RandomState == restored.RandomState, "Restored refill diverged");

        var legacyLevel = new LevelDefinition
        {
            version = 0,
            id = null,
            width = 3,
            height = 3,
            colorCount = 3,
            seed = 99,
            startingPieces = null
        };
        BoardModel legacyBoard = BoardModel.FromLevel(legacyLevel);
        Check(legacyLevel.version == GameDataVersions.Level, "Legacy level was not upgraded");
        Check(legacyLevel.id == "legacy-level", "Legacy level ID was not supplied");
        Check(legacyBoard.FindMatches().Count == 0 && legacyBoard.HasMove(), "Migrated level is not playable");

        bool futureRejected = false;
        try
        {
            BoardModel.FromLevel(new LevelDefinition { version = GameDataVersions.Level + 1 });
        }
        catch (NotSupportedException)
        {
            futureRejected = true;
        }
        Check(futureRejected, "Unsupported future level version was accepted");
    }

    private static void ExerciseLayersAndObjectives()
    {
        var board = new BoardModel(3, 3, 3, 77);
        Empty(board);
        board.Cells[0, 0] = new BoardPiece(0);
        board.Cells[1, 0] = new BoardPiece(0);
        board.Cells[2, 0] = new BoardPiece(1);
        board.Layers[0, 0] = new CellLayer(CellLayerKind.Crystal, 1);
        board.Layers[1, 0] = new CellLayer(CellLayerKind.Crystal, 2);

        var objectives = new[]
        {
            new ObjectiveData { kind = (int)ObjectiveKind.Score, target = 100 },
            new ObjectiveData { kind = (int)ObjectiveKind.CollectColor, colorIndex = 0, target = 3 },
            new ObjectiveData { kind = (int)ObjectiveKind.ClearLayers, target = 2 }
        };
        var tracker = new ObjectiveTracker(objectives, board.ColorCount);

        ClearResult first = board.Clear(new[] { 0, 0, 1, 2 });
        Check(first.PieceCount == 3, "Duplicate clear IDs removed a piece twice");
        Check(first.RemovedColor(0) == 2 && first.RemovedColor(1) == 1, "Clear color counts are wrong");
        Check(first.LayersCleared == 1, "One-hit layer did not clear");
        Check(board.Layers[0, 0].IsEmpty, "Cleared layer remains on board");
        Check(board.Layers[1, 0].Durability == 1, "Two-hit layer durability is wrong");
        tracker.Apply(first, 60);
        Check(tracker.Objectives[0].Current == 60, "Score objective progress is wrong");
        Check(tracker.Objectives[1].Current == 2, "Collection objective progress is wrong");
        Check(tracker.Objectives[2].Current == 1, "Layer objective progress is wrong");
        Check(!tracker.Complete, "Objectives completed too early");

        board.Cells[1, 0] = new BoardPiece(0);
        ClearResult second = board.Clear(new[] { 1 });
        tracker.Apply(second, 120);
        Check(second.LayersCleared == 1 && board.Layers[1, 0].IsEmpty, "Second layer hit failed");
        Check(tracker.Objectives[0].Current == 100, "Score objective did not cap at target");
        Check(tracker.Objectives[1].Current == 3, "Collection objective did not complete");
        Check(tracker.Objectives[2].Current == 2, "Layer objective did not complete");
        Check(tracker.Complete, "Completed objectives were not recognized");

        var level = new LevelDefinition
        {
            width = 3,
            height = 3,
            colorCount = 3,
            seed = 5,
            startingLayers = new CellLayerData[9]
        };
        for (int i = 0; i < level.startingLayers.Length; i++)
            level.startingLayers[i] = new CellLayerData(CellLayer.Empty);
        level.startingLayers[4] = new CellLayerData(new CellLayer(CellLayerKind.Crystal, 2));
        BoardModel levelBoard = BoardModel.FromLevel(level);
        Check(levelBoard.Layers[1, 1].Durability == 2, "Authored layer did not load");
    }

    private static void ExerciseLineSpecials()
    {
        var board = new BoardModel(5, 5, 6, 12);
        Empty(board);
        for (int x = 0; x < 4; x++)
        {
            board.Cells[x, 0] = new BoardPiece(0);
            board.Layers[x, 0] = new CellLayer(CellLayerKind.Crystal, 1);
        }

        MatchResult fourMatch = board.FindMatches();
        int preferred = 2;
        MatchResolution creation = board.PlanMatchResolution(fourMatch, preferred);
        Check(creation.AffectedCells.Count == 4, "Four-match affected the wrong cells");
        Check(creation.RemovedCells.Count == 3, "Four-match must preserve one creation cell");
        Check(creation.CreatedPieces.Count == 1, "Four-match did not create exactly one special");
        Check(creation.CreatedPieces.ContainsKey(preferred), "Player destination was not preferred");
        Check(creation.CreatedPieces[preferred].Special == SpecialKind.RowClear, "Horizontal four created wrong special");

        ClearResult creationClear = board.ApplyMatchResolution(creation);
        Check(creationClear.PieceCount == 4, "Special creation did not count all matched pieces");
        Check(creationClear.LayersCleared == 4, "Special creation did not damage every matched layer");
        Check(board.Cells[2, 0].Special == SpecialKind.RowClear, "Created line special did not survive");
        Check(board.Cells[0, 0].IsEmpty && board.Cells[1, 0].IsEmpty && board.Cells[3, 0].IsEmpty,
            "Four-match left ordinary pieces behind");

        Empty(board);
        for (int x = 0; x < board.Width; x++)
            board.Cells[x, 2] = new BoardPiece((x + 1) % board.ColorCount);
        board.Cells[2, 1] = new BoardPiece(0);
        board.Cells[2, 2] = new BoardPiece(0, SpecialKind.RowClear);
        board.Cells[2, 3] = new BoardPiece(0);
        MatchResolution rowActivation = board.PlanMatchResolution(board.FindMatches());
        Check(rowActivation.AffectedCells.Count == 7, "Row clear affected the wrong area");
        Check(rowActivation.CreatedPieces.Count == 0, "Three-match created a special");
        ClearResult rowClear = board.ApplyMatchResolution(rowActivation);
        Check(rowClear.PieceCount == 7, "Row clear removed the wrong number of pieces");

        Empty(board);
        for (int x = 0; x < board.Width; x++)
            board.Cells[x, 2] = new BoardPiece((x + 1) % board.ColorCount);
        for (int y = 0; y < board.Height; y++)
            board.Cells[4, y] = new BoardPiece((y + 2) % board.ColorCount);
        board.Cells[2, 1] = new BoardPiece(0);
        board.Cells[2, 2] = new BoardPiece(0, SpecialKind.RowClear);
        board.Cells[2, 3] = new BoardPiece(0);
        board.Cells[4, 2] = new BoardPiece(3, SpecialKind.ColumnClear);
        MatchResolution chained = board.PlanMatchResolution(board.FindMatches());
        Check(chained.AffectedCells.Count == 11, "Chained line clears affected the wrong area");
        ClearResult chainedClear = board.ApplyMatchResolution(chained);
        Check(chainedClear.PieceCount == 11, "Chained line clears removed the wrong number of pieces");

        Empty(board);
        for (int y = 0; y < 4; y++)
            board.Cells[0, y] = new BoardPiece(5);
        MatchResolution vertical = board.PlanMatchResolution(board.FindMatches(), board.Width * 2);
        Check(vertical.CreatedPieces[board.Width * 2].Special == SpecialKind.ColumnClear,
            "Vertical four created wrong special");
    }

    private static void ExerciseDeterministicRandom()
    {
        var first = new BoardModel(8, 8, 6, 123456);
        var second = new BoardModel(8, 8, 6, 123456);
        Equal(first.Cells, second.Cells);
        Check(first.RandomState == second.RandomState, "Equal seeds produced different random state");

        uint savedState = first.RandomState;
        BoardPiece[] expected = new BoardPiece[16];
        for (int i = 0; i < expected.Length; i++)
            expected[i] = first.RandomPiece();

        first.RestoreRandomState(savedState);
        for (int i = 0; i < expected.Length; i++)
            Check(first.RandomPiece() == expected[i], "Restored random state produced a different sequence");

        Empty(first);
        Empty(second);
        first.RestoreRandomState(savedState);
        second.RestoreRandomState(savedState);
        first.CollapseAndRefill();
        second.CollapseAndRefill();
        Equal(first.Cells, second.Cells);
        Check(first.RandomState == second.RandomState, "Equal refills ended with different random state");

        var zeroSeed = new DeterministicRandom(0);
        Check(zeroSeed.State != 0, "Zero seed was not normalized");
    }

    private static void ExerciseSpecialState()
    {
        var board = new BoardModel(3, 3, 3, 2);
        Empty(board);
        board.Cells[0, 0] = new BoardPiece(0, SpecialKind.RowClear);
        board.Cells[1, 0] = new BoardPiece(0);
        board.Cells[2, 0] = new BoardPiece(0);
        MatchResult specialMatch = board.FindMatches();
        Check(specialMatch.Count == 3, "Special must match by its color");
        Check(specialMatch.Groups.Count == 1, "Special changed the match grouping");

        Empty(board);
        BoardPiece blast = new BoardPiece(2, SpecialKind.Blast);
        board.Cells[1, 0] = new BoardPiece(1);
        board.Cells[1, 2] = blast;
        int[,] sources = board.CollapseAndRefill();
        Check(sources[1, 0] == 0 && sources[1, 1] == 2, "Special gravity sources are wrong");
        Check(board.Cells[1, 1] == blast, "Special state did not survive gravity");

        board.Swap(1, 0, 1, 1);
        Check(board.Cells[1, 0] == blast, "Special state did not survive a swap");
    }

    private static void ExerciseGameState()
    {
        var game = new GameManager();
        var config = new BoardConfig { moves = 1, targetScore = 90 };

        game.Begin(config);
        game.ConsumeMove();
        game.Award(3, 1);
        game.Award(3, 2);
        Check(game.State == GameState.Playing, "Game ended before cascades completed");
        game.FinishTurn();
        Check(game.Score == 90 && game.Moves == 0 && game.State == GameState.Won, "Last-move win failed");

        game.Begin(config);
        game.ConsumeMove();
        game.Award(3, 1);
        game.FinishTurn();
        Check(game.State == GameState.Lost, "Loss condition failed");

        game.Begin(config);
        Check(game.Score == 0 && game.Moves == 1 && game.State == GameState.Playing, "Restart failed");

        var board = new BoardModel(3, 3, 3, 8);
        game.ConsumeMove();
        game.Award(4, 2);
        AttemptSnapshot savedAttempt = game.CreateSnapshot(board.CreateSnapshot("save-test"));
        var restoredGame = new GameManager();
        restoredGame.Restore(savedAttempt);
        Check(restoredGame.Score == game.Score, "Saved score did not restore");
        Check(restoredGame.Moves == game.Moves, "Saved moves did not restore");
        Check(restoredGame.Target == game.Target, "Saved target did not restore");
        Check(restoredGame.State == game.State, "Saved game state did not restore");
    }

    private static void ExerciseLevelValidation()
    {
        var valid = new LevelDefinition
        {
            id = "validation-generated",
            width = 8,
            height = 8,
            colorCount = 6,
            moves = 20,
            targetScore = 1000,
            seed = 123
        };
        LevelValidationResult validResult = LevelValidator.Validate(valid);
        Check(validResult.IsValid, "Generated level failed validation");
        Check(validResult.LegalMoveCount > 0, "Validator did not report legal moves");

        var malformed = new LevelDefinition
        {
            id = string.Empty,
            width = 2,
            height = 20,
            colorCount = 2,
            moves = 0,
            targetScore = 0
        };
        LevelValidationResult malformedResult = LevelValidator.Validate(malformed);
        Check(!malformedResult.IsValid, "Malformed level passed validation");
        Check(malformedResult.HasCode("ID_REQUIRED"), "Missing ID was not reported");
        Check(malformedResult.HasCode("DIMENSIONS_INVALID"), "Invalid dimensions were not reported");
        Check(malformedResult.HasCode("COLOR_COUNT_INVALID"), "Invalid color count was not reported");

        var matched = AuthoredLevel(new[]
        {
            0, 0, 0,
            1, 2, 1,
            2, 1, 2
        });
        LevelValidationResult matchedResult = LevelValidator.Validate(matched);
        Check(matchedResult.HasCode("STARTING_MATCH"), "Starting match was not reported");

        var dead = AuthoredLevel(new[]
        {
            0, 1, 2,
            1, 2, 0,
            2, 0, 1
        });
        LevelValidationResult deadResult = LevelValidator.Validate(dead);
        Check(deadResult.HasCode("DEAD_BOARD"), "Dead starting board was not reported");

        var trivial = new LevelDefinition
        {
            id = "trivial-generated",
            width = 8,
            height = 8,
            colorCount = 6,
            moves = 10,
            targetScore = 30,
            seed = 123
        };
        LevelValidationResult trivialResult = LevelValidator.Validate(trivial);
        Check(trivialResult.HasCode("ONE_MOVE_COMPLETION"), "One-move level warning was not reported");

        BoardSimulationResult simulation = BoardSimulator.Run(valid, 5);
        Check(simulation.TurnsPlayed > 0 && simulation.TurnsPlayed <= 5, "Simulation turn count is invalid");
        Check(simulation.Score > 0 && simulation.PiecesCleared > 0, "Simulation did not resolve matches");
        Check(simulation.CascadeSteps >= simulation.TurnsPlayed, "Simulation cascade count is invalid");
    }

    private static LevelDefinition AuthoredLevel(int[] colors)
    {
        var level = new LevelDefinition
        {
            id = "authored-test",
            width = 3,
            height = 3,
            colorCount = 3,
            moves = 10,
            targetScore = 100,
            seed = 1,
            startingPieces = new PieceData[colors.Length]
        };
        for (int i = 0; i < colors.Length; i++)
            level.startingPieces[i] = new PieceData(new BoardPiece(colors[i]));
        return level;
    }
}
