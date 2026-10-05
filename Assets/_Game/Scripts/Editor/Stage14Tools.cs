using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Tycoon.Editor
{
    public static class Stage14Tools
    {
        const string Root="Assets/_Game/Art/Imported/Models/";
        [MenuItem("TYCOON/Polish Work Animations")]
        public static void PrepareAnimations()
        {
            const string directory="Assets/_Game/Art/Animations";
            if(!AssetDatabase.IsValidFolder(directory))AssetDatabase.CreateFolder("Assets/_Game/Art","Animations");
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Root+"player.controller");
            if(!controller)throw new InvalidOperationException("Thiếu player controller.");
            var sourceClips=AssetDatabase.LoadAllAssetsAtPath(Root+"player.fbx").OfType<AnimationClip>().ToArray();
            var source=sourceClips.First(x=>x.name.Split('|').Last()=="Pickup");
            var machine=controller.layers[0].stateMachine;
            foreach(string action in new[]{"Operate","Farming","AnimalCare","Cashier","Cooking","Serving","Cleaning"})
            {
                var clip=UnityEngine.Object.Instantiate(source);clip.name=action;
                float duration=action is "Farming" or "Cleaning"?1.1f:action=="AnimalCare"?1.4f:.85f;
                foreach(var binding in AnimationUtility.GetCurveBindings(source))
                {
                    var original=AnimationUtility.GetEditorCurve(source,binding);
                    bool left=binding.path.Contains(".L");
                    var keys=new Keyframe[25];
                    for(int i=0;i<keys.Length;i++)
                    {
                        float u=i/24f;
                        float phase=(action is "Cooking" or "Operate" or "Cashier")&&left?(u+.5f)%1:u;
                        keys[i]=new Keyframe(u*duration,original.Evaluate(phase*source.length));
                    }
                    AnimationUtility.SetEditorCurve(clip,binding,new AnimationCurve(keys));
                }
                // Chỉ quay xương, không sửa vị trí/scale nên không kéo dài mesh hoặc tranh NavMesh.
                foreach(var group in AnimationUtility.GetCurveBindings(source).Where(x=>x.propertyName.StartsWith("m_LocalRotation.")).GroupBy(x=>x.path))
                {
                    var bindings=group.ToArray();if(bindings.Length!=4)continue;
                    bool torso=group.Key.EndsWith("Spine")||group.Key.EndsWith("Chest");
                    bool swipe=(action is "Cooking" or "Cleaning")&&group.Key.EndsWith("LowerArm.R");
                    if(!torso&&!swipe)continue;
                    var curves=bindings.ToDictionary(x=>x.propertyName.Last(),x=>AnimationUtility.GetEditorCurve(source,x));
                    var values=new AnimationCurve[4]{new(),new(),new(),new()};
                    for(int i=0;i<=24;i++)
                    {
                        float u=i/24f,t=u*source.length,wave=Mathf.Sin(u*Mathf.PI*2);
                        var q=new Quaternion(curves['x'].Evaluate(t),curves['y'].Evaluate(t),curves['z'].Evaluate(t),curves['w'].Evaluate(t));
                        float bend=torso?(action=="Cleaning"?12:action=="Farming"?9:action=="AnimalCare"?4:2)*(1-Mathf.Cos(u*Mathf.PI*2))*.5f:0;
                        q=(q*Quaternion.Euler(bend,0,swipe?wave*12:0)).normalized;
                        values[0].AddKey(u*duration,q.x);values[1].AddKey(u*duration,q.y);values[2].AddKey(u*duration,q.z);values[3].AddKey(u*duration,q.w);
                    }
                    for(int i=0;i<4;i++)AnimationUtility.SetEditorCurve(clip,bindings.Single(x=>x.propertyName.Last()=="xyzw"[i]),values[i]);
                }
                clip.EnsureQuaternionContinuity();
                var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;settings.loopBlend=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
                string path=directory+"/Player_"+action+".anim";
                var saved=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if(saved){EditorUtility.CopySerialized(clip,saved);UnityEngine.Object.DestroyImmediate(clip);}else{AssetDatabase.CreateAsset(clip,path);saved=clip;}
                var state=machine.states.Select(x=>x.state).FirstOrDefault(x=>x.name==action)??machine.AddState(action);
                state.motion=saved;EditorUtility.SetDirty(saved);EditorUtility.SetDirty(state);
            }
            EditorUtility.SetDirty(controller);EditorUtility.SetDirty(machine);AssetDatabase.SaveAssets();
            string output=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"work","stage14");Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output,"animation-report.json"),JsonUtility.ToJson(new AnimationAudit{states=machine.states.Select(x=>x.state.name).ToArray()},true));
            Debug.Log("STAGE14_ANIMATION_PASS • work states generated for player and employees.");
        }
        [Serializable] sealed class AnimationAudit { public string[] states; }
    }
}
