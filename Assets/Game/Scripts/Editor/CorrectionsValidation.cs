using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bellator.Character;
using Bellator.Classes;
using Bellator.Combat;
using Bellator.Match;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellator.Editor
{
    [InitializeOnLoad]
    public static class CorrectionsValidation
    {
        const string Active="Bellator.Corrections.Test";
        static IEnumerator routine;static int frame=-1;static double deadline;
        static readonly StringBuilder report=new StringBuilder();static readonly List<string> errors=new List<string>();
        static float volume,sensitivity;static int quality,screenMode;static InputSettings.EditorInputBehaviorInPlayMode priorInput;
        static CorrectionsValidation() { EditorApplication.update+=Check;EditorApplication.playModeStateChanged+=State; }
        static void Check()
        {
            if(routine!=null && EditorApplication.isPlaying && frame!=Time.frameCount)
            { frame=Time.frameCount;try { if(EditorApplication.timeSinceStartup>deadline) throw new TimeoutException("Corrections validation timeout");if(!routine.MoveNext()) Finish(null); } catch(Exception e) { Finish(e); } }
            if(EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            string flag=File.Exists("Temp/BellatorCorrections.test")?"Temp/BellatorCorrections.test":File.Exists("Temp/BellatorInterface.test")?"Temp/BellatorInterface.test":null;
            if(flag==null) return;File.Delete(flag);Run();
        }
        [MenuItem("Bellator/Test Fixed Equipment and Selection Corrections")]
        public static void Run()
        { if(EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;EditorSceneManager.OpenScene(GameInterface.MainScene);SessionState.SetBool(Active,true);EditorApplication.EnterPlaymode(); }
        static void State(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Active+".Reopen",false))
            { SessionState.SetBool(Active+".Reopen",false);EditorSceneManager.OpenScene(GameInterface.MainScene); }
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Active,false)) return;
            report.Clear();errors.Clear();report.AppendLine("Bellator corrections — Unity "+Application.unityVersion+" — real Play Mode / scene transitions / injected Input System keys / native uGUI raycasts");
            volume=GameSettings.Volume;sensitivity=GameSettings.Sensitivity;quality=GameSettings.Quality;screenMode=GameSettings.ScreenMode;
            priorInput=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            Application.logMessageReceived+=Log;deadline=EditorApplication.timeSinceStartup+180;routine=Flatten(Checks());frame=-1;
        }
        static void Log(string message,string trace,LogType type)
        { if((type==LogType.Exception || type==LogType.Assert || type==LogType.Error) && (trace.Contains("Bellator.") || message.Contains("NullReference") || message.Contains("MissingReference") || message.Contains("NavMeshAgent"))) errors.Add(message); }
        static void Finish(Exception error)
        {
            Application.logMessageReceived-=Log;if(error==null && errors.Count>0) error=new InvalidOperationException(string.Join("\n",errors));
            routine=null;SessionState.SetBool(Active,false);Time.timeScale=1;InputSystem.settings.editorInputBehaviorInPlayMode=priorInput;
            GameSettings.SetVolume(volume);GameSettings.SetSensitivity(sensitivity);GameSettings.SetQuality(quality);GameSettings.SetScreenMode(screenMode);GameSettings.Save();
            report.AppendLine(error==null?"RESULT: PASS":"RESULT: FAIL\n"+error);
            File.WriteAllText("Assets/Game/Documentation/CorrectionsValidation.txt",report.ToString());File.WriteAllText("Temp/BellatorCorrections.test-result.txt",report.ToString());
            if(error!=null) Debug.LogException(error);else Debug.Log("Bellator fixed-equipment corrections validation passed.");
            SessionState.SetBool(Active+".Reopen",true);EditorApplication.ExitPlaymode();
        }
        static IEnumerator Flatten(IEnumerator root)
        {
            var stack=new Stack<IEnumerator>();stack.Push(root);
            while(stack.Count>0) { var current=stack.Peek();if(!current.MoveNext()) { stack.Pop();continue; } if(current.Current is IEnumerator nested) { stack.Push(nested);continue; }yield return null; }
        }
        static void Assert(bool value,string message) { if(!value) throw new InvalidOperationException(message); }
        static void Pass(string message) { report.AppendLine("PASS: "+message);File.WriteAllText("Temp/BellatorCorrections.progress.txt",report.ToString()); }
        static IEnumerator Click(string name)
        {
            Canvas.ForceUpdateCanvases();
            var go=GameObject.Find(name);Assert(go!=null,"Missing button "+name);var button=go.GetComponent<Button>();Assert(button.IsInteractable(),"Disabled "+name);
            var rect=go.GetComponent<RectTransform>();var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));var hits=new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            Assert(hits.Count>0 && hits[0].gameObject.GetComponentInParent<Button>()==button,"Occluded "+name);button.onClick.Invoke(); yield return null; yield return null;
        }
        static IEnumerator Scene(string name)
        { while(SceneManager.GetActiveScene().name!=name) yield return null;yield return null;yield return null; }
        static IEnumerator KeyPress(Key key)
        {
            Assert(Keyboard.current!=null,"Keyboard unavailable");InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));yield return null;yield return null;
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;yield return null;
        }
        static IEnumerator Capture(string name)
        { yield return null;ScreenCapture.CaptureScreenshot("Assets/Game/Documentation/Corrections_"+name+".png");yield return null;yield return null; }
        static IEnumerator Checks()
        {
            yield return null;yield return null;ClassSelectionState.Confirm(null);
            yield return Click("GuestButton");
            yield return Click("PlayButton");yield return null;yield return null;yield return Click("StoryModeButton");yield return Scene("RomanArena");var game=UnityEngine.Object.FindAnyObjectByType<GameManager>();
            Assert(game.Player.Current.id==GladiatorClassId.Warrior && game.State==MatchState.Preparing && game.Waves.AliveCount==0,"Play without selection did not initialize default Warrior/countdown");
            yield return KeyPress(Key.Escape);yield return Click("MenuButton");yield return Scene("MainMenu");
            Pass("Jogar without prior selection loads a fresh arena with Guerreiro, default attributes/equipment and first-wave countdown.");
            var types=new[]{EquipmentType.ShortSword,EquipmentType.HeavySword,EquipmentType.ShortSword,EquipmentType.Crossbow};
            for(int index=0;index<4;index++)
            {
                yield return Click("SelectCharacterButton");yield return Scene("GladiatorSelection");var selection=UnityEngine.Object.FindAnyObjectByType<GladiatorSelectionController>();
                Assert(GameObject.Find("StartButton")==null,"Direct start remains on selection");
                yield return Click("ClassCard_"+index);yield return null;var chosen=selection.Catalog.classes[index];Assert(selection.SelectedIndex==index,"Class card not selected");
                if(index==3) yield return Capture("Selection");
                yield return Click("ConfirmButton");yield return Scene("MainMenu");
                Assert(ClassSelectionState.Selected==chosen && UnityEngine.Object.FindAnyObjectByType<GameManager>()==null && UnityEngine.Object.FindAnyObjectByType<PlayerClassController>()==null && UnityEngine.Object.FindAnyObjectByType<WaveManager>()==null,"Confirmation started combat or lost selection");
                yield return Click("SelectCharacterButton");yield return Scene("GladiatorSelection");selection=UnityEngine.Object.FindAnyObjectByType<GladiatorSelectionController>();
                Assert(selection.SelectedIndex==index,"Reopened selection lost the previous class");yield return Click("SelectionMenuButton");yield return Scene("MainMenu");
                yield return Click("PlayButton");yield return null;yield return null;yield return Click("StoryModeButton");yield return Scene("RomanArena");game=UnityEngine.Object.FindAnyObjectByType<GameManager>();var actor=game.Player;
                Assert(actor.Current==chosen && actor.GetComponent<CharacterHealth>().Maximum==chosen.health,"Class/attributes lost on Play");game.Waves.ManualSimulation=true;
                var equipment=actor.GetComponent<WeaponEquipment>();var combat=actor.GetComponent<PlayerCombat>();var stamina=actor.GetComponent<CharacterStamina>();var ability=actor.GetComponent<PlayerAbility>();
                Assert(equipment.IsFixed && equipment.Loadout.Length==1 && equipment.Current==chosen.equipment[0] && equipment.Current.type==types[index] && equipment.HasShield==(index==1 || index==2),"Wrong fixed equipment: "+chosen.displayName);
                Assert(equipment.Muzzle!=null || !equipment.Current.IsRanged,"Crossbow visual/muzzle missing");
                int changes=0;equipment.Changed+=()=>changes++;var original=equipment.Current;var originalShield=equipment.ShieldVisual;
                foreach(var key in new[]{Key.Digit1,Key.Digit2,Key.Digit3,Key.Digit4,Key.Digit5}) yield return KeyPress(key);
                Assert(changes==0 && equipment.Current==original && equipment.ShieldVisual==originalShield,"Numeric keys changed equipment/shield");
                Assert(!equipment.Equip(1) && equipment.Equip(0),"Fixed slots not enforced");equipment.ToggleShield();Assert(changes==0 && equipment.ShieldVisual==originalShield,"Fixed shield can be toggled");
                Assert(!InputSystem.ListEnabledActions().Any(a=>a.actionMap?.name=="BellatorCombat" && (a.name.StartsWith("Slot") || a.name=="ToggleShield")),"Weapon actions still registered");
                Assert(GameObject.Find("AbilityPanel")==null && GameObject.Find("AbilityCooldown")==null && GameObject.Find("Ability")==null,"Ability UI still exists");
                var hints=GameObject.Find("Controls").GetComponent<Text>().text;
                Assert(!hints.Contains("1–4") && !hints.Contains("5 escudo") && hints.Contains("Q habilidade"),"Outdated controls");
                Assert(hints.Contains("defender")==equipment.HasShield && hints.Contains("recarregar")==equipment.Current.IsRanged && hints.Contains("ataque pesado")==!equipment.Current.IsRanged,"Class-inapplicable hints");
                Assert((GameObject.Find("AmmunitionPanel")!=null)==equipment.Current.IsRanged,"Melee HUD has irrelevant ammunition panel");
                stamina.Restore();float initial=stamina.Current;yield return KeyPress(Key.Q);
                Assert(ability.CooldownRemaining>0 && stamina.Current<initial && ability.IsActive,"Q did not activate ability: "+chosen.displayName);
                Assert(actor.transform.Find("AbilityFeedback_Primitives")!=null,"Special effects removed");float cooldown=ability.CooldownRemaining;float spent=stamina.Current;
                yield return KeyPress(Key.Q);Assert(ability.CooldownRemaining<cooldown && stamina.Current<=spent+.01f,"Repeated Q bypassed cooldown");
                if(index==0) Assert(actor.GetComponent<ThirdPersonMotor>().SpeedBuff>1,"Survivor effect lost");
                if(index==2) Assert(actor.GetComponent<ActorAttributes>().AbilityDamage>1,"Warrior effect lost");
                if(index==1) { float until=Time.time+2;while(ability.IsCasting && Time.time<until) yield return null;Assert(!ability.IsCasting,"Colossus ability stuck"); }
                if(index==3)
                {
                    float until=Time.time+2;while(!combat.IsReloading && Time.time<until) yield return null;
                    Assert(combat.IsReloading && !combat.CrossbowLoaded,"Precise shot/reload no longer work");
                    yield return null;yield return null;Assert(GameObject.Find("Ammo").GetComponent<Text>().text.Contains("Recarga:"),"Crossbow reload HUD missing");
                }
                yield return Capture(chosen.id.ToString());
                ability.TickAbility(50);combat.ResetTrainingState();stamina.Restore();
                yield return KeyPress(Key.E);Assert(combat.IsAttacking==!equipment.Current.IsRanged,"Heavy attack behavior changed");
                combat.ResetTrainingState();stamina.Restore();var guard=actor.GetComponent<ShieldGuard>();guard.Request(true);Assert(guard.IsBlocking==equipment.HasShield,"Class shield restrictions broken");guard.Request(false);
                Assert(combat.TryAttack(false),"Basic attack/shot failed");float end=Time.time+3;while(combat.IsAttacking && Time.time<end) yield return null;
                var hp=actor.GetComponent<CharacterHealth>();hp.Damage(7);yield return null;yield return null;
                Assert(Mathf.Abs(GameObject.Find("HealthBar/Fill").GetComponent<Image>().fillAmount-hp.Current/hp.Maximum)<.001f,"Health bar stale");
                Assert(Mathf.Abs(GameObject.Find("StaminaBar/Fill").GetComponent<Image>().fillAmount-stamina.Current/stamina.Maximum)<.001f,"Stamina bar stale");
                game.Waves.ManualSimulation=false;yield return KeyPress(Key.Escape);Assert(Time.timeScale==0 && game.State==MatchState.Paused,"Pause broken");
                yield return Click("PauseSettingsButton");yield return null;Assert(UnityEngine.Object.FindAnyObjectByType<GameInterface>().SettingsVisible,"Pause settings broken");
                float clock=game.Waves.Countdown;yield return KeyPress(Key.Escape);Assert(Time.timeScale==0 && game.Waves.Countdown==clock && game.State==MatchState.Paused,"Closing settings resumed combat");
                yield return Click("ResumeButton");yield return null;Assert(Time.timeScale==1,"Resume broken");
                if(index==3)
                {
                    hp.Damage(10000);yield return null;yield return null;Assert(game.State==MatchState.Defeat,"Defeat broken");yield return Click("RestartButton");while(game!=null) yield return null;yield return Scene("RomanArena");game=UnityEngine.Object.FindAnyObjectByType<GameManager>();
                    Assert(game.Player.Current==chosen && game.Player.GetComponent<WeaponEquipment>().Current==chosen.equipment[0] && game.State==MatchState.Preparing,"Restart lost equipment/class");
                }
                game.TogglePause();yield return null;yield return null;yield return Click("MenuButton");yield return Scene("MainMenu");
                Pass(chosen.displayName+": selection confirms to menu without arena actors; previous selection restored; Jogar applies class and fixed equipment; real keys 1–5 do nothing; Q activates ability/consumes stamina/creates effects/respects cooldown; class-only hints; no ability UI; combat, bars, pause/settings/resume and menu work.");
            }
            Pass("Hunter has only crossbow; shot, automatic reload and defeat/restart preserve fixed equipment. Full wave/AI/victory integrity is covered by the adapted Stage 6 regression suite.");
        }
    }
}
