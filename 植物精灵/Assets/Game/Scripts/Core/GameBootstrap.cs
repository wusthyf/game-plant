using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

namespace PlantSpirit.GGJ
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap Instance { get; private set; }
        public GameStateController State { get; } = new GameStateController();
        public GameSession Session { get; } = new GameSession();
        private bool loading;
        private Coroutine deathFreezeRoutine;
        private Coroutine hitStopRoutine;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            GameAudio.Ensure();
            State.Changed += ApplyTimeScale;
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "-plantspirit-smoke")) gameObject.AddComponent<RuntimeSmokeDriver>();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            State.Changed -= ApplyTimeScale;
            Instance = null;
        }

        private void Update() => Session.Tick(Time.deltaTime);

        public void StartLevel()
        {
            LoadLevel("Level01");
        }

        public void LoadLevel(string sceneName)
        {
            if (!string.IsNullOrEmpty(sceneName)) BeginLoad(sceneName);
        }

        public void ReturnToMenu()
        {
            BeginLoad("MainMenu");
        }

        private void BeginLoad(string sceneName)
        {
            if (loading) return;
            loading = true;
            State.SetState(GameState.Loading);
            Time.timeScale = 1f;
            StartCoroutine(LoadScene(sceneName));
        }

        private IEnumerator LoadScene(string sceneName)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            while (operation != null && !operation.isDone) yield return null;
        }

        public void BeginRun()
        {
            loading = false;
            Session.BeginRun();
            State.SetState(GameState.Playing);
        }

        public void ContinueRun()
        {
            loading = false;
            State.SetState(GameState.Playing);
        }

        public void FinishRun()
        {
            Session.CompleteRun();
            State.SetState(GameState.Result);
        }

        public void RequestHitStop(float duration)
        {
            if (State.Current != GameState.Playing || duration <= 0f || Mathf.Approximately(Time.timeScale, 0f)) return;
            if (hitStopRoutine != null) StopCoroutine(hitStopRoutine);
            Time.timeScale = 0f;
            hitStopRoutine = StartCoroutine(ReleaseHitStop(duration));
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "MainMenu")
            {
                loading = false;
                State.SetState(GameState.MainMenu);
            }
        }

        private void ApplyTimeScale(GameState state)
        {
            if (hitStopRoutine != null)
            {
                StopCoroutine(hitStopRoutine);
                hitStopRoutine = null;
            }
            if (deathFreezeRoutine != null)
            {
                StopCoroutine(deathFreezeRoutine);
                deathFreezeRoutine = null;
            }
            if (state == GameState.Dead)
            {
                Time.timeScale = 1f;
                deathFreezeRoutine = StartCoroutine(FreezeAfterDeathDelay());
                return;
            }
            Time.timeScale = state == GameState.Playing || state == GameState.Loading || state == GameState.MainMenu ? 1f : 0f;
        }

        private IEnumerator ReleaseHitStop(float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            hitStopRoutine = null;
            if (State.Current == GameState.Playing) Time.timeScale = 1f;
        }

        private IEnumerator FreezeAfterDeathDelay()
        {
            yield return new WaitForSecondsRealtime(.8f);
            deathFreezeRoutine = null;
            if (State.Current == GameState.Dead) Time.timeScale = 0f;
        }
    }

    public sealed class SceneMarker : MonoBehaviour { }
}
