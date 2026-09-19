using UnityEngine;

namespace GemforgeCascade.Core
{
    public sealed class Piece : MonoBehaviour
    {
        public Vector2Int GridPosition { get; private set; }
        public PieceColor Color { get; private set; }
        public SpecialKind Special { get; private set; }
        public bool IsSelected { get; private set; }
        public bool IsMoving { get; private set; }
        private float size;
        private SpriteRenderer specialMarker;
        private Vector3 start;
        private Vector3 target;

        public void Initialize(BoardPiece state, Sprite sprite, Color color, float cellSize)
        {
            Color = (PieceColor)state.ColorIndex;
            Special = state.Special;
            size = cellSize * 0.82f;
            var visual = gameObject.AddComponent<SpriteRenderer>();
            visual.sprite = sprite;
            visual.color = color;
            visual.sortingOrder = 1;
            var markerObject = new GameObject("Special Marker");
            markerObject.transform.SetParent(transform, false);
            specialMarker = markerObject.AddComponent<SpriteRenderer>();
            specialMarker.sprite = sprite;
            specialMarker.color = new Color(1f, 1f, 1f, 0.9f);
            specialMarker.sortingOrder = 2;
            SetState(state);
            SetSelected(false);
        }

        public void SetState(BoardPiece state)
        {
            Color = (PieceColor)state.ColorIndex;
            Special = state.Special;
            specialMarker.enabled = Special == SpecialKind.RowClear || Special == SpecialKind.ColumnClear;
            specialMarker.transform.localScale = Special == SpecialKind.RowClear
                ? new Vector3(0.62f, 0.12f, 1f)
                : new Vector3(0.12f, 0.62f, 1f);
        }

        public void Place(int x, int y, Vector3 position, bool instant)
        {
            GridPosition = new Vector2Int(x, y);
            name = Special == SpecialKind.None
                ? $"{Color} ({x}, {y})"
                : $"{Color} {Special} ({x}, {y})";
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
