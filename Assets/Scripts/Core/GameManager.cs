using System;
using UnityEngine;

namespace GemforgeCascade.Core
{
    public enum GameState { Playing, Won, Lost }

    public sealed class GameManager : MonoBehaviour
    {
        public int Score { get; private set; }
        public int Moves { get; private set; }
        public int Target { get; private set; }
        public GameState State { get; private set; }
        public ObjectiveTracker Objectives { get; private set; }
        public string LevelId { get; private set; }
        public int Chain { get; private set; }
        public event Action Changed;
        public event Action RestartRequested;

        public void Begin(BoardConfig config)
        {
            Begin(new LevelDefinition { moves = config.moves, targetScore = config.targetScore });
        }

        public void Begin(LevelDefinition level)
        {
            Score = 0; Moves = level.moves; Target = level.targetScore; Chain = 0;
            LevelId = level.id;
            Objectives = new ObjectiveTracker(level.objectives, level.colorCount);
            State = GameState.Playing;
            Changed?.Invoke();
        }

        public void ConsumeMove() { Moves--; Changed?.Invoke(); }
        public void Award(int count, int multiplier) { Score += count * 10 * multiplier; Changed?.Invoke(); }
        public void Award(ClearResult clear, int multiplier)
        {
            Score += clear.PieceCount * 10 * multiplier;
            Chain = multiplier;
            Objectives.Apply(clear, Score);
            Changed?.Invoke();
        }
        public void FinishTurn()
        {
            bool won = Objectives != null && Objectives.Objectives.Count > 0 ? Objectives.Complete : Score >= Target;
            State = won ? GameState.Won : Moves <= 0 ? GameState.Lost : GameState.Playing;
            Changed?.Invoke();
        }
        public AttemptSnapshot CreateSnapshot(BoardSnapshot board)
        {
            return new AttemptSnapshot
            {
                board = board,
                score = Score,
                moves = Moves,
                targetScore = Target,
                gameState = (int)State,
                objectiveProgress = Objectives == null ? new int[0] : Objectives.Capture()
            };
        }

        public void Restore(AttemptSnapshot snapshot)
        {
            if (snapshot == null || snapshot.board == null)
                throw new ArgumentException("Attempt snapshot must include board state.", nameof(snapshot));
            if (snapshot.version != GameDataVersions.Snapshot)
                throw new NotSupportedException($"Unsupported attempt version {snapshot.version}.");
            if (!Enum.IsDefined(typeof(GameState), snapshot.gameState))
                throw new ArgumentException("Attempt snapshot has an invalid game state.", nameof(snapshot));

            if (snapshot.score < 0 || snapshot.moves < 0 || snapshot.targetScore < 0)
                throw new ArgumentException("Attempt counters cannot be negative.", nameof(snapshot));
            // Validate objective progress before mutating the current attempt.
            Objectives?.Restore(snapshot.objectiveProgress);
            Score = snapshot.score;
            Moves = snapshot.moves;
            Target = snapshot.targetScore;
            State = (GameState)snapshot.gameState;
            Changed?.Invoke();
        }
        public void Restart() => RestartRequested?.Invoke();
    }
}
