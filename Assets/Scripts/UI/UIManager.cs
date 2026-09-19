using GemforgeCascade.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GemforgeCascade.UI
{
    public sealed class UIManager : MonoBehaviour
    {
        private GameManager game;
        private BoardManager board;
        private Text title, score, moves, goals, chain, result, detail;
        private Image progress;
        private GameObject overlay, resume, next, retry, settings, confirm;
        private RectTransform safeRoot;
        private Font font;
        private bool confirming;
        private Toggle motionToggle, hintsToggle;
        private readonly Color accent = new Color(0.3f, 0.86f, 0.65f);

        public void Initialize(GameManager manager, BoardManager owner)
        {
            game = manager; board = owner;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("HUD Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 960);
            scaler.matchWidthOrHeight = 0.5f;
            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            safeRoot = new GameObject("Safe Area", typeof(RectTransform)).GetComponent<RectTransform>();
            safeRoot.SetParent(canvasObject.transform, false);
            title = Label(safeRoot, "Gemforge Cascade", 30, .05f, .935f, .84f, .99f);
            var pause = Button(safeRoot, "", .86f, .93f, .96f, .99f, () => board.SetPaused(true));
            pause.name = "Pause";
            Panel(pause.transform, Color.white, .35f, .25f, .45f, .75f).raycastTarget = false;
            Panel(pause.transform, Color.white, .55f, .25f, .65f, .75f).raycastTarget = false;
            score = Label(safeRoot, "", 23, .04f, .87f, .40f, .93f);
            moves = Label(safeRoot, "", 23, .60f, .87f, .96f, .93f);
            chain = Label(safeRoot, "", 21, .40f, .87f, .60f, .93f);
            chain.color = accent;
            goals = Label(safeRoot, "", 23, .05f, .80f, .95f, .865f);
            Panel(safeRoot, new Color(.17f, .21f, .23f), .08f, .78f, .92f, .788f);
            progress = Panel(safeRoot, accent, .08f, .78f, .08f, .788f);
            overlay = Panel(safeRoot, new Color(.045f, .058f, .063f, .97f), 0, 0, 1, 1).gameObject;
            result = Label(overlay.transform, "", 36, .08f, .72f, .92f, .84f);
            detail = Label(overlay.transform, "", 24, .08f, .61f, .92f, .72f);
            resume = Button(overlay.transform, "Resume", .22f, .48f, .78f, .57f, () => { confirming = false; board.SetPaused(false); });
            next = Button(overlay.transform, "Next Level", .22f, .48f, .78f, .57f, board.NextLevel);
            retry = Button(overlay.transform, "Retry", .22f, .36f, .78f, .45f, () =>
            {
                if (game.State == GameState.Playing) { confirming = true; Refresh(); }
                else board.Restart();
            });
            confirm = Button(overlay.transform, "Restart Level", .22f, .48f, .78f, .57f, () => { confirming = false; board.Restart(); });
            settings = new GameObject("Settings", typeof(RectTransform));
            settings.transform.SetParent(overlay.transform, false);
            Stretch(settings.GetComponent<RectTransform>(), 0, .05f, 1, .32f);
            motionToggle = Toggle(settings.transform, "Reduced motion", .5f, board.SetReducedMotion);
            hintsToggle = Toggle(settings.transform, "Hints", 0, board.SetHints);
            game.Changed += Refresh;
        }

        private void Update()
        {
            Rect safe = Screen.safeArea;
            Stretch(safeRoot, safe.xMin / Screen.width, safe.yMin / Screen.height,
                safe.xMax / Screen.width, safe.yMax / Screen.height);
        }

        public void Refresh()
        {
            if (board.Level == null) return;
            title.text = $"{board.LevelIndex + 1:00}  {board.Level.title}";
            score.text = $"Score  {game.Score:N0}";
            moves.text = $"Moves  {game.Moves}";
            moves.color = game.Moves <= 5 ? new Color(1f, .68f, .35f) : Color.white;
            chain.text = game.Chain > 1 ? $"x{game.Chain}" : "";
            float fraction = 0;
            string description = "";
            foreach (var objective in game.Objectives.Objectives)
            {
                string name = objective.Kind == ObjectiveKind.CollectColor ? ((PieceColor)objective.ColorIndex).ToString() :
                    objective.Kind == ObjectiveKind.ClearLayers ? "Crystal" : "Score";
                if (description.Length > 0) description += "    ";
                description += $"{name}  {objective.Current}/{objective.Target}";
                fraction += (float)objective.Current / objective.Target;
            }
            if (game.Objectives.Objectives.Count == 0)
            {
                description = $"Target  {game.Score}/{game.Target}";
                fraction = Mathf.Clamp01((float)game.Score / game.Target);
            }
            else fraction /= game.Objectives.Objectives.Count;
            goals.text = description;
            Stretch(progress.rectTransform, .08f, .78f, .08f + .84f * fraction, .788f);
            bool ended = game.State != GameState.Playing;
            overlay.SetActive(ended || board.Paused);
            if (!ended && !board.Paused) confirming = false;
            result.text = confirming ? "Restart this level?" : !ended ? "Paused" :
                game.State == GameState.Won ? "Level Complete" : "Out of Moves";
            detail.text = confirming ? "This attempt will be reset." : $"Score  {game.Score:N0}\n{description}";
            resume.SetActive(!ended && !confirming);
            next.SetActive(game.State == GameState.Won && board.LevelIndex + 1 < board.LevelCount);
            retry.SetActive(!confirming);
            confirm.SetActive(confirming);
            settings.SetActive(!ended && !confirming);
            if (confirming)
            {
                retry.SetActive(true);
                retry.GetComponentInChildren<Text>().text = "Cancel";
                retry.GetComponent<Button>().onClick.RemoveAllListeners();
                retry.GetComponent<Button>().onClick.AddListener(() => { confirming = false; Refresh(); });
            }
            else
            {
                retry.GetComponentInChildren<Text>().text = ended ? "Replay" : "Restart";
                retry.GetComponent<Button>().onClick.RemoveAllListeners();
                retry.GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (game.State == GameState.Playing) { confirming = true; Refresh(); }
                    else board.Restart();
                });
            }
            motionToggle.SetIsOnWithoutNotify(board.ReducedMotion);
            hintsToggle.SetIsOnWithoutNotify(board.HintsEnabled);
        }

        private Text Label(Transform parent, string value, int size, float x, float y, float right, float top)
        {
            var obj = new GameObject(value.Length > 0 ? value : "Label", typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            var text = obj.GetComponent<Text>();
            text.font = font; text.text = value; text.fontSize = size;
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 12; text.resizeTextMaxSize = size;
            text.alignment = TextAnchor.MiddleCenter; text.color = Color.white; text.raycastTarget = false;
            Stretch(text.rectTransform, x, y, right, top);
            return text;
        }

        private Image Panel(Transform parent, Color color, float x, float y, float right, float top)
        {
            var obj = new GameObject("Surface", typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            var image = obj.GetComponent<Image>(); image.color = color;
            Stretch(image.rectTransform, x, y, right, top);
            return image;
        }

        private GameObject Button(Transform parent, string text, float x, float y, float right, float top, UnityEngine.Events.UnityAction action)
        {
            var image = Panel(parent, new Color(.12f, .39f, .30f), x, y, right, top);
            image.name = text;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image; button.onClick.AddListener(action);
            Label(image.transform, text, 26, .06f, .05f, .94f, .95f);
            return image.gameObject;
        }

        private Toggle Toggle(Transform parent, string text, float y, UnityEngine.Events.UnityAction<bool> action)
        {
            var root = Panel(parent, new Color(.12f, .15f, .16f), .22f, y + .08f, .78f, y + .42f);
            var toggle = root.gameObject.AddComponent<Toggle>();
            var box = Panel(root.transform, new Color(.38f, .44f, .46f), .1f, .5f, .1f, .5f);
            box.rectTransform.sizeDelta = new Vector2(24, 24);
            var check = Panel(box.transform, accent, .14f, .14f, .86f, .86f);
            toggle.targetGraphic = root; toggle.graphic = check; toggle.onValueChanged.AddListener(action);
            Label(root.transform, text, 22, .21f, .05f, .95f, .95f);
            return toggle;
        }

        private static void Stretch(RectTransform rect, float x, float y, float right, float top)
        {
            rect.anchorMin = new Vector2(x, y); rect.anchorMax = new Vector2(right, top);
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }
        private void OnDestroy() { if (game != null) game.Changed -= Refresh; }
    }
}
