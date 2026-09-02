using UnityEngine;
using UnityEditor;
using TowerDefense.Placement.Editor;
namespace TowerDefence.Placement.Editor
{
    [CustomEditor(typeof(TowerPlacementGenerator))]
    public class TowerPlacementGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            TowerPlacementGenerator generator = (TowerPlacementGenerator)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Analyze Terrain"))
                    generator.AnalyzeTerrain();

                if (GUILayout.Button("Generate platforms"))
                    generator.GeneratePlatforms();

                if (GUILayout.Button("Clear Platforms"))
                    generator.ClearPlatforms();
            }

            EditorGUILayout.HelpBox($"Valid Points  Cached: {generator.ValidPoints.Count}", MessageType.Info);
        }
    }
}
