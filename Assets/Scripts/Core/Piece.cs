using UnityEngine;

namespace GemforgeCascade.Core
{
    public sealed class Piece : MonoBehaviour
    {
        public Vector2Int GridPosition { get; private set; }
        public PieceType Type { get; private set; }
        public bool IsSelected { get; private set; }
        public bool IsMoving { get; private set; }
        private float size;
        private Vector3 start;
        private Vector3 target;

        public void Initialize(int type, Sprite sprite, Color color, float cellSize)
        {
            Type = (PieceType)type;
            size = cellSize * 0.82f;
            var visual = gameObject.AddComponent<SpriteRenderer>();
            visual.sprite = sprite;
            visual.color = color;
            visual.sortingOrder = 1;
            SetSelected(false);
        }

        public void Place(int x, int y, Vector3 position, bool instant)
        {
            GridPosition = new Vector2Int(x, y);
            name = $"{Type} ({x}, {y})";
            start = transform.position;
            target = position;
            IsMoving = !instant && start != target;
            if (instant) transform.position = target;
        }

        public void Animate(float progress)
        {
            if (!IsMoving) return;
            transform.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0, 1, progress));
            if (progress >= 1) IsMoving = false;
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            transform.localScale = Vector3.one * size * (selected ? 1.14f : 1);
        }
        public void Shrink(float progress) => transform.localScale = Vector3.one * size * (1 - progress);
    }
}
