using System;
using System.Collections;
using System.IO;
using System.Linq;
using Bellator.Character;
using Bellator.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Bellator.Editor
{
    /// <summary>Explicit, idempotent prefab upgrade and an opt-in batch regression runner.</summary>
    [InitializeOnLoad]
    public static class SoulslikeStageTools
    {
        const string Batch = "Bellator.Soulslike.Batch";
        static readonly string[] suites = { "Soulslike", "Corrections", "Match", "Infinite", "Accounts" };
        static double deadline;
        static SoulslikeStageTools()
        {
            EditorApplication.update += Check;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Batch, false)) return;
                // Batch editors have no native input devices. Exercise the real Input System with virtual devices.
                if (Keyboard.current == null) InputSystem.AddDevice<Keyboard>("BellatorValidationKeyboard");
                if (Mouse.current == null) InputSystem.AddDevice<Mouse>("BellatorValidationMouse");
                InputSystem.EnableDevice(Keyboard.current); InputSystem.EnableDevice(Mouse.current);
                if (Application.isBatchMode) Time.captureFramerate = 0;
                var view = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
                view.position = new Rect(20,20,1280,840); view.Focus();
            };
        }
        [MenuItem("Bellator/Soulslike/Upgrade Existing Prefabs")]
        public static void UpgradeAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Leave Play Mode before upgrading prefabs.");
            var paths = new[] { "Assets/Game/Prefabs/Characters/PrimitivePlayer.prefab", "Assets/Game/Prefabs/Training/TrainingDummy.prefab" }
                .Concat(Directory.GetFiles("Assets/Game/Prefabs/Enemies", "*.prefab"));
            int added = 0;
            foreach (var path in paths)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int before = added;
                    foreach (var receiver in root.GetComponentsInChildren<DamageReceiver>(true))
                        if (receiver.GetComponent<ActorPosture>() == null) { receiver.gameObject.AddComponent<ActorPosture>(); added++; }
                    foreach (var motor in root.GetComponentsInChildren<ThirdPersonMotor>(true))
                    {
                        if (motor.GetComponent<PlayerLockOn>() == null) { motor.gameObject.AddComponent<PlayerLockOn>(); added++; }
                        if (motor.GetComponent<PlayerActionStateMachine>() == null) { motor.gameObject.AddComponent<PlayerActionStateMachine>(); added++; }
                        if (motor.GetComponent<PlayerAnimationBridge>() == null) { motor.gameObject.AddComponent<PlayerAnimationBridge>(); added++; }
                    }
                    if (added != before) PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/BellatorSoulslike.upgrade-result.txt", "PASS: existing prefabs upgraded; components added=" + added);
        }
        public static void BatchRun()
        { BeginBatch(0); }
        static void BeginBatch(int first)
        {
            UpgradeAssets();
            string prior = "";
            for (int i=0;i<first;i++)
            {
                string previous="Temp/Bellator"+suites[i]+".test-result.txt";
                if (!File.Exists(previous) || !File.ReadAllText(previous).Contains("RESULT: PASS")) throw new InvalidOperationException("Cannot resume without passing suite " + suites[i]);
                prior += suites[i] + ": PASS\n";
            }
            SessionState.SetBool(Batch, true); SessionState.SetInt(Batch + ".Index", first);
            SessionState.SetFloat(Batch + ".Deadline", (float)(EditorApplication.timeSinceStartup + 900));
            File.WriteAllText("Temp/BellatorSoulslike.batch.txt", "Unity " + Application.unityVersion + "\n" + prior);
            File.WriteAllText("Assets/Game/Documentation/SoulslikeBatchValidation.txt", "Unity " + Application.unityVersion + " — batch regression results\n" + prior);
            StartSuite(first);
        }
        static void StartSuite(int index)
        {
            string result = "Temp/Bellator" + suites[index] + ".test-result.txt";
            if (File.Exists(result)) File.Delete(result);
            File.WriteAllText("Temp/Bellator" + suites[index] + ".test", "run");
            deadline = EditorApplication.timeSinceStartup + 350;
            SessionState.SetFloat(Batch + ".SuiteDeadline", (float)deadline);
        }
        static void Check()
        {
            if (Application.isBatchMode && EditorApplication.isPlaying && SessionState.GetBool(Batch, false))
            {
                var game = UnityEngine.Object.FindAnyObjectByType<Bellator.Match.GameManager>();
                if (game != null && !game.IsTerminal && game.State != Bellator.Match.MatchState.Paused && game.Player.GetComponent<ThirdPersonInput>().enabled)
                {
                    // Emulate the focused Game View that a physical editor session provides.
                    game.Player.GetComponent<ThirdPersonInput>().SendMessage("OnApplicationFocus", true);
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
            {
                if (File.Exists("Temp/BellatorSoulslike.refresh")) { File.Delete("Temp/BellatorSoulslike.refresh"); AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate); return; }
                if (File.Exists("Temp/BellatorSoulslike.run"))
                {
                    int.TryParse(File.ReadAllText("Temp/BellatorSoulslike.run"), out int first);
                    File.Delete("Temp/BellatorSoulslike.run"); BeginBatch(Mathf.Clamp(first,0,suites.Length-1)); return;
                }
            }
            if (!SessionState.GetBool(Batch, false) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            int index = SessionState.GetInt(Batch + ".Index", 0);
            string result = "Temp/Bellator" + suites[index] + ".test-result.txt";
            if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Batch + ".SuiteDeadline", float.MaxValue))
            { File.AppendAllText("Temp/BellatorSoulslike.batch.txt", suites[index] + ": TIMEOUT\n"); End(1); return; }
            if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(result)) return;
            string contents = File.ReadAllText(result);
            bool passed = contents.Contains("RESULT: PASS");
            File.AppendAllText("Temp/BellatorSoulslike.batch.txt", suites[index] + ": " + (passed ? "PASS" : "FAIL") + "\n");
            File.AppendAllText("Assets/Game/Documentation/SoulslikeBatchValidation.txt", suites[index] + ": " + (passed ? "PASS" : "FAIL") + "\n");
            if (!passed) { End(1); return; }
            index++;
            if (index >= suites.Length) { End(0); return; }
            SessionState.SetInt(Batch + ".Index", index); StartSuite(index);
        }
        static void End(int code)
        {
            SessionState.SetBool(Batch, false);
            if (Application.isBatchMode && (code == 0 || !Environment.GetCommandLineArgs().Contains("-bellatorKeepTestsOpen"))) EditorApplication.Exit(code);
        }
        public static IEnumerator PressKey(Key key)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) throw new InvalidOperationException("Validation keyboard unavailable");
            KeyboardState state = new KeyboardState(key);
            Action inject = () =>
            {
                if (InputState.currentUpdateType == InputUpdateType.Dynamic) InputSystem.QueueStateEvent(keyboard, state);
            };
            InputSystem.onBeforeUpdate += inject;
            try
            {
                yield return null; yield return null;
                state = new KeyboardState();
                yield return null; yield return null;
            }
            finally { InputSystem.onBeforeUpdate -= inject; }
        }
        [MenuItem("Bellator/Soulslike/Check Scene and Prefab Integrity")]
        public static void CheckIntegrity()
        {
            var previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (var scenePath in new[] { Bellator.Match.GameInterface.MainScene, Bellator.Classes.PlayerClassController.SelectionScene, Bellator.Classes.PlayerClassController.ArenaScene })
                {
                    var scene = EditorSceneManager.OpenScene(scenePath);
                    foreach (var root in scene.GetRootGameObjects()) foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                        if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0) throw new InvalidOperationException("Missing script: " + scenePath + "/" + transform.name);
                    foreach (var root in scene.GetRootGameObjects()) foreach (var motor in root.GetComponentsInChildren<ThirdPersonMotor>(true))
                        if (motor.GetComponents<ThirdPersonMotor>().Length != 1 || motor.GetComponent<Rigidbody>() != null || motor.GetComponent<PlayerLockOn>() == null || motor.GetComponent<PlayerActionStateMachine>() == null)
                            throw new InvalidOperationException("Invalid player composition: " + scenePath);
                }
            }
            finally
            {
                if (previous.Any(scene => scene.isLoaded && scene.isActive)) EditorSceneManager.RestoreSceneManagerSetup(previous);
                else EditorSceneManager.OpenScene(Bellator.Match.GameInterface.MainScene);
            }
        }
    }
}
