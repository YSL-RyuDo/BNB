using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ReviewBuild
{
    [Serializable] public class Entry { public string path; public int objects, missingScripts, brokenReferences, unreadableOutlineMeshes; }
    [Serializable] public class Result {
        public string unityVersion, buildResult;
        public int buildErrors, buildWarnings;
        public ulong buildBytes;
        public List<Entry> scenes = new List<Entry>();
        public List<Entry> prefabs = new List<Entry>();
        public List<string> issues = new List<string>();
    }
    static Result result;
    static void Inspect(GameObject root, Entry entry) {
        // CharacterSystem adds Outline at runtime; every child mesh must permit CPU access.
        if (root.GetComponent<LocalPlayerController>() != null) {
            var meshes = root.GetComponentsInChildren<MeshFilter>(true).Select(f => f.sharedMesh)
                .Concat(root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r => r.sharedMesh)).Distinct();
            foreach (Mesh mesh in meshes) if (mesh != null && !mesh.isReadable) {
                entry.unreadableOutlineMeshes++;
                result.issues.Add(entry.path + ": unreadable outline mesh " + mesh.name + " in " + AssetDatabase.GetAssetPath(mesh));
            }
        }
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) {
            entry.objects++;
            int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            entry.missingScripts += missing;
            if (missing > 0) result.issues.Add(entry.path + ": missing script on " + t.name);
            foreach (Component c in t.GetComponents<Component>()) {
                if (c == null) continue;
                using (var serialized = new SerializedObject(c)) {
                    var p = serialized.GetIterator();
                    while (p.Next(true)) if (p.propertyType == SerializedPropertyType.ObjectReference &&
                        p.objectReferenceValue == null && p.objectReferenceInstanceIDValue != 0) {
                        entry.brokenReferences++;
                        result.issues.Add(entry.path + ": " + t.name + "/" + c.GetType().Name + "." + p.propertyPath);
                    }
                }
            }
        }
    }
    public static void Run() {
        string output = Environment.GetEnvironmentVariable("SLAP_REVIEW_OUTPUT");
        if (String.IsNullOrEmpty(output)) throw new InvalidOperationException("SLAP_REVIEW_OUTPUT required");
        Directory.CreateDirectory(output);
        result = new Result { unityVersion = Application.unityVersion };
        try {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();
            foreach (var s in scenes) {
                var scene = EditorSceneManager.OpenScene(s.path, OpenSceneMode.Single);
                var e = new Entry { path = s.path };
                foreach (GameObject root in scene.GetRootGameObjects()) Inspect(root, e);
                result.scenes.Add(e);
            }
            var prefabPaths = new HashSet<string>(AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefab" }).Select(AssetDatabase.GUIDToAssetPath));
            foreach (var s in scenes) foreach (string dependency in AssetDatabase.GetDependencies(s.path, true))
                if (dependency.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) prefabPaths.Add(dependency);
            foreach (string path in prefabPaths.OrderBy(p => p)) {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var e = new Entry { path = path }; Inspect(prefab, e); result.prefabs.Add(e);
            }
            // Validation players must never read or overwrite the user's real PlayerPrefs namespace.
            PlayerSettings.companyName = "SlapSquadValidation";
            PlayerSettings.productName = "SlapSquadReview";
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = scenes.Select(s => s.path).ToArray(),
                locationPathName = Path.Combine(output, "player/SlapSquadReview.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            result.buildResult = report.summary.result.ToString();
            result.buildErrors = (int)report.summary.totalErrors;
            result.buildWarnings = (int)report.summary.totalWarnings;
            result.buildBytes = report.summary.totalSize;
            File.WriteAllText(Path.Combine(output, "review.json"), JsonUtility.ToJson(result, true));
            Debug.Log("REVIEW: " + result.scenes.Count + " scenes, " + result.prefabs.Count +
                " prefabs, issues=" + result.issues.Count + ", build=" + result.buildResult);
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded && result.issues.Count == 0 ? 0 : 1);
        } catch (Exception e) {
            result.issues.Add(e.GetType().Name + ": " + e.Message);
            File.WriteAllText(Path.Combine(output, "review.json"), JsonUtility.ToJson(result, true));
            Debug.LogException(e); EditorApplication.Exit(2);
        }
    }
}
