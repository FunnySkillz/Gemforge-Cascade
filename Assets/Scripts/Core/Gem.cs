using UnityEngine;

namespace GemforgeCascade.Core
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class Gem : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        public Vector2Int GridPosition { get; private set; }
        public GemType Type { get; private set; }

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        public void Initialize(GemType type, Vector2Int gridPosition, Color color)
        {
            Type = type;
            GridPosition = gridPosition;
            spriteRenderer.color = color;
            name = $"{type} Gem ({gridPosition.x},{gridPosition.y})";
        }

        public void SetGridPosition(Vector2Int gridPosition, Vector3 worldPosition)
        {
            GridPosition = gridPosition;
            transform.position = worldPosition;
            name = $"{Type} Gem ({gridPosition.x},{gridPosition.y})";
        }

        public void SetSelected(bool selected)
        {
            transform.localScale = selected ? Vector3.one * 1.12f : Vector3.one;
        }
    }
}
