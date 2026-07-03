using System.Collections.Generic;
using UnityEngine;
using Dreamteck.Splines;

namespace App
{
    public class SplinePrefabSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject _prefab;
        [SerializeField] private float _spacing = 1f;
        [SerializeField] private Transform _parent;
        [SerializeField] private bool _alignToSpline = true;

        [Header("Behaviour")]
        [SerializeField] private bool _spawnOnStart = true;

        private SplineComputer _spline;

        private readonly List<GameObject> _spawnedObjects = new List<GameObject>();

        public SplineComputer Spline
        {
            get => _spline;
            set => _spline = value;
        }

        public float Spacing
        {
            get => _spacing;
            set => _spacing = value;
        }

        private void Start()
        {
            if (_spawnOnStart)
            {
                Spawn();
            }
        }

        /// <summary>
        /// Spawns the prefab along the spline with the configured spacing.
        /// Existing spawned objects will be cleared first.
        /// </summary>
        public void Spawn()
        {
            Clear();

            if (_spline == null || _prefab == null || _spacing <= 0f)
            {
                return;
            }

            var totalLength = _spline.CalculateLength();
            var count = Mathf.FloorToInt(totalLength / _spacing) + 1;

            var parent = _parent != null ? _parent : transform;

            for (var i = 0; i < count; i++)
            {
                var distance = i * _spacing;
                var percent = _spline.Travel(0.0, distance);
                var sample = _spline.Evaluate(percent);

                var instance = Instantiate(_prefab, sample.position, _alignToSpline ? sample.rotation : Quaternion.identity, parent);
                _spawnedObjects.Add(instance);
            }
        }

        /// <summary>
        /// Destroys all previously spawned objects.
        /// </summary>
        public void Clear()
        {
            for (var i = _spawnedObjects.Count - 1; i >= 0; i--)
            {
                if (_spawnedObjects[i] != null)
                {
                    Destroy(_spawnedObjects[i]);
                }
            }

            _spawnedObjects.Clear();
        }

        /// <summary>
        /// Clears and re-spawns all objects.
        /// </summary>
        public void Respawn()
        {
            Clear();
            Spawn();
        }
    }
}
