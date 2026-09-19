using GemforgeCascade.Core;
using TMPro;
using UnityEngine;

namespace GemforgeCascade.UI
{
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private GemBoard board;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text movesText;

        private void Awake()
        {
            board.ScoreChanged.AddListener(score => scoreText.text = $"Score {score}");
            board.MovesChanged.AddListener(moves => movesText.text = $"Moves {moves}");
        }
    }
}
