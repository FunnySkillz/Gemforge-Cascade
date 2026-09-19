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
        public void Restart() => RestartRequested?.Invoke();
    }
}
