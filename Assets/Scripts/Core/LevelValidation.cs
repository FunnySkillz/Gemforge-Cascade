using System;
using System.Collections.Generic;

namespace GemforgeCascade.Core
{
    public enum ValidationSeverity
    {
        Warning,
        Error
    }

    public sealed class ValidationIssue
    {
        public ValidationSeverity Severity { get; }
        public string Code { get; }
        public string Message { get; }

        public ValidationIssue(ValidationSeverity severity, string code, string message)
        {
            Severity = severity;
            Code = code;
            Message = message;
        }
    }

    public sealed class LevelValidationResult
    {
        private readonly List<ValidationIssue> issues = new List<ValidationIssue>();

        public IReadOnlyList<ValidationIssue> Issues => issues;
        public int LegalMoveCount { get; internal set; }
        public bool IsValid
        {
            get
            {
                foreach (ValidationIssue issue in issues)
                    if (issue.Severity == ValidationSeverity.Error)
                        return false;
                return true;
            }
        }

        internal void Error(string code, string message) =>
            issues.Add(new ValidationIssue(ValidationSeverity.Error, code, message));

        internal void Warning(string code, string message) =>
            issues.Add(new ValidationIssue(ValidationSeverity.Warning, code, message));

        public bool HasCode(string code)
        {
            foreach (ValidationIssue issue in issues)
                if (issue.Code == code)
                    return true;
            return false;
        }
    }

    public sealed class BoardSimulationResult
    {
        public int TurnsPlayed { get; internal set; }
        public int Score { get; internal set; }
        public int PiecesCleared { get; internal set; }
        public int CascadeSteps { get; internal set; }
        public int Reshuffles { get; internal set; }
        public bool ObjectivesComplete { get; internal set; }
    }

    public static class LevelValidator
    {
        public static LevelValidationResult Validate(LevelDefinition source)
        {
            var result = new LevelValidationResult();
            if (source == null)
            {
                result.Error("LEVEL_NULL", "Level definition is missing.");
                return result;
            }

            try
            {
                GameDataMigrations.Upgrade(source);
            }
            catch (Exception exception)
            {
                result.Error("VERSION_UNSUPPORTED", exception.Message);
                return result;
            }

            if (string.IsNullOrWhiteSpace(source.id))
                result.Error("ID_REQUIRED", "Level ID is required.");
            if (source.width < 3 || source.width > 16 || source.height < 3 || source.height > 16)
                result.Error("DIMENSIONS_INVALID", "Board width and height must be between 3 and 16.");
            if (source.colorCount < 3 || source.colorCount > 6)
                result.Error("COLOR_COUNT_INVALID", "Color count must be between 3 and 6.");
            if (source.moves <= 0)
                result.Error("MOVES_INVALID", "Move count must be positive.");
            if (source.targetScore <= 0 && source.objectives.Length == 0)
                result.Error("OBJECTIVE_REQUIRED", "Provide a score target or at least one objective.");

            if (!ValidOptionalGridLength(source.startingPieces, source.width, source.height))
                result.Error("PIECE_COUNT_INVALID", "Starting pieces must be empty or match board dimensions.");
            if (!ValidOptionalGridLength(source.startingLayers, source.width, source.height))
                result.Error("LAYER_COUNT_INVALID", "Starting layers must be empty or match board dimensions.");

            ValidateObjectives(source, result);
            if (!result.IsValid)
                return result;

            BoardModel board;
            try
            {
                board = BoardModel.FromLevel(source);
            }
            catch (Exception exception)
            {
                result.Error("BOARD_DATA_INVALID", exception.Message);
                return result;
            }

            if (board.FindMatches().Count > 0)
                result.Error("STARTING_MATCH", "Starting board contains an automatic match.");

            result.LegalMoveCount = CountLegalMoves(board);
            if (result.LegalMoveCount == 0)
                result.Error("DEAD_BOARD", "Starting board has no legal swap.");

            if (result.IsValid)
            {
                BoardSimulationResult firstTurn = BoardSimulator.Run(source, 1);
                if (firstTurn.ObjectivesComplete)
                    result.Warning("ONE_MOVE_COMPLETION", "The deterministic first legal move completes the level.");
            }

            if (source.objectives.Length == 0)
                result.Warning("LEGACY_SCORE_GOAL", "Level uses the prototype score target instead of explicit objectives.");

            return result;
        }

