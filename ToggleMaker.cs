using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.UIElements;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars;
using UnityEngine.Animations;
using VRC.SDK3.Avatars.ScriptableObjects;
using System.Linq;
using System.IO;
using UnityEditor.Animations;
using static VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control;
using System;
using System.Text;
using UnityEditor.SceneManagement;
using System.Reflection;

public class ToggleMaker : EditorWindow
{
    VRCAvatarDescriptor yourAv;
    VRCExpressionsMenu menu;
    VRCExpressionParameters parameters;
    AnimatorController fxLayer;
    string parameterName;
    string menuName;
    GameObject objectToToggle;
    GameObject[] objectsToToggle = new GameObject[0];
    private string baseFolderPath = Directory.GetCurrentDirectory() + @"\Assets\ToggleMaker\";
    private AnimationClip toggleOffClip = null;
    private AnimationClip toggleOnClip = null;
    bool multiToggleSupport = false;
    bool onByDefault = false;
    bool localToggle = false;
    bool writeDefaults = false;
    bool multipleItemsPerToggle = false;
    bool materialToggle = false;
    int numberOfToggles = 2;
    Vector2 scrollPos;
    MaterialRenderer[] materialRenderers = null;

    // Creates Unity Editor Window
    [MenuItem("ToggleMaker")]
    public static void MakeWindow()
    {
        GetWindow(typeof(ToggleMaker));

    }

