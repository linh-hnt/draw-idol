using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace App
{
    /// <summary>
    /// Represents which chapter a level belongs to.
    /// </summary>
    public enum ChapterName
    {
        BrainBrot,
        Kpop,
        Lababu,
        Valentine,
        NUM,
    }

    /// <summary>
    /// Data container for a single level entry: unique ID, chapter, and level prefab reference.
    /// </summary>
    [System.Serializable]
    public class LevelInfo
    {
        [Tooltip("Unique identifier for this level.")]
        public int id;
        public string levelName;
        public Sprite iconSprite;

        [Tooltip("The chapter this level belongs to.")]
        public ChapterName chapter;

        [Tooltip("The prefab containing the LevelController component for this level.")]
        public GameObject levelPrefab;
    }

    /// <summary>
    /// ScriptableObject that holds the configuration for all levels in the game.
    /// Provides lookup methods for retrieving level info by ID, chapter, etc.
    /// Create via: Assets → Create → App → Level Config
    /// </summary>
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "App/Level Config")]
    public class LevelConfigSO : ScriptableObject
    {
        [Header("Levels")]
        [Tooltip("All levels configured for the game.")]
        [SerializeField] private List<LevelInfo> _levels = new List<LevelInfo>();

        /// <summary>Read-only access to the full level list.</summary>
        public IReadOnlyList<LevelInfo> Levels => _levels;

        /// <summary>
        /// Finds the first level with the given ID.
        /// </summary>
        /// <param name="id">Level ID to search for.</param>
        /// <returns>The matching <see cref="LevelInfo"/>, or null if not found.</returns>
        public LevelInfo GetLevelById(int id)
        {
            for (int i = 0, count = _levels.Count; i < count; i++)
            {
                if (_levels[i].id == id)
                    return _levels[i];
            }

            Debug.LogWarning($"[{nameof(LevelConfigSO)}] No level found with id = {id}.");
            return null;
        }

        /// <summary>
        /// Returns all levels belonging to the specified chapter.
        /// </summary>
        /// <param name="chapter">The chapter filter.</param>
        /// <returns>A list of matching <see cref="LevelInfo"/> entries.</returns>
        public List<LevelInfo> GetLevelsByChapter(ChapterName chapter)
        {
            var result = new List<LevelInfo>();
            for (int i = 0, count = _levels.Count; i < count; i++)
            {
                if (_levels[i].chapter == chapter)
                    result.Add(_levels[i]);
            }

            return result;
        }

        public int GetTotalLevel(ChapterName chapter)
        {
            return GetLevelsByChapter(chapter).Count;
        }

        /// <summary>
        /// Tries to retrieve the level prefab for a given level ID.
        /// </summary>
        /// <param name="id">Level ID to search for.</param>
        /// <param name="prefab">The associated prefab, or null if not found.</param>
        /// <returns>True if a matching level was found; false otherwise.</returns>
        public bool TryGetLevelPrefab(int id, out GameObject prefab)
        {
            var info = GetLevelById(id);
            prefab = info?.levelPrefab;
            return info != null && prefab != null;
        }

        /// <summary>
        /// Returns an array of all level IDs currently configured.
        /// </summary>
        /// <returns>Array of all IDs.</returns>
        public int[] GetAllIds()
        {
            var ids = new int[_levels.Count];
            for (int i = 0; i < _levels.Count; i++)
            {
                ids[i] = _levels[i].id;
            }

            return ids;
        }

        private void SetIndex()
        {
            for (int i = 0, count = _levels.Count; i < count; i++)
            {
                _levels[i].id = i;
            }
        }
    }
}
