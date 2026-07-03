using UnityEngine;
using UnityEditor;
using Dreamteck.Splines;

namespace App.Editor
{
    public class SplinePointsCopier : EditorWindow
    {
        private SplineComputer _source;
        private SplineComputer _destination;
        private bool _copySettings;
        private bool _convertSpace = true;

        [MenuItem("Tools/Spines Tools/Spline Points Copier")]
        private static void ShowWindow()
        {
            var window = GetWindow<SplinePointsCopier>("Spline Copier");
            window.minSize = new Vector2(320, 200);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Spline Points Copier", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Copy all control points from the source spline to the destination spline.", MessageType.Info);

            EditorGUILayout.Space(5);

            _source = (SplineComputer)EditorGUILayout.ObjectField("Source", _source, typeof(SplineComputer), true);
            _destination = (SplineComputer)EditorGUILayout.ObjectField("Destination", _destination, typeof(SplineComputer), true);

            EditorGUILayout.Space(5);

            _convertSpace = EditorGUILayout.ToggleLeft("Auto-convert space (World coords)", _convertSpace);
            _copySettings = EditorGUILayout.ToggleLeft("Also copy spline settings", _copySettings);

            EditorGUILayout.Space(10);

            GUI.enabled = _source != null && _destination != null;
            if (GUILayout.Button("Copy Points", GUILayout.Height(30)))
            {
                CopyPoints();
            }

            if (GUILayout.Button("Copy Points + Settings", GUILayout.Height(30)))
            {
                _copySettings = true;
                CopyPoints();
            }

            GUI.enabled = _source != null && _destination != null;
            if (GUILayout.Button("Swap Source <-> Destination", GUILayout.Height(25)))
            {
                var temp = _source;
                _source = _destination;
                _destination = temp;
            }

            GUI.enabled = true;

            EditorGUILayout.Space(5);

            if (_source != null)
            {
                EditorGUILayout.LabelField($"Source points: {_source.pointCount}");
            }

            if (_destination != null)
            {
                EditorGUILayout.LabelField($"Destination points: {_destination.pointCount}");
            }
        }

        private void CopyPoints()
        {
            if (_source == null || _destination == null)
            {
                Debug.LogError("[SplineCopier] Source or destination is not assigned.");
                return;
            }

            Undo.RecordObject(_destination, "Copy Spline Points");

            var points = _source.GetPoints(_convertSpace ? SplineComputer.Space.World : _source.space);
            _destination.SetPoints(points, _convertSpace ? SplineComputer.Space.World : _source.space);

            if (_copySettings)
            {
                CopySplineSettings();
            }

            _destination.RebuildImmediate();
            EditorUtility.SetDirty(_destination);

            Debug.Log($"[SplineCopier] Copied {points.Length} points from \"{_source.name}\" to \"{_destination.name}\".");
        }

        private void CopySplineSettings()
        {
            if (_destination.type != _source.type)
            {
                _destination.type = _source.type;
            }

            if (_destination.space != _source.space)
            {
                _destination.space = _source.space;
            }

            _destination.is2D = _source.is2D;
            _destination.sampleRate = _source.sampleRate;
            _destination.sampleMode = _source.sampleMode;
            _destination.knotParametrization = _source.knotParametrization;

            if (_source.isClosed && !_destination.isClosed && _destination.pointCount >= 3)
            {
                _destination.Close();
            }
            else if (!_source.isClosed && _destination.isClosed)
            {
                _destination.Break();
            }
        }
    }
}