    // On Gui Update
    private void OnGUI()
    { 
        GUILayout.Label("ToggleMaker");
        // Make scrolling work
        scrollPos = GUILayout.BeginScrollView(scrollPos, false, true, GUILayout.ExpandHeight(true));
        yourAv = EditorGUILayout.ObjectField("Avatar", yourAv, typeof(VRCAvatarDescriptor), true) as VRCAvatarDescriptor;
        menu = EditorGUILayout.ObjectField("Menu To Add Toggle To", menu, typeof(VRCExpressionsMenu), true) as VRCExpressionsMenu;
        parameters = EditorGUILayout.ObjectField("Parameters", parameters, typeof(VRCExpressionParameters), true) as VRCExpressionParameters;
        fxLayer = EditorGUILayout.ObjectField("FX Layer", fxLayer, typeof(AnimatorController), true) as AnimatorController;
        // Make gui adjustments if only one toggle
        if (!multiToggleSupport){
            GUILayout.Label("Menu Item Name");
            menuName = EditorGUILayout.TextField("", menuName);
            GUILayout.Label("Parameter Name");
            parameterName = EditorGUILayout.TextField("", parameterName);
        }
        if (GUILayout.Button("Try To Auto Find Items From Avatar"))
        {
            if (yourAv != null)
            {

                if (fxLayer == null)
                {
                    fxLayer = (AnimatorController)System.Array.Find(yourAv.baseAnimationLayers, (animLayer) => animLayer.type == VRCAvatarDescriptor.AnimLayerType.FX).animatorController;
                }
                if (menu == null)
                {
                    menu = yourAv.expressionsMenu;
                }
                if (parameters == null)
                {
                    parameters = yourAv.expressionParameters;
                }


            }

        }
        onByDefault = EditorGUILayout.Toggle("On By Default", onByDefault);
        localToggle = EditorGUILayout.Toggle("Local Toggle", localToggle);
        writeDefaults = EditorGUILayout.Toggle("Write Defaults", writeDefaults);
        materialToggle = EditorGUILayout.Toggle("Material Toggle", materialToggle);

        // Menu options for material swap toggles
        if (!materialToggle)
        {
            multiToggleSupport = EditorGUILayout.Toggle("Multi Toggle Support", multiToggleSupport);
            multipleItemsPerToggle = EditorGUILayout.Toggle("Multiple Items Per Toggle", multipleItemsPerToggle);
            if (multipleItemsPerToggle)
            {
                numberOfToggles = EditorGUILayout.IntField("Number of items to toggle", numberOfToggles);
                // Dissallow greater than 100 toggles
                if (numberOfToggles > 100)
                {
                    numberOfToggles = 100;

                }
                // Ensure at least one toggle
                else if (numberOfToggles < 1)
                {
                    numberOfToggles = 1;
                }

                if (objectsToToggle.Length != numberOfToggles)
                {
                    objectsToToggle = new GameObject[numberOfToggles];
                }

                for (int i = 0; i < objectsToToggle.Length; ++i)
                {
                    objectsToToggle[i] = EditorGUILayout.ObjectField("Object", objectsToToggle[i], typeof(GameObject), true) as GameObject;
                }
            }
            else if (multiToggleSupport)
            {

                numberOfToggles = EditorGUILayout.IntField("Number of toggles", numberOfToggles);
                if (numberOfToggles > 100)
                {
                    numberOfToggles = 100;

                }
                else if (numberOfToggles < 1)
                {
                    numberOfToggles = 1;
                }

                if (objectsToToggle.Length != numberOfToggles)
                {
                    objectsToToggle = new GameObject[numberOfToggles];
                }

                for (int i = 0; i < objectsToToggle.Length; ++i)
                {
                    objectsToToggle[i] = EditorGUILayout.ObjectField("Object", objectsToToggle[i], typeof(GameObject), true) as GameObject;

                }
            }
            else
            {
                objectToToggle = EditorGUILayout.ObjectField("Object To Toggle", objectToToggle, typeof(GameObject), true) as GameObject;

            }
        }
        else
        {
            if (GUILayout.Button("Scan For Renderers"))
            {
                Renderer[] renderers = yourAv.gameObject.GetComponentsInChildren<Renderer>(true);
                materialRenderers = new MaterialRenderer[renderers.Length];
                for (int i = 0; i < renderers.Length; ++i)
                {
                    materialRenderers[i] = new MaterialRenderer();
                    materialRenderers[i].renderer = renderers[i];
                    foreach (Material mat in renderers[i].sharedMaterials)
                    {
                        materialRenderers[i].materials.Add(new KeyValuePair<Material, Material>(mat, null));
                    }
                }
            }
            if (materialRenderers != null)
            {
                // Render found renderers on avatar
                for (int i = 0; i < materialRenderers.Length; ++i)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField("", materialRenderers[i].renderer, typeof(Renderer), true, GUILayout.Width(this.position.size.x * .9f));
                    materialRenderers[i].activated = EditorGUILayout.Toggle(materialRenderers[i].activated);
                    GUILayout.EndHorizontal();
                    if (materialRenderers[i].activated)
                    {
                        GUILayout.BeginHorizontal();
                        // 2.1f seems to be a good scale
                        EditorGUILayout.LabelField("Old Materials", GUILayout.Width(this.position.size.x / 2.1f));
                        EditorGUILayout.LabelField("New Materials", GUILayout.Width(this.position.size.x / 2.1f));
                        GUILayout.EndHorizontal();
                        for (int j = 0; j < materialRenderers[i].materials.Count; ++j)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.ObjectField("", materialRenderers[i].materials[j].Key, typeof(Material), true, GUILayout.MinWidth(0));
                            materialRenderers[i].materials[j] = new KeyValuePair<Material, Material>(materialRenderers[i].materials[j].Key, (Material)EditorGUILayout.ObjectField("", materialRenderers[i].materials[j].Value, typeof(Material), true, GUILayout.MinWidth(0)));
                            GUILayout.EndHorizontal();
                        }

                        EditorGUILayout.LabelField(new string('-', (int)(this.position.size.x * .17f)));
                    }
                }
            }
        }
        // Perform toggle creation
        if (GUILayout.Button("Execute"))
        {
            Undo.RecordObjects(new UnityEngine.Object[] { menu, parameters }, "Menu Stuff");
            if (localToggle)
            {
                AddLocalParamIfNeeded(fxLayer);
            }
            if (materialToggle)
            {
                MakeFolderForAvatarIfNeeded();
                AddParameter(parameterName);
                AddMenuItem(menuName, parameterName);
                MakeMaterialToggleAnimation(materialRenderers, menuName);
                MakeNewFXLayerLayer(parameterName, parameterName);
                MakeNewFXLayerParameter(parameterName);
            }
            else
            {
 
                if (multipleItemsPerToggle)
                {

                    MakeFolderForAvatarIfNeeded();
                    AddParameter(parameterName);
                    AddMenuItem(menuName, parameterName);
                    MakeMultpleTogglesPerAnimation(objectsToToggle, menuName);

                    MakeNewFXLayerLayer(menuName, parameterName);
                    MakeNewFXLayerParameter(parameterName);
                }
                else if (multiToggleSupport)
                {
                    for (int i = 0; i < objectsToToggle.Length; ++i)
                    {
                        MakeFolderForAvatarIfNeeded();
                        AddParameter(objectsToToggle[i].name + i);
                        AddMenuItem(objectsToToggle[i].name + i, objectsToToggle[i].name + i);
                        MakeToggleAnimation(objectsToToggle[i], "");
                        MakeNewFXLayerLayer(objectsToToggle[i].name + i, objectsToToggle[i].name + i);
                        MakeNewFXLayerParameter(objectsToToggle[i].name + i);
                    }
                }
                else
                {
                    MakeFolderForAvatarIfNeeded();
                    AddParameter(parameterName);
                    AddMenuItem(menuName, parameterName);
                    MakeToggleAnimation(objectToToggle, menuName);
                    MakeNewFXLayerLayer(parameterName, parameterName);
                    MakeNewFXLayerParameter(parameterName);
                }
            }
            // Makes it so the actual assets get updated and not just unity's in-memory version of them
            // Note: This took me forever to figure out. Unity really needs better editor script docs...
            EditorUtility.SetDirty(menu);
            EditorUtility.SetDirty(parameters);
            EditorSceneManager.SaveOpenScenes();

        }

        GUILayout.EndScrollView();
    }

    private void AddLocalParamIfNeeded(AnimatorController fx)
    {
        bool hasLocal = false;
        for (int i = 0; i < fx.parameters.Length; ++i)
        {
            if (fx.parameters[i].name.Equals("IsLocal"))
            {
                hasLocal = true;
                break;
            }
        }

        if (!hasLocal)
        {
            fxLayer.AddParameter(new AnimatorControllerParameter() { name = "IsLocal", defaultBool = false, type = AnimatorControllerParameterType.Bool });
        }
    }

    // Ensure string is valid unity string
    private string SanityCheckString(string stringToCheck)
    {
        char[] arr = stringToCheck.Where(c => (char.IsLetterOrDigit(c) ||
                             char.IsWhiteSpace(c) ||
                             c == '-')).ToArray();

        return new string(arr);
    }

    // Adds a parameter by using serialized object properties.
    // Not the best but it's what was needed at the time of creation
    private void AddParameter(string paramString)
    {
        SerializedObject paramsOb = new SerializedObject(parameters);
        SerializedProperty paramsProps = paramsOb.FindProperty("parameters");
        paramsProps.InsertArrayElementAtIndex(paramsProps.arraySize);
        SerializedProperty param = paramsProps.GetArrayElementAtIndex(paramsProps.arraySize - 1);

        param.FindPropertyRelative("valueType").intValue = 2;
        param.FindPropertyRelative("defaultValue").floatValue = 0f;
        param.FindPropertyRelative("name").stringValue = paramString;
        param.FindPropertyRelative("saved").boolValue = true;
        paramsOb.ApplyModifiedProperties();
    }

    // Add menu item to VRC Menu
    private void AddMenuItem(string itemName, string paramString)
    {
        VRCExpressionsMenu.Control el = new VRCExpressionsMenu.Control() { name = itemName, parameter = new VRCExpressionsMenu.Control.Parameter() { name = paramString }, type = ControlType.Toggle, value = 1f, icon = null, subMenu = null, subParameters = null, style = Style.Style1 };
        menu.controls.Add(el);
    }

    // Make animation clips for the object toggles
    // This can be wayyyy better but this is simple enough for the project
    private void MakeToggleAnimation(GameObject objectToggle, string menuName)
    {
        menuName = SanityCheckString(menuName);

        toggleOffClip = new AnimationClip();
        toggleOffClip.SetCurve(GetPathToObjectFromAv(objectToggle), typeof(GameObject), "m_IsActive", AnimationCurve.Constant(0f, 0f, 0f));
        AssetDatabase.CreateAsset(toggleOffClip, GetFolderForAvatar(true) + @"\Toggle" + objectToggle.name + "MenuName" + menuName + "Off.anim");


        toggleOnClip = new AnimationClip();
        toggleOnClip.SetCurve(GetPathToObjectFromAv(objectToggle), typeof(GameObject), "m_IsActive", AnimationCurve.Constant(0f, 0f, 1f));
        AssetDatabase.CreateAsset(toggleOnClip, GetFolderForAvatar(true) + @"\Toggle" + objectToggle.name + "MenuName" + menuName + "On.anim");

    }

    // Make animation clip for material swaps
    private void MakeMaterialToggleAnimation(MaterialRenderer[] matRenderer, string menuName)
    {
        menuName = SanityCheckString(menuName);
        toggleOffClip = new AnimationClip();
        string offAssetName = GetFolderForAvatar(true) + @"\Toggle" + matRenderer[0].renderer.name + "MenuName" + menuName + "Off.anim";


        string onAssetName = GetFolderForAvatar(true) + @"\Toggle" + matRenderer[0].renderer.name + "MenuName" + menuName + "On.anim";
        toggleOnClip = new AnimationClip();

        for (int i = 0; i < matRenderer.Length; ++i)
        {
            for (int j = 0; j < matRenderer[i].materials.Count; ++j)
            {

                if (matRenderer[i].materials[j].Value != null)
                {
                    // This looks horrible, but unity documentation is actually the worst
                    // and this is the most function thing I can come up with without
                    // just writing plain text to a animation clip file.
                    // I literally got the "m_Materials" property by opening an AnimationClip in notepad....
                    AnimationUtility.SetObjectReferenceCurve(toggleOffClip, EditorCurveBinding.PPtrCurve(GetPathToObjectFromAv(matRenderer[i].renderer.gameObject),
                        matRenderer[i].renderer.GetType(), $"m_Materials.Array.data[{j}]"),
                        new ObjectReferenceKeyframe[] { new ObjectReferenceKeyframe() { time = 0f, value = matRenderer[i].materials[j].Key } });

                    AnimationUtility.SetObjectReferenceCurve(toggleOnClip, EditorCurveBinding.PPtrCurve(GetPathToObjectFromAv(matRenderer[i].renderer.gameObject),
                        matRenderer[i].renderer.GetType(), $"m_Materials.Array.data[{j}]"),
                        new ObjectReferenceKeyframe[] { new ObjectReferenceKeyframe() { time = 0f, value = matRenderer[i].materials[j].Value } });
                }
            }

        }
        // Makes the asset actually appear in the files and not just in memory
        AssetDatabase.CreateAsset(toggleOffClip, offAssetName);
        AssetDatabase.CreateAsset(toggleOnClip, onAssetName);

    }

    // Makes the animation clips for when there are multiple toggles
    private void MakeMultpleTogglesPerAnimation(GameObject[] objectToggle, string menuName)
    {
        menuName = SanityCheckString(menuName);

        toggleOffClip = new AnimationClip();
        string fullname = "";
        for (int i = 0; i < objectToggle.Length; ++i)
        {
            toggleOffClip.SetCurve(GetPathToObjectFromAv(objectToggle[i]), typeof(GameObject), "m_IsActive", AnimationCurve.Constant(0f, 0f, 0f));
            fullname += objectToggle[i].name;
        }
        AssetDatabase.CreateAsset(toggleOffClip, GetFolderForAvatar(true) + @"\Toggle" + fullname + "MenuName" + menuName + "Off.anim");


        toggleOnClip = new AnimationClip();
        for (int i = 0; i < objectToggle.Length; ++i)
        {
            toggleOnClip.SetCurve(GetPathToObjectFromAv(objectToggle[i]), typeof(GameObject), "m_IsActive", AnimationCurve.Constant(0f, 0f, 1f));
        }
        AssetDatabase.CreateAsset(toggleOnClip, GetFolderForAvatar(true) + @"\Toggle" + fullname + "MenuName" + menuName + "On.anim");

    }

    // Small utility function for getting folder assigned to a specific avatar
    // Helps a lot with naming conflicts
    private string GetFolderForAvatar(bool relative = false)
    {
        if (relative)
        {
            return @"Assets\ToggleMaker\" + yourAv.name;
        }
        else
        {
            return baseFolderPath + @"\" + yourAv.name;

        }
    }

    // Creates avatar specific folder if needed
    private void MakeFolderForAvatarIfNeeded()
    {
        if (!Directory.Exists(GetFolderForAvatar()))
        {
            Directory.CreateDirectory(GetFolderForAvatar());
        }
        AssetDatabase.Refresh();
    }

    // Gets gameobject path relative to avatar base
    // If I made this recursive it would have looked much cooler
    private string GetPathToObjectFromAv(GameObject gameOb)
    {
        string path = "";
        Transform trans = gameOb.transform;
        List<string> names = new List<string>();
        while (trans.parent != null && !GameObject.ReferenceEquals(yourAv, trans.gameObject))
        {
            names.Add(trans.gameObject.name);
            trans = trans.parent;

        }
        // Need to reverse the list since we build it backwards
        names.Reverse();
        for (int i = 0; i < names.Count; ++i)
        {
            if (i == names.Count - 1)
            {
                path += names[i];

            }
            else
            {
                path += names[i] + @"/";

            }

        }
        return path;

    }

    // Make FX layer on AnimationController and adds the toggle states
    private void MakeNewFXLayerLayer(string nameOfLayer, string param)
    {

        AnimatorControllerLayer animLayer = new AnimatorControllerLayer() { defaultWeight = 1f, name = nameOfLayer, blendingMode = AnimatorLayerBlendingMode.Override };


        animLayer.stateMachine = new AnimatorStateMachine();

        animLayer.stateMachine.name = animLayer.name;

        animLayer.stateMachine.hideFlags = HideFlags.HideInHierarchy;


        fxLayer.AddLayer(animLayer);


        AssetDatabase.AddObjectToAsset(animLayer.stateMachine, AssetDatabase.GetAssetPath(fxLayer));

        if (onByDefault)
        {
            AnimatorState onMach = animLayer.stateMachine.AddState("ToggleOn", new Vector3(200f, 200f, 0f));
            AnimatorState offMach = animLayer.stateMachine.AddState("ToggleOff", new Vector3(200f, 0f, 0f));

            AnimatorStateTransition trans2 = onMach.AddTransition(offMach);
            trans2.hasExitTime = false;
            trans2.duration = 0f;
            trans2.AddCondition(AnimatorConditionMode.If, 0f, param);

            if (localToggle)
            {
                trans2.AddCondition(AnimatorConditionMode.If, 0f, "IsLocal");
            }

            AnimatorStateTransition trans1 = offMach.AddTransition(onMach);
            trans1.hasExitTime = false;
            trans1.duration = 0f;
            trans1.AddCondition(AnimatorConditionMode.IfNot, 0f, param);


            if (writeDefaults)
            {
                offMach.writeDefaultValues = true;
                onMach.writeDefaultValues = true;
            }
            else
            {
                offMach.writeDefaultValues = false;
                onMach.writeDefaultValues = false;
            }

            offMach.motion = toggleOffClip;
            onMach.motion = toggleOnClip;


        }
        else
        {

            AnimatorState offMach = animLayer.stateMachine.AddState("ToggleOff", new Vector3(200f, 0f, 0f));
            AnimatorState onMach = animLayer.stateMachine.AddState("ToggleOn", new Vector3(200f, 200f, 0f));

            AnimatorStateTransition trans1 = offMach.AddTransition(onMach);
            trans1.hasExitTime = false;
            trans1.duration = 0f;
            trans1.AddCondition(AnimatorConditionMode.If, 0f, param);
            if (localToggle)
            {
                trans1.AddCondition(AnimatorConditionMode.If, 0f, "IsLocal");
            }
            AnimatorStateTransition trans2 = onMach.AddTransition(offMach);
            trans2.hasExitTime = false;
            trans2.duration = 0f;
            trans2.AddCondition(AnimatorConditionMode.IfNot, 0f, param);
            if (writeDefaults)
            {
                offMach.writeDefaultValues = true;
                onMach.writeDefaultValues = true;
            }
            else
            {
                offMach.writeDefaultValues = false;
                onMach.writeDefaultValues = false;
            }

            offMach.motion = toggleOffClip;
            onMach.motion = toggleOnClip;

        }

    }

    // Adds parameter to AnimatorController(FX)
    private void MakeNewFXLayerParameter(string name)
    {
        fxLayer.AddParameter(new AnimatorControllerParameter() { name = name, defaultBool = false, type = AnimatorControllerParameterType.Bool });
    }
}

class MaterialRenderer
{
    public Renderer renderer = null;
    public bool activated = false;
    public List<KeyValuePair<Material, Material>> materials = new List<KeyValuePair<Material, Material>>();
}
