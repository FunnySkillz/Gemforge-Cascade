#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace GemforgeCascade.Core
{
    public sealed class DevelopmentSmoke : MonoBehaviour
    {
        private bool failed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-gemforgeSmoke") >= 0)
            {
                Application.runInBackground = true;
                new GameObject("Development Smoke").AddComponent<DevelopmentSmoke>();
            }
        }

        private void OnEnable() => Application.logMessageReceived += OnLog;
        private void OnDisable() => Application.logMessageReceived -= OnLog;
        private void OnLog(string message, string trace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) failed = true;
        }
        private IEnumerator Start()
        {
            yield return null;
            var board = FindObjectOfType<BoardManager>();
            Require(board != null && board.Model != null, "Scene did not start");
            Directory.CreateDirectory("Logs");
            board.SetPaused(false);
            yield return new WaitForSeconds(.4f);
            Capture("board");
            yield return new WaitForSeconds(.4f);
            var initial = board.Model.CreateSnapshot(board.Level.id);
            Require(board.Model.TryGetLegalMove(out int from, out int to), "No opening move");
            board.SetPaused(true);
            Require(!board.AttemptMove(from, to), "Paused board accepted input");
            Capture("pause");
            yield return new WaitForSeconds(.4f);
            board.SetPaused(false);
            int moves = board.Game.Moves;
            Require(board.AttemptMove(from, to), "Legal move rejected");
            float timeout = Time.realtimeSinceStartup + 30;
            while (board.IsBusy && Time.realtimeSinceStartup < timeout) yield return null;
            Require(!board.IsBusy && board.Game.Moves == moves - 1 && board.Game.Score > 0, "Turn did not settle");
            Require(board.Model.FindMatches().Count == 0, "Unresolved matches");
            var pieces = board.GetComponentsInChildren<Piece>();
            Require(pieces.Length == board.Model.Width * board.Model.Height, "Visual/model piece count differs");
            board.Restart();
            Require(board.Game.Moves == moves && board.Game.Score == 0, "Retry failed");
            Require(JsonUtility.ToJson(initial) == JsonUtility.ToJson(board.Model.CreateSnapshot(board.Level.id)), "Retry changed seed/layout");
            foreach (TextAsset asset in LevelCatalog.LoadAll())
            {
                var level = LevelCatalog.Parse(asset);
                var sim = BoardSimulator.Run(level, level.moves);
                Require(sim.TurnsPlayed > 0 && sim.TurnsPlayed <= level.moves, "Chapter simulation failed");
            }
            Debug.Log(failed ? "GEMFORGE_SMOKE_FAIL" : "GEMFORGE_SMOKE_PASS");
            Application.Quit(failed ? 1 : 0);
        }
        private void Require(bool condition, string message)
        {
            if (condition) return;
            failed = true;
            Debug.LogError(message);
        }

        // Hidden Windows players can skip swap-chain rendering. Render the same scene/UI offscreen.
        private void Capture(string name)
        {
            var canvas = FindObjectOfType<Canvas>();
            var uiTransforms = canvas.GetComponentsInChildren<Transform>(true);
            var originalLayers = new int[uiTransforms.Length];
            for (int i = 0; i < uiTransforms.Length; i++)
            {
                originalLayers[i] = uiTransforms[i].gameObject.layer;
                uiTransforms[i].gameObject.layer = 5;
            }
            var uiCamera = new GameObject("Capture UI Camera").AddComponent<Camera>();
            uiCamera.clearFlags = CameraClearFlags.Depth;
            uiCamera.cullingMask = 1 << 5;
            uiCamera.orthographic = true;
            uiCamera.depth = 100;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCamera;
            canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases();
            var target = new RenderTexture(Screen.width, Screen.height, 24);
            var cameras = Camera.allCameras;
            Array.Sort(cameras, (a, b) => a.depth.CompareTo(b.depth));
            foreach (var camera in cameras)
            {
                int mask = camera.cullingMask;
                if (camera != uiCamera) camera.cullingMask &= ~(1 << 5);
                camera.targetTexture = target;
                camera.Render();
                camera.targetTexture = null;
                camera.cullingMask = mask;
            }
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var capture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            capture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            capture.Apply();
            File.WriteAllBytes(Path.GetFullPath($"Logs/{name}-{Screen.width}x{Screen.height}.png"), capture.EncodeToPNG());
            int bright = 0;
            foreach (var pixel in capture.GetPixels32()) if (Math.Max(pixel.r, Math.Max(pixel.g, pixel.b)) > 80) bright++;
            Require(bright > Screen.width * Screen.height / 100, "Blank rendered capture");
            RenderTexture.active = previous;
            canvas.worldCamera = null;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            for (int i = 0; i < uiTransforms.Length; i++) uiTransforms[i].gameObject.layer = originalLayers[i];
            Destroy(uiCamera.gameObject);
            Destroy(capture);
            target.Release();
            Destroy(target);
        }
    }
}
#endif
