using System.Web.DynamicData;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

public class WeaponEditorWindow : ScriptableWizard
{
    protected SerializedObject serializedObject;
    protected SerializedProperty serializedProperty;
    protected ScriptableObject[] weapos;
    
    [MenuItem("Window/Weapon Wizard")]
    public static WeaponEditorWindow ShowWindow()
    {
        WeaponEditorWindow window = GetWindow<WeaponEditorWindow>();
        window.titleContent = new GUIContent("Weapon Wizard");
        window.minSize = new Vector2(300, 300);
        return window;
    }

    private void OnGUI()
    {
        weapos = GetAllInstances<ScriptableObject>();
        for (int i = 0; i < weapos.Length; i++)
        {
            serializedObject = new SerializedObject(weapos[i]);
            serializedProperty = serializedObject.GetIterator();
            serializedProperty.NextVisible(true);
            DrawProperties(serializedProperty);
        }
    }

    protected void DrawProperties(SerializedProperty p)
    {
        while (p.NextVisible(false))
        {
            EditorGUILayout.PropertyField(p,true);
        }
    }

    public static T[] GetAllInstances<T>() where T : ScriptableObject
    {
        string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
        T[] a = new T[guids.Length];
        for (int i = 0; i < a.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            a[i] = AssetDatabase.LoadAssetAtPath<T>(path);
        }

        return a;
    }
    
    // Start is called before the first frame update
    //void OnEnable()
    //{
    //    helpString = "Please set the color of the light!";
    //    TextField weaponName = new TextField();
    //    rootVisualElement.Add(weaponName);
    //}

    //// Update is called once per frame
    //void Update()
    //{
        
    //}
}
