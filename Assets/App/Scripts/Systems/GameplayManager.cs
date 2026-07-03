using AFramework;
using App;
using UnityEngine;

public class GameplayManager : ManualSingletonMono<GameplayManager>
{
    [Header("Config")]
    [SerializeField] private App.LevelConfigSO _levelConfig;

    [Header("References")]
    [SerializeField] private Transform _penTransform;
    [SerializeField] private Transform _levelContainer;
    [SerializeField] private App.CameraController _cameraController;

    private App.LevelController _currentLevelController;
    private ChapterName _currentChapter = ChapterName.NUM;
    private int _currentChapterLevelIndex = 0;

    private float _progressLevel = 0;

    public ChapterName CurrentChapter => _currentChapter;
    public int CurrentChapterIndex => _currentChapterLevelIndex;

    /// <summary>Returns the currently active LevelController, or null if no level is loaded.</summary>
    public App.LevelController CurrentLevelController => _currentLevelController;

    public float ProgressLevel
    {
        get => _progressLevel;
        set
        {
            _progressLevel = value;
        }
    }

    private void Start()
    {
        if (_levelConfig == null)
        {
            Debug.LogError($"[{nameof(GameplayManager)}] LevelConfigSO is not assigned.");
            return;
        }

        EventManager.I.CloseLevelAction += ClearCurrentLevel;
        EventManager.I.CompleteLevelAction += OnCompleteLevelAction;
    }

    /// <summary>
    /// Loads a level by ID from LevelConfigSO, instantiates the prefab,
    /// and assigns the Pen transform to the LevelController's PenTrailDrawers.
    /// </summary>
    public void LoadLevel(int id)
    {
        if (_levelConfig == null)
        {
            Debug.LogError($"[{nameof(GameplayManager)}] LevelConfigSO is not assigned.");
            return;
        }

        if (!_levelConfig.TryGetLevelPrefab(id, out GameObject prefab))
        {
            Debug.LogWarning($"[{nameof(GameplayManager)}] Level with id={id} not found in LevelConfigSO.");
            return;
        }

        ClearCurrentLevel();

        _progressLevel = 0f;

        var levelInstance = Instantiate(prefab, _levelContainer);
        _currentLevelController = levelInstance.GetComponent<App.LevelController>();

        if (_currentLevelController != null && _penTransform != null)
        {
            _currentLevelController.SetPenTransform(_penTransform);

            _penTransform.gameObject.SetActive(true);
        }

        if (_currentLevelController != null && _cameraController != null)
        {
            _currentLevelController.SetCameraController(_cameraController);
        }

        if (_currentLevelController != null)
        {
            _currentLevelController.Begin();
        }
    }

    public void SetChapterData(ChapterName chapter, int chapterLevelIndex)
    {
        _currentChapter = chapter;
        _currentChapterLevelIndex = chapterLevelIndex;
    }

    private void OnCompleteLevelAction()
    {
        int maxLevelIndex = SaveManager.I.GetChapterCurrentLevelIndex(_currentChapter);
        if (_currentChapterLevelIndex >= maxLevelIndex)
            SaveManager.I.SetChapterLevelIndex(_currentChapter, _currentChapterLevelIndex + 1);
    }

    private void ClearCurrentLevel()
    {
        if (_levelContainer == null) return;
        for (int i = _levelContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(_levelContainer.GetChild(i).gameObject);
        }

        _currentLevelController = null;

        if (_penTransform)
        {
            _penTransform.localPosition = Vector3.zero;
            _penTransform.gameObject.SetActive(false);
        }
    }
}
