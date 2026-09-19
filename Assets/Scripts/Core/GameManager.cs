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
        public event Action Changed;
        public event Action RestartRequested;

        public void Begin(BoardConfig config)
        {
            Score = 0; Moves = config.moves; Target = config.targetScore;
            State = GameState.Playing;
            Changed?.Invoke();
        }

        public void ConsumeMove() { Moves--; Changed?.Invoke(); }
        public void Award(int count, int multiplier) { Score += count * 10 * multiplier; Changed?.Invoke(); }
        public void FinishTurn()
        {
            State = Score >= Target ? GameState.Won : Moves == 0 ? GameState.Lost : GameState.Playing;
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
                gameState = (int)State
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

            Score = snapshot.score;
            Moves = snapshot.moves;
            Target = snapshot.targetScore;
            State = (GameState)snapshot.gameState;
            Changed?.Invoke();
        }
        public void Restart() => RestartRequested?.Invoke();
    }
}