        private static bool ValidOptionalGridLength(Array values, int width, int height)
        {
            if (values == null || values.Length == 0)
                return true;
            if (width <= 0 || height <= 0)
                return false;
            return values.Length == width * height;
        }

        private static void ValidateObjectives(LevelDefinition source, LevelValidationResult result)
        {
            foreach (ObjectiveData objective in source.objectives)
            {
                if (objective == null)
                {
                    result.Error("OBJECTIVE_NULL", "Objective entry is missing.");
                    continue;
                }
                if (!Enum.IsDefined(typeof(ObjectiveKind), objective.kind))
                    result.Error("OBJECTIVE_KIND_INVALID", $"Unknown objective kind {objective.kind}.");
                if (objective.target <= 0)
                    result.Error("OBJECTIVE_TARGET_INVALID", "Objective targets must be positive.");
                if (objective.kind == (int)ObjectiveKind.CollectColor &&
                    (objective.colorIndex < 0 || objective.colorIndex >= source.colorCount))
                    result.Error("OBJECTIVE_COLOR_INVALID", "Collect objective uses an unavailable color.");
            }
        }

        private static int CountLegalMoves(BoardModel board)
        {
            int count = 0;
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    for (int direction = 0; direction < 2; direction++)
                    {
                        int targetX = x + (direction == 0 ? 1 : 0);
                        int targetY = y + (direction == 1 ? 1 : 0);
                        if (!board.Contains(targetX, targetY))
                            continue;
                        if (!board.TrySwap(x, y, targetX, targetY))
                            continue;
                        count++;
                        board.Swap(x, y, targetX, targetY);
                    }
                }
            }
            return count;
        }
    }

    public static class BoardSimulator
    {
        public static BoardSimulationResult Run(LevelDefinition source, int maximumTurns)
        {
            if (maximumTurns < 0)
                throw new ArgumentOutOfRangeException(nameof(maximumTurns));

            LevelDefinition level = GameDataMigrations.Upgrade(source);
            BoardModel board = BoardModel.FromLevel(level);
            var tracker = new ObjectiveTracker(level.objectives, level.colorCount);
            var result = new BoardSimulationResult();

            for (int turn = 0; turn < maximumTurns; turn++)
            {
                if (!TryFirstLegalSwap(board))
                {
                    board.Generate();
                    result.Reshuffles++;
                    if (!TryFirstLegalSwap(board))
                        break;
                }

                result.TurnsPlayed++;
                int multiplier = 1;
                MatchResult matches = board.FindMatches();
                while (matches.Count > 0)
                {
                    ClearResult clear = board.Clear(matches.Cells);
                    result.PiecesCleared += clear.PieceCount;
                    result.Score += clear.PieceCount * 10 * multiplier++;
                    result.CascadeSteps++;
                    tracker.Apply(clear, result.Score);
                    board.CollapseAndRefill();
                    matches = board.FindMatches();
                }

                result.ObjectivesComplete = tracker.Objectives.Count > 0
                    ? tracker.Complete
                    : result.Score >= level.targetScore;
                if (result.ObjectivesComplete)
                    break;
            }

            return result;
        }

        private static bool TryFirstLegalSwap(BoardModel board)
        {
            for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++)
                    for (int direction = 0; direction < 2; direction++)
                    {
                        int targetX = x + (direction == 0 ? 1 : 0);
                        int targetY = y + (direction == 1 ? 1 : 0);
                        if (board.Contains(targetX, targetY) && board.TrySwap(x, y, targetX, targetY))
                            return true;
                    }
            return false;
        }
    }
}
