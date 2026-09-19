using System;

namespace GemforgeCascade.Core
{
    public static class GameDataVersions
    {
        public const int Level = 2;
        public const int Snapshot = 2;
    }

    [Serializable]
    public sealed class CellLayerData
    {
        public int kind;
        public int durability;

        public CellLayerData() { }

        public CellLayerData(CellLayer layer)
        {
            kind = (int)layer.Kind;
            durability = layer.Durability;
        }

        public CellLayer ToCellLayer()
        {
            if (!Enum.IsDefined(typeof(CellLayerKind), kind))
                throw new InvalidOperationException($"Unknown cell layer kind {kind}.");
            return new CellLayer((CellLayerKind)kind, durability);
        }
    }

    [Serializable]
    public sealed class ObjectiveData
    {
        public int kind;
        public int colorIndex = -1;
        public int target = 1;
    }

    [Serializable]
    public sealed class PieceData
    {
        public int colorIndex = -1;
        public int specialKind;

        public PieceData() { }

        public PieceData(BoardPiece piece)
        {
            colorIndex = piece.ColorIndex;
            specialKind = (int)piece.Special;
        }

        public BoardPiece ToBoardPiece()
        {
            if (colorIndex < 0)
                return BoardPiece.Empty;
            if (!Enum.IsDefined(typeof(SpecialKind), specialKind))
                throw new InvalidOperationException($"Unknown special kind {specialKind}.");
            return new BoardPiece(colorIndex, (SpecialKind)specialKind);
        }
    }

    [Serializable]
    public sealed class LevelDefinition
    {
        public int version = GameDataVersions.Level;
        public string id = "level-001";
        public string title = "First Spark";
        public int width = 8;
        public int height = 8;
        public int colorCount = 6;
        public int moves = 25;
        public int targetScore = 1000;
        public int seed = 1;
        public PieceData[] startingPieces = new PieceData[0];
        public CellLayerData[] startingLayers = new CellLayerData[0];
        public ObjectiveData[] objectives = new ObjectiveData[0];
    }

    [Serializable]
    public sealed class BoardSnapshot
    {
        public int version = GameDataVersions.Snapshot;
        public string levelId = string.Empty;
        public int width;
        public int height;
        public int colorCount;
        public uint randomState;
        public PieceData[] pieces = new PieceData[0];
        public CellLayerData[] layers = new CellLayerData[0];
    }

    [Serializable]
    public sealed class AttemptSnapshot
    {
        public int version = GameDataVersions.Snapshot;
        public BoardSnapshot board;
        public int score;
        public int moves;
        public int targetScore;
        public int gameState;
        public int[] objectiveProgress = new int[0];
    }

    public static class GameDataMigrations
    {
        public static LevelDefinition Upgrade(LevelDefinition level)
        {
            if (level == null)
                throw new ArgumentNullException(nameof(level));
            if (level.version < 0 || level.version > GameDataVersions.Level)
                throw new NotSupportedException($"Unsupported level version {level.version}.");

            if (level.version < GameDataVersions.Level)
            {
                level.id = string.IsNullOrWhiteSpace(level.id) ? "legacy-level" : level.id;
                level.version = GameDataVersions.Level;
            }

            level.startingPieces = level.startingPieces ?? new PieceData[0];
            level.startingLayers = level.startingLayers ?? new CellLayerData[0];
            level.objectives = level.objectives ?? new ObjectiveData[0];

            return level;
        }

        public static BoardSnapshot Upgrade(BoardSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (snapshot.version < 0 || snapshot.version > GameDataVersions.Snapshot)
                throw new NotSupportedException($"Unsupported snapshot version {snapshot.version}.");

            if (snapshot.version < GameDataVersions.Snapshot)
            {
                snapshot.levelId = string.IsNullOrWhiteSpace(snapshot.levelId) ? "legacy-level" : snapshot.levelId;
                snapshot.version = GameDataVersions.Snapshot;
            }

            snapshot.pieces = snapshot.pieces ?? new PieceData[0];
            snapshot.layers = snapshot.layers ?? new CellLayerData[0];

            return snapshot;
        }
    }
}
