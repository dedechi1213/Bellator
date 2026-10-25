using System;
using System.IO;
using System.Linq;
using Bellator.Character;
using Bellator.Classes;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Bellator.Editor
{
    public static class RollUpdateTools
    {
        public const string Root = "Assets/Game/Animations/SoulslikeGameplay/RollUpdate";
        [MenuItem("Bellator/Roll for all classes/Connect assets")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first");
            Directory.CreateDirectory(Root+"/Survivor"); Directory.CreateDirectory(Root+"/Evidence"); AssetDatabase.Refresh();
            var roll = AnimationStageBuilder.BakeRoll(0,Root+"/Survivor/DodgeRoll.anim");
            foreach (string name in new[]{"Survivor","Hunter"})
            {
                var definition=AssetDatabase.LoadAssetAtPath<GladiatorClassDefinition>("Assets/Game/ScriptableObjects/Classes/"+name+".asset");
                // Migrate once; subsequent builds preserve Inspector tuning.
                if (!definition.hasQuickstep)
                {
                    definition.quickstep=definition.dodge;
                    definition.hasQuickstep=true;
                    definition.dodge=new DodgeConfiguration { style=DodgeStyle.Roll,
                        duration=name=="Survivor"?.5f:.54f, recovery=name=="Survivor"?.14f:.16f,
                        cost=name=="Survivor"?20:23, distance=name=="Survivor"?3.9f:3.6f,
                        invulnerableStart=.18f, invulnerableEnd=.62f };
                }
                var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerIntegrationTools.Root+"/Controllers/"+name+"Gameplay.controller");
                if (!controller.parameters.Any(p=>p.name=="Quickstep")) controller.AddParameter("Quickstep",AnimatorControllerParameterType.Trigger);
                var states=controller.layers[0].stateMachine.states.Select(s=>s.state).ToArray();
                var rolling=states.Single(s=>s.name=="DodgeRoll");
                if(name=="Survivor") rolling.motion=roll;
                foreach(var state in states.Where(s=>s.motion is BlendTree))
                {
                    foreach(var transition in state.transitions)
                        if(transition.conditions.Any(c=>c.parameter=="Dodge")) transition.destinationState=rolling;
                    if(!state.transitions.Any(t=>t.conditions.Any(c=>c.parameter=="Quickstep")))
                    {
                        var transition=state.AddTransition(states.Single(s=>s.name=="Quickstep"));
                        transition.hasExitTime=false; transition.duration=.05f;
                        transition.AddCondition(AnimatorConditionMode.If,0,"Quickstep");
                    }
                }
                EditorUtility.SetDirty(definition); EditorUtility.SetDirty(controller);
            }
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        }
        public static void Batch()
        {
            try { Build(); PlayerIntegrationValidation.Begin(); }
            catch(Exception e) { Debug.LogException(e); Directory.CreateDirectory(Root+"/Evidence"); File.WriteAllText(Root+"/Evidence/BuildFailure.txt",e.ToString()); EditorApplication.Exit(1); }
        }
    }
}
