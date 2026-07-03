using System.Collections;
using AFramework;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace App
{
    /// <summary>
    /// Quản lý UI Loading khi mở app với thanh progress bar.
    /// Progress được cập nhật bằng cách thay đổi width của RectTransform,
    /// hỗ trợ cả set thẳng (force) và animate bằng DOTween.
    /// Fake loading chia thành 3 đoạn với tổng thời gian random 5-7 giây.
    /// </summary>
    public class OpenAppLoadingManager : ManualSingletonMono<OpenAppLoadingManager>
    {
        [Header("Progress Bar")]
        [SerializeField] private RectTransform _progressBarRect;
        [Tooltip("Thời gian animation DOTween cho mỗi lần UpdateProgress (force=false)")]
        [SerializeField] private float _progressAnimDuration = 0.3f;
        [SerializeField] private Ease _progressEase = Ease.OutCubic;

        [Header("Fake Loading Config")]
        [SerializeField] private float _minLoadTime = 5f;
        [SerializeField] private float _maxLoadTime = 7f;

        [Header("Scene Config")]
        [SerializeField] private string _mainSceneName = "Main";

        private float _maxProgressWidth;
        private Tweener _progressTween;
        private Coroutine _loadMainSceneCoroutine;
        private AsyncOperation _loadMainSceneOp;

        private void Start()
        {
            if (_progressBarRect == null)
            {
                Debug.LogError($"[{nameof(OpenAppLoadingManager)}] ProgressBarRect chưa được gán.");
                return;
            }

            _maxProgressWidth = _progressBarRect.sizeDelta.x;

            LoadMainScene();
            StartFakeLoading();
        }

        #region Public Methods

        /// <summary>
        /// Cập nhật tiến trình hiển thị của thanh progress bar.
        /// </summary>
        /// <param name="progress">Giá trị từ 0 đến 1, thể hiện mức độ hoàn thành.</param>
        /// <param name="force">
        /// true  → set thẳng width ngay lập tức.
        /// false → animate width mượt bằng DOTween.
        /// </param>
        public void UpdateProgress(float progress, bool force)
        {
            if (_progressBarRect == null) return;

            progress = Mathf.Clamp01(progress);
            float targetWidth = Mathf.Lerp(0f, _maxProgressWidth, progress);

            KillTween();

            if (force)
            {
                SetWidthDirect(targetWidth);
            }
            else
            {
                Vector2 targetSize = new Vector2(targetWidth, _progressBarRect.sizeDelta.y);
                _progressTween = _progressBarRect
                    .DOSizeDelta(targetSize, _progressAnimDuration)
                    .SetEase(_progressEase);
            }
        }

        /// <summary>
        /// Bắt đầu quá trình fake loading (làm giả).
        /// Tổng thời gian load random từ _minLoadTime đến _maxLoadTime,
        /// chia thành 3 đoạn load xen kẽ 2 đoạn nghỉ.
        /// </summary>
        public void StartFakeLoading()
        {
            StartCoroutine(FakeLoadingCoroutine());
        }

        /// <summary>
        /// Load MainScene bất đồng bộ sau 2 frame kể từ khi được gọi.
        /// Sử dụng Addressables hoặc LoadSceneAsync tùy project setup.
        /// </summary>
        public void LoadMainScene()
        {
            if (_loadMainSceneCoroutine != null)
            {
                StopCoroutine(_loadMainSceneCoroutine);
                _loadMainSceneCoroutine = null;
            }

            _loadMainSceneCoroutine = StartCoroutine(LoadMainSceneCoroutine());
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Coroutine fake loading với 3 đoạn load + 2 đoạn nghỉ.
        /// </summary>
        private IEnumerator FakeLoadingCoroutine()
        {
            float totalTime = Random.Range(_minLoadTime, _maxLoadTime);

            // Sinh 5 phần cho: load1, pause1, load2, pause2, load3
            float[] timePortions = GenerateRandomPortions(5);
            float load1Duration = timePortions[0] * totalTime;
            float pause1Duration = timePortions[1] * totalTime;
            float load2Duration = timePortions[2] * totalTime;
            float pause2Duration = timePortions[3] * totalTime;
            float load3Duration = timePortions[4] * totalTime;

            // Sinh 3 phần progress cho 3 đoạn load
            float[] progressPortions = GenerateRandomPortions(3);
            float progress1 = progressPortions[0];
            float progress2 = progressPortions[1];
            float progress3 = progressPortions[2];

            Debug.Log($"[{nameof(OpenAppLoadingManager)}] Fake loading bắt đầu. Tổng thời gian: {totalTime:F2}s. " +
                      $"Đoạn: L1={load1Duration:F2}s → N1={pause1Duration:F2}s → L2={load2Duration:F2}s → N2={pause2Duration:F2}s → L3={load3Duration:F2}s");

            // Đoạn load 1: 0 → progress1
            yield return LoadSegment(0f, progress1, load1Duration);

            // Nghỉ 1
            yield return new WaitForSeconds(pause1Duration);

            // Đoạn load 2: progress1 → progress1 + progress2
            yield return LoadSegment(progress1, progress1 + progress2, load2Duration);

            // Nghỉ 2
            yield return new WaitForSeconds(pause2Duration);

            // Đoạn load 3: progress1 + progress2 → 1.0
            yield return LoadSegment(progress1 + progress2, 1f, load3Duration);

            Debug.Log($"[{nameof(OpenAppLoadingManager)}] Fake loading hoàn thành.");

            // Chờ 2 frame rồi destroy LoadingScene
            yield return null;
            yield return null;

            DestroyLoadingScene();
        }

        /// <summary>
        /// Chạy 1 đoạn load, mỗi frame cập nhật progress từ from đến to trong duration giây.
        /// </summary>
        private IEnumerator LoadSegment(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                SetWidthDirect(to);
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float progress = Mathf.Lerp(from, to, t);
                SetWidthDirect(progress);
                yield return null;
            }

            SetWidthDirect(to);
        }

        /// <summary>
        /// Set thẳng width của progress bar mà không cần animation.
        /// </summary>
        private void SetWidthDirect(float progress)
        {
            if (_progressBarRect == null) return;

            float targetWidth = Mathf.Lerp(0f, _maxProgressWidth, Mathf.Clamp01(progress));
            _progressBarRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
        }

        /// <summary>
        /// Coroutine load MainScene bất đồng bộ. Chờ 2 frame rồi bắt đầu load Additive.
        /// </summary>
        private IEnumerator LoadMainSceneCoroutine()
        {
            yield return null;
            yield return null;

            _loadMainSceneOp = SceneManager.LoadSceneAsync(_mainSceneName, LoadSceneMode.Additive);
            _loadMainSceneOp.allowSceneActivation = true;
        }

        /// <summary>
        /// Destroy scene Loading hiện tại sau khi MainScene đã load xong.
        /// </summary>
        private void DestroyLoadingScene()
        {
            StartCoroutine(DestroyLoadingSceneCoroutine());
        }

        private IEnumerator DestroyLoadingSceneCoroutine()
        {
            if (_loadMainSceneOp != null)
            {
                yield return _loadMainSceneOp;
            }

            var currentScene = SceneManager.GetActiveScene();
            SceneManager.UnloadSceneAsync(currentScene);
        }

        /// <summary>
        /// Sinh mảng count phần tử random có tổng = 1.
        /// </summary>
        private float[] GenerateRandomPortions(int count)
        {
            float[] portions = new float[count];
            float sum = 0f;

            for (int i = 0; i < count; i++)
            {
                portions[i] = Random.Range(0.2f, 1f);
                sum += portions[i];
            }

            // Chuẩn hóa
            for (int i = 0; i < count; i++)
            {
                portions[i] /= sum;
            }

            return portions;
        }

        /// <summary>
        /// Kill DOTween tween hiện tại nếu đang chạy.
        /// </summary>
        private void KillTween()
        {
            _progressTween?.Kill();
            _progressTween = null;
        }

        #endregion

        #region Cleanup

        private void OnDestroyOpenAppLoading()
        {
            KillTween();

            if (_loadMainSceneCoroutine != null)
            {
                StopCoroutine(_loadMainSceneCoroutine);
                _loadMainSceneCoroutine = null;
            }
        }

        #endregion
    }
}
