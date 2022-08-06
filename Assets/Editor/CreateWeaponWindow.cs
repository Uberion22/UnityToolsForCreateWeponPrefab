using Game;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public enum TabType
{
    Data = 0,
    Geometry = 1,
    Skin = 2
}

public class WeaponWizardWindow : EditorWindow
{
    private readonly string PrefabsDirectory = "Assets/Prefabs/";
    private readonly string MaterialSuffix = ".mat";
    private readonly string AssetSuffix = ".asset";
    private readonly string EnterNameHelpString = "Please enter weapon name";
    private readonly string NameNotAvailableHelpString = "This name is already in use, please enter another one";
    private readonly string GeometryPropertyFieldName = "Geometry";
    private WeaponData _newWeapon;
    private SerializedObject _serializedObject;
    private SerializedProperty _serializedProperty;
    private WeaponData[] _weapons;
    private bool _textureIsEmpty = true;
    private bool _meshIsEmpty = true;
    private TabType _currentTab;
    private string _objectName;
    private string _currentHelpString;
    private Editor _gameObjectEditor;
    private string _assetPath;
    private Texture _addedTexture;
    private Mesh _addedMesh;
    private Material _currentMaterial;

    [MenuItem("Tools/Weapon Wizard")]
    public static WeaponWizardWindow ShowWindow()
    {
        WeaponWizardWindow window = GetWindow<WeaponWizardWindow>();
        window.titleContent = new GUIContent("Weapon Wizard");
        window.minSize = new Vector2(400, 400);
        
        return window;
    }

    private void Awake()
    {
        _currentHelpString = EnterNameHelpString;
    }

    private void OnGUI()
    {
        if (_newWeapon == null)
        {
            DrawFirstStepCreateWeaponGroup();
        }
        else
        {
            DrawTabsGroup();
        }
    }

    #region Draw interfase and window wethods

    private void DrawFirstStepCreateWeaponGroup()
    {
        EditorGUILayout.HelpBox(_currentHelpString, MessageType.Info);
        _objectName = GUILayout.TextField(_objectName);
       
        if (GUILayout.Button("Create weapon") && NameAvailable(_objectName))
        {
            CreateNewWeaponData();
            CreateMaterial();
        }
    }

    private void DrawTabsGroup()
    {
        _currentTab = (TabType)GUILayout.Toolbar((int)_currentTab, new string[] { "Data", "Geometry", "Skin" });
        switch (_currentTab)
        {
            case TabType.Data:
            {
                DrawDataPropertyGroup();
                break;
            }
            case TabType.Geometry:
            {
                DrawGeometryTabGroup();
                break;
            }
            case TabType.Skin:
            {
                DrawSkinTabGroup();
                break;
            }
        }

        DrawViewWindow();
        DrawSaveButtonGroup();
    }

    private void DrawDataPropertyGroup()
    {
        EditorGUILayout.HelpBox("Please enter weapon data", MessageType.Info);
        _serializedProperty = _serializedObject.GetIterator();
        _serializedProperty.NextVisible(true);
        DrawProperties(_serializedProperty);
    }

    private void DrawGeometryTabGroup()
    {
        EditorGUILayout.HelpBox("Please select weapon mesh", MessageType.Info);
        
        EditorGUI.BeginChangeCheck();
        _addedMesh = (Mesh)EditorGUILayout.ObjectField(_addedMesh, typeof(Mesh),false);
        
        if (EditorGUI.EndChangeCheck())
        {
            _newWeapon.geometry.GetComponent<MeshFilter>().mesh = _addedMesh;
            _meshIsEmpty = _addedMesh == null;
            SaveAndUpdate();
            _gameObjectEditor?.ReloadPreviewInstances();
        }
    }

    private void DrawSkinTabGroup()
    {
        EditorGUILayout.HelpBox("Please select weapon texture", MessageType.Info);
        
        EditorGUI.BeginChangeCheck();
        
        EditorGUI.BeginDisabledGroup(_meshIsEmpty);
        _addedTexture = (Texture)EditorGUILayout.ObjectField(_addedTexture, typeof(Texture), false);
        EditorGUI.EndDisabledGroup();
        
        if (EditorGUI.EndChangeCheck())
        {
            var currentRender = _newWeapon.geometry.GetComponent<Renderer>();
            currentRender.sharedMaterial = _currentMaterial;
            currentRender.sharedMaterial.SetTexture("_MainTex", _addedTexture);
            _textureIsEmpty = _addedTexture == null;
            _gameObjectEditor?.ReloadPreviewInstances();
            SaveAndUpdate();
        }
    }

    private void DrawSaveButtonGroup()
    {
        EditorGUI.BeginDisabledGroup(_meshIsEmpty || _textureIsEmpty);
        
        if (_currentTab == TabType.Skin && GUILayout.Button("Save"))
        {
            _serializedObject.ApplyModifiedProperties();
            SaveAndUpdate();
            Close();
        }

        EditorGUI.EndDisabledGroup();
    }

    private void DrawViewWindow()
    {
        GUILayout.Space(30);

        if (_newWeapon.geometry == null || _currentTab == TabType.Data) return;

        if (_gameObjectEditor == null)
        {
            _gameObjectEditor = Editor.CreateEditor(_newWeapon.geometry);
        }
        _gameObjectEditor.OnInteractivePreviewGUI(GUILayoutUtility.GetRect(256, 300),GUIStyle.none);
    }

    protected void DrawProperties(SerializedProperty property)
    {
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(_serializedProperty);
        EditorGUI.EndDisabledGroup();
        while (property.NextVisible(false))
        {
            var disabled = property.displayName == GeometryPropertyFieldName;
            
            EditorGUI.BeginDisabledGroup(disabled);
            EditorGUILayout.PropertyField(property, true);
            EditorGUI.EndDisabledGroup();
        }
    }

    #endregion

    #region Create and save methods

    private void CreateMaterial()
    {
        Material material = new Material(Shader.Find("Specular"));
        var materialName = _newWeapon.geometry.name + MaterialSuffix;
        _currentMaterial = AssetsUtility.CreateAssetAtPath<Material>(material, materialName, _assetPath);
    }

    private void CreateNewWeaponData()
    {
        _newWeapon = ScriptableObject.CreateInstance<WeaponData>();
        _newWeapon.name = _objectName;
        var geometry = new GameObject(_objectName);
        geometry.AddComponent<MeshFilter>();
        geometry.AddComponent<MeshRenderer>();
        _assetPath = PrefabsDirectory + geometry.name;
        _newWeapon.geometry = AssetsUtility.CreatePrefabAtDirectory(geometry, _assetPath);
        _serializedObject = new SerializedObject(_newWeapon);
        AssetsUtility.CreateAssetAtPath<WeaponData>(_newWeapon, _newWeapon.name + AssetSuffix, _assetPath);
    }

    private void SaveAndUpdate()
    {
        _serializedObject.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    #endregion

    #region Other methods

    private bool NameAvailable(string newWeaponName)
    {
        _weapons = GetAllInstances<WeaponData>();
        var nameIsAvailable = !String.IsNullOrEmpty(_objectName) && _weapons.All(weapon => weapon.name != newWeaponName);
        _currentHelpString = nameIsAvailable ? EnterNameHelpString : NameNotAvailableHelpString;
        
        return nameIsAvailable;
    }

    public static T[] GetAllInstances<T>() where T : WeaponData
    {
        var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
        T[] assets = new T[guids.Length];
        for (var i = 0; i < assets.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            assets[i] = AssetDatabase.LoadAssetAtPath<T>(path);
        }

        return assets;
    }

    #endregion
}