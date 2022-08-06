using Game;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Windows;

public class WeaponWizardWindow : EditorWindow
{
    public WeaponData newWeapon;
    protected SerializedObject serializedObject;
    protected SerializedProperty serializedProperty;
    private WeaponData[] _weapons;
    private bool _textureNotEmpty;
    private bool _objectsSaved;
    private int _currentTab;
    private bool _meshNotEmpty;
    private string _objectName;
    private string _currentHelpString;
    private Editor _gameObjectEditor;
    private string _assetPath;
    private Texture _addedTexture;
    private Mesh _addedMesh;
    private Material _currentMaterial;
    private readonly string _prefabsDirectory = "Assets/Prefabs/";
    private readonly string _prefabSuffix = ".prefab";
    private readonly string _materialSuffix = ".mat";
    private readonly string _assetSuffix = ".asset";
    private readonly string _enterNameHelpString = "Please enter weapon name";
    private readonly string _nameNotAvailableHelpString = "This name is already in use, please enter another one";

    [UnityEditor.MenuItem("Tools/Weapon Wizard")]
    public static WeaponWizardWindow ShowWindow()
    {
        WeaponWizardWindow window = GetWindow<WeaponWizardWindow>();
        window.titleContent = new GUIContent("Weapon Wizard");
        window.minSize = new Vector2(400, 400);
        return window;
    }

    private void Awake()
    {
        _currentHelpString = _enterNameHelpString;
    }

    private void OnGUI()
    {
        if (newWeapon == null)
        {
            DrawFirstStepCreateWeaponGroup();
        }
        else
        {
            _currentTab = GUILayout.Toolbar(_currentTab, new string[] { "Data", "Geometry", "Skin" });
            switch (_currentTab)
            {
                case 0:
                    {
                        DrawDataPropertyGroup();
                        break;
                    }
                case 1:
                    {
                        DrawGeometryTabGroup();
                        break;
                    }
                case 2:
                    {
                        DrawSkinTabGroup();
                        break;
                    }
            }

            DrawViewWindow(_currentTab);
            DrawSaveButtonGroup(_currentTab);
        }
    }

    #region Draw interfase and window wethods

