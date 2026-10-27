using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BeforeItWasYou.EditorTools
{
    /// <summary>
    /// Víctor (MakeHuman, esqueleto game_engine) y las animaciones CC0 de Quaternius se importan como Humanoid para
    /// reutilizar los clips entre esqueletos. Crea los materiales URP del personaje y su Animator Controller.
    /// </summary>
    public static class CharacterImport
    {
        public const string VictorFolder = ArtImport.ArtFolder + "/Characters/Victor";
        public const string VictorModel = VictorFolder + "/SK_Victor.fbx";
        public const string AnimationsModel = ArtImport.ArtFolder + "/Characters/Animations/UAL1_Standard.fbx";
        public const string ControllerPath = VictorFolder + "/AC_Victor.controller";

        public static void Run()
        {
            if (!System.IO.File.Exists(VictorModel)) return;
            CreateMaterials();
            ConfigureHumanoid(VictorModel, false);
            ConfigureHumanoid(AnimationsModel, true);
            BuildController();
        }

        private static void CreateMaterials()
        {
            var folder = ArtImport.MaterialsFolder + "/Characters";
            EditorUtil.EnsureFolder(folder);
            foreach (var (name, smooth, cutout) in new[] { ("M_Victor_Skin", 0.38f, false), ("M_Victor_Clothes", 0.18f, false), ("M_Victor_Shoes", 0.25f, false),
                         ("M_Victor_Hair", 0.35f, true), ("M_Victor_Brows", 0.2f, true), ("M_Victor_Lashes", 0.2f, true), ("M_Victor_Eyes", 0.9f, false) })
            {
                var tex = ArtImport.Texture($"{VictorFolder}/{name}_BaseColor.png");
                var normal = ArtImport.Texture($"{VictorFolder}/{name}_Normal.png");
                var mat = ArtImport.LitMaterial($"{folder}/{name}.mat", tex, normal, null, Color.white, Vector2.one, smooth, 0f);
                if (cutout) ArtImport.MakeCutout(mat, 0.45f, true);
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { VictorFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                var isNormal = path.Contains("_Normal");
                if (importer.textureType == (isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default) && importer.maxTextureSize == 2048) continue;
                importer.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.maxTextureSize = 2048;
                importer.alphaIsTransparency = !isNormal;
                importer.SaveAndReimport();
            }
        }

        private static void ConfigureHumanoid(string path, bool animations)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) return;
            var changed = importer.animationType != ModelImporterAnimationType.Human;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = animations;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Everywhere);
            if (animations)
            {
                var clips = importer.defaultClipAnimations;
                foreach (var clip in clips)
                {
                    clip.loopTime = clip.name.Contains("Loop");
                    clip.lockRootRotation = true;
                    clip.lockRootHeightY = true;
                    clip.lockRootPositionXZ = true;
                    clip.keepOriginalOrientation = true;
                    clip.keepOriginalPositionY = true;
                    clip.keepOriginalPositionXZ = true;
                }
                importer.clipAnimations = clips;
                changed = true;
            }
            if (changed) importer.SaveAndReimport();
        }

        private static AnimationClip Clip(string contains)
        {
            return AssetDatabase.LoadAllAssetsAtPath(AnimationsModel).OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__") && c.name.Contains(contains));
        }

        private static void BuildController()
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            foreach (var t in new[] { "Interact", "PickUp", "Push", "Attack", "Idle" }) controller.AddParameter(t, AnimatorControllerParameterType.Trigger);
            var machine = controller.layers[0].stateMachine;
            var tree = new BlendTree { name = "Locomotion", blendParameter = "Speed", blendType = BlendTreeType.Simple1D, useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(tree, controller);
            tree.AddChild(Clip("Idle_Loop"), 0f);
            tree.AddChild(Clip("Walk_Loop"), 1.35f);
            tree.AddChild(Clip("Jog_Fwd_Loop"), 3.2f);
            tree.AddChild(Clip("Sprint_Loop"), 5f);
            var locomotion = machine.AddState("Locomotion");
            locomotion.motion = tree;
            machine.defaultState = locomotion;
            foreach (var (trigger, clip) in new[] { ("Interact", "Interact"), ("PickUp", "PickUp_Table"), ("Push", "Push_Loop"), ("Attack", "Punch_Cross") })
            {
                var state = machine.AddState(trigger);
                state.motion = Clip(clip);
                var enter = machine.AddAnyStateTransition(state);
                enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);
                enter.duration = 0.15f;
                var back = state.AddTransition(locomotion);
                back.hasExitTime = true;
                back.exitTime = trigger == "Push" ? 0.6f : 0.9f;
                back.duration = 0.2f;
            }
            var idle = machine.AddAnyStateTransition(locomotion);
            idle.AddCondition(AnimatorConditionMode.If, 0f, "Idle");
            idle.duration = 0.25f;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }
    }
}
