using System;
using UnityEngine;

namespace GemforgeCascade.Core
{
    [Serializable]
    public sealed class BoardConfig
    {
        [Range(3, 16)] public int width = 8;
        [Range(3, 16)] public int height = 8;
        [Range(3, 6)] public int pieceTypes = 6;
        [Min(0.25f)] public float cellSize = 1;
        [Min(1)] public int moves = 25;
        [Min(1)] public int targetScore = 1000;
    }
}