    private void DrawFirstStepCreateWeaponGroup()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.HelpBox(_currentHelpString, MessageType.Info);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        _objectName = GUILayout.TextField(_objectName);
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("Create weapon") && NameAvailable(_objectName))
        {
            CreateNewWeaponData();
            CreateMaterial();
        }
    }

    private void DrawDataPropertyGroup()
    {
        EditorGUILayout.HelpBox("Please enter weapon data", MessageType.Info);
        serializedProperty = serializedObject.GetIterator();
        serializedProperty.NextVisible(true);
        DrawProperties(serializedProperty);
    }

    private void DrawGeometryTabGroup()
    {
        EditorGUILayout.HelpBox("Please select weapon mesh", MessageType.Info);
        EditorGUI.BeginChangeCheck();
        _addedMesh = (Mesh)EditorGUILayout.ObjectField(_addedMesh, typeof(Mesh),false);
        if (EditorGUI.EndChangeCheck())
        {
            newWeapon.geometry.GetComponent<MeshFilter>().mesh = _addedMesh;
            _meshNotEmpty = _addedMesh != null;
            SaveAndUpdateAssets();
        }
    }

    private void DrawSkinTabGroup()
    {
        EditorGUILayout.HelpBox("Please select weapon texture", MessageType.Info);
        EditorGUI.BeginChangeCheck();
        EditorGUI.BeginDisabledGroup(!_meshNotEmpty);
        _addedTexture = (Texture)EditorGUILayout.ObjectField(_addedTexture, typeof(Texture), false);
        EditorGUI.EndDisabledGroup();
        if (EditorGUI.EndChangeCheck())
        {
            var currentRender = newWeapon.geometry.GetComponent<Renderer>();
            currentRender.sharedMaterial = _currentMaterial;
            currentRender.sharedMaterial.SetTexture("_MainTex", _addedTexture);
            ;
            _textureNotEmpty = _addedTexture != null;
            SaveAndUpdateAssets();
        }
    }

    private void DrawSaveButtonGroup(int tabNumber)
    {
        EditorGUI.BeginDisabledGroup(!_meshNotEmpty || !_textureNotEmpty);
        if (tabNumber == 2 && GUILayout.Button("Save"))
        {
            //CreateAssetAtPath<WeaponData>(newWeapon, newWeapon.name + _assetSuffix, _assetPath);
            serializedObject.ApplyModifiedProperties();
            SaveAndUpdateAssets();
            Close();
        }

        EditorGUI.EndDisabledGroup();
    }

    private void DrawViewWindow(int tabNumber)
    {
        GUILayout.Space(30);
        if (newWeapon.geometry == null || (tabNumber == 0)) return;

        if (_gameObjectEditor == null)
        {
            _gameObjectEditor = Editor.CreateEditor(newWeapon.geometry);
        }

        _gameObjectEditor.OnInteractivePreviewGUI(GUILayoutUtility.GetRect(256, 300), GUIStyle.none);
    }

    protected void DrawProperties(SerializedProperty p)
    {
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(serializedProperty);
        EditorGUI.EndDisabledGroup();
        while (p.NextVisible(false))
        {
            EditorGUILayout.PropertyField(p, true);
        }
    }

    #endregion

    #region Create and save methods

    private void CreateDirectoryFoldersByPathIfNotExists(string directoryPath)
    {
        var foldersToCreate = directoryPath.Split('/');
        var allCurrentDirectory = foldersToCreate.FirstOrDefault();
        for (int i = 0; i < foldersToCreate.Length - 1; i++)
        {
            var currentPath = allCurrentDirectory + "/" + foldersToCreate[i + 1];
            if (!Directory.Exists(currentPath))
            {
                AssetDatabase.CreateFolder(allCurrentDirectory, foldersToCreate[i + 1]);
            }

            allCurrentDirectory = currentPath;
        }
    }

    private GameObject CreatePrefabAtDirectory(GameObject gameObject, string directoryPath)
    {
        CreateDirectoryFoldersByPathIfNotExists(directoryPath);
        string localPath = directoryPath + "/" + gameObject.name +  _prefabSuffix;
        localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);

        var savedPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, localPath, InteractionMode.UserAction,
            out var prefabSuccess);
        if (prefabSuccess)
        {
            Debug.Log("Prefab was saved successfully");
            DestroyImmediate(gameObject);
        }
        else
        {
            Debug.Log("Prefab failed to save");
        }

        return savedPrefab;
    }

    private void CreateMaterial()
    {
        Material material = new Material(Shader.Find("Specular"));
        var materialName = newWeapon.geometry.name + _materialSuffix;
        _currentMaterial = CreateAssetAtPath<Material>(material, materialName, _assetPath);
    }

    private void CreateNewWeaponData()
    {
        newWeapon = new WeaponData();
        newWeapon.name = _objectName;
        var geometry = new GameObject(_objectName);
        geometry.AddComponent<MeshFilter>();
        geometry.AddComponent<MeshRenderer>();
        _assetPath = _prefabsDirectory + geometry.name;
        newWeapon.geometry = CreatePrefabAtDirectory(geometry, _assetPath);
        serializedObject = new SerializedObject(newWeapon);
        CreateAssetAtPath<WeaponData>(newWeapon, newWeapon.name + _assetSuffix, _assetPath);
    }

    private T CreateAssetAtPath<T>(T asset, string assetName, string assetFolderPath) where T : UnityEngine.Object
    {
        CreateDirectoryFoldersByPathIfNotExists(assetFolderPath);
        var assetPath = assetFolderPath + "/" + assetName;
        AssetDatabase.CreateAsset(asset, assetPath);
        Debug.Log(AssetDatabase.GetAssetPath(asset));

        return AssetDatabase.LoadAssetAtPath<T>(assetPath);
    }

    private void SaveAndUpdateAssets()
    {
        serializedObject.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    #endregion

    #region Other methods

    private bool NameAvailable(string newWeaponName)
    {
        _weapons = GetAllInstances<WeaponData>();
        bool nameIsAvailable = !String.IsNullOrEmpty(_objectName) && _weapons.All(weapon => weapon.name != newWeaponName);
        _currentHelpString = nameIsAvailable ? _enterNameHelpString : _nameNotAvailableHelpString;
        return nameIsAvailable;
    }

    public static T[] GetAllInstances<T>() where T : WeaponData
    {
        string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
        T[] assets = new T[guids.Length];
        for (int i = 0; i < assets.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            assets[i] = AssetDatabase.LoadAssetAtPath<T>(path);
        }

        return assets;
    }

    #endregion
}
