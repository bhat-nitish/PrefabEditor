using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editor;
using Editor.Config;
using UnityEditor;
using UnityEditor.Experimental.U2D;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
using UnityEditor.IMGUI.Controls;
using UnityEditor.VersionControl;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;
using Object = UnityEngine.Object;


public class PrefabConfigEditor : EditorWindow
{
    private MultiColumnHeader columnHeader;

    private MultiColumnHeaderState.Column[] columns;

    private float xScroll = 0;

    private PrefabConfigEditorState _editorState = new PrefabConfigEditorState() { };

    private PrefabConfig _prefabConfiguration = new PrefabConfig() {config = new PrefabConfigItem[] { }};

    private Vector2 _configScrollPosition = Vector2.zero;

    private Vector2 _previewScrollPosition = Vector2.zero;

    private Vector2 _prefabSearchListScrollPosition = Vector2.zero;

    private Vector2 _prefabSelectedListScrollPosition = Vector2.zero;

    private string _configSearchString;

    private string _prefabSearchString;

    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private Color _prefabColor = new Color();

    private const string AssetRootPath = "Assets";

    private const string ResourcesPath = "Assets/Resources";

    private const string DefaultPrefabCreationPath = "Assets/Scripts/Prefabs/EditorPrefabs";

    private List<PrefabListItem> _prefabsInProject = new List<PrefabListItem>();

    private string _currentPrefabDirectory =>
        _editorState != null && _editorState.ShowAllPrefabsInDirectory
            ? _editorState.PrefabLocalDirectory
            : AssetRootPath;

    private Color _previewColor;

    [MenuItem("EditorTools/PrefabConfigEditor")]
    public static void Create()
    {
        var instance = CreateInstance<PrefabConfigEditor>();
        instance.titleContent = new GUIContent("Prefab Editor");
        instance.Show();
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            return;
        }

        _prefabsInProject = GetPrefabsInPath(AssetRootPath);

        columns = new MultiColumnHeaderState.Column[]
        {
            new MultiColumnHeaderState.Column()
            {
                headerContent = new GUIContent("Select Configuration"),
                width = 200,
                // minWidth = 100,
                // maxWidth = 500,
                autoResize = true,
                headerTextAlignment = TextAlignment.Center
            },
            new MultiColumnHeaderState.Column()
            {
                headerContent = new GUIContent("Preview Configuration"),
                width = 200,
                // minWidth = 100,
                //  maxWidth = 500,
                autoResize = true,
                headerTextAlignment = TextAlignment.Center
            },
            new MultiColumnHeaderState.Column()
            {
                headerContent = new GUIContent("Apply Configuration"),
                width = 200,
                //  minWidth = 100,
                //  maxWidth = 500,
                autoResize = true,
                headerTextAlignment = TextAlignment.Center
            }
        };
        columnHeader = new MultiColumnHeader(new MultiColumnHeaderState(columns));
        columnHeader.height = 25;
        columnHeader.ResizeToFit();
    }

    private void OnGUI()
    {
        if (Application.isPlaying)
        {
            AddAndRegisterNonEditorWarning();
            return;
        }

        // calculate the window visible rect
        GUILayout.FlexibleSpace();
        var windowVisibleRect = GUILayoutUtility.GetLastRect();
        windowVisibleRect.width = position.width;
        windowVisibleRect.height = position.height;

        // draw the column headers
        var headerRect = windowVisibleRect;
        headerRect.height = columnHeader.height;

        columnHeader.OnGUI(headerRect, xScroll);

        DrawConfigRect(windowVisibleRect);
        DrawPreviewRect(windowVisibleRect);
        DrawApplyConfigRect(windowVisibleRect);
    }

    #region Non Editor Scope

    private void AddAndRegisterNonEditorWarning()
    {
        GUILayout.Label(
            "Please stop the application to use the editor. Running the editor tool when the application is running might cause conflicts");
    }

    #endregion

    #region Select Configuration

    private void DrawConfigRect(Rect windowVisibleRect)
    {
        var contentRect = columnHeader.GetColumnRect(0);
        contentRect.x -= xScroll;
        contentRect.y = contentRect.yMax;
        contentRect.yMax = windowVisibleRect.yMax;
        AddAndRegisterConfigurationControls(contentRect);
        GUI.DrawTexture(contentRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 1f,
            new Color(1f, 0f, 0f, 0.5f), 1, 1);
    }

    private void AddAndRegisterConfigSearch()
    {
        if (_prefabConfiguration.config == null || !_prefabConfiguration.config.Any())
            return;

        GUILayout.BeginHorizontal(GUI.skin.FindStyle("Toolbar"));

        EditorGUI.BeginChangeCheck();

        _configSearchString = GUILayout.TextField(_configSearchString, GUI.skin.FindStyle("ToolbarSeachTextField"));

        if (EditorGUI.EndChangeCheck())
        {
            _editorState.ConfigSearchString = _configSearchString;
        }

        if (GUILayout.Button(string.Empty, GUI.skin.FindStyle("ToolbarSeachCancelButton")))
        {
            _configSearchString = string.Empty;
            _editorState.ConfigSearchString = _configSearchString;
            _editorState.ReSetCurrentConfigurationForPreview();
            GUI.FocusControl(null);
        }

        GUILayout.EndHorizontal();
    }

    private void AddAndRegisterSearchResults()
    {
        if (_prefabConfiguration.config == null || !_prefabConfiguration.config.Any())
            return;

        _configScrollPosition = EditorGUILayout.BeginScrollView(_configScrollPosition);

        foreach (var configItem in _prefabConfiguration.config)
        {
            if (!string.IsNullOrWhiteSpace(_editorState.ConfigSearchString) &&
                !configItem.text.ContainsIgnoreCase(_editorState.ConfigSearchString))
            {
                continue;
            }

            if (GUILayout.Button(configItem.text, GUILayout.ExpandWidth(true)))
            {
                OnPrefabConfigItemClicked(configItem);
            }
        }

        GUILayout.EndScrollView();
    }

    private void OnPrefabConfigItemClicked(PrefabConfigItem config)
    {
        _editorState.CurrentConfigurationForPreviewText = config.text;
        _editorState.CurrentConfigurationForPreview = JsonUtility.ToJson(config, true);
    }

    private void AddAndRegisterResultsView()
    {
        AddAndRegisterConfigSearch();

        AddAndRegisterSearchResults();
    }

    private void DrawApplyConfigRect(Rect windowVisibleRect)
    {
        var contentRect = columnHeader.GetColumnRect(2);
        contentRect.x -= xScroll;
        contentRect.y = contentRect.yMax;
        contentRect.yMax = windowVisibleRect.yMax;
        AddAndRegisterApplyConfigurationControls(contentRect);
        GUI.DrawTexture(contentRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 1f,
            new Color(1f, 0f, 0f, 0.5f), 1, 1);
    }

    private void AddAndRegisterConfigurationControls(Rect configurationRect)
    {
        GUILayout.BeginArea(configurationRect);

        GUILayout.Space(10);

        AddAndRegisterFileUploadButton();

        AddAndRegisterConfigLabels();

        GUILayout.Space(10);

        AddAndRegisterResultsView();

        GUILayout.EndArea();
    }

    private void AddAndRegisterConfigLabels()
    {
        AddAndRegisterFileNameInfoLabel();

        AddAndRegisterFileUploadErrorLabel();

        AddAndRegisterFileUploadWarningLabel();
    }

    private void AddAndRegisterFileUploadButton()
    {
        if (GUILayout.Button("Upload Configuration File"))
        {
            OpenFilePanel();
        }
    }

    private void AddAndRegisterFileUploadErrorLabel()
    {
        if (!_editorState.HasConfiguratonError) return;
        GUILayout.Space(5);
        GUILayout.Label(" Error in Uploading File", "red");
    }

    private void AddAndRegisterFileUploadWarningLabel()
    {
        if (!_editorState.IsInvalidConfiguration) return;
        GUILayout.Space(5);
        GUILayout.Label(" Invalid Configuration -- Please check .json file", "red");
    }

    private void AddAndRegisterFileNameInfoLabel()
    {
        if (string.IsNullOrWhiteSpace(_editorState.CurrentConfigurationFileName)) return;
        GUILayout.Space(5);
        GUILayout.Label($" Success!! - Configuration was read from {_editorState.CurrentConfigurationFileName}",
            EditorStyles.whiteLabel);
    }

    private void OpenFilePanel()
    {
        var path = EditorUtility.OpenFilePanel("Please select a configuration file", "", "json");
        var json = "";

        using (var reader = new StreamReader(path))
        {
            json = reader.ReadToEnd();
        }

        try
        {
            _editorState.ClearConfigErrorsAndWarnings();

            _prefabConfiguration = ParseConfig(json);

            _editorState.ReSetCurrentConfigurationForPreview();

            if (_prefabConfiguration == null || !_prefabConfiguration.config.Any())
            {
                _editorState.IsInvalidConfiguration = true;
            }
            else
            {
                _editorState.IsValidConfiguration = true;
                _editorState.CurrentConfigurationFileName = Path.GetFileName(path);
            }
        }

        catch (System.Exception ex)
        {
            _prefabConfiguration = new PrefabConfig() {config = new PrefabConfigItem[] { }};
            _editorState.HasConfiguratonError = true;
            Debug.Log(ex.Message);
        }
    }

    private PrefabConfig ParseConfig(string json)
    {
        var prefabConfig = JsonUtility.FromJson<PrefabConfig>(json);
        if (prefabConfig?.config == null)
        {
            return new PrefabConfig() {config = new PrefabConfigItem[] { }};
        }

        prefabConfig.config = prefabConfig.config.DistinctBy(c => c.text).ToArray();
        return prefabConfig;
    }

    #endregion

    #region Preview Configuration

    private void DrawPreviewRect(Rect windowVisibleRect)
    {
        var contentRect = columnHeader.GetColumnRect(1);
        contentRect.x -= xScroll;
        contentRect.y = contentRect.yMax;
        contentRect.yMax = windowVisibleRect.yMax;
        AddAndRegisterPreviewControls(contentRect);
        GUI.DrawTexture(contentRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 1f,
            new Color(1f, 0f, 0f, 0.5f), 1, 1);
    }

    private void AddAndRegisterConfigPreview()
    {
        if (!_editorState.HasValidConfigurationSelected)
        {
            return;
        }

        GUILayout.Space(10);

        EditorGUILayout.LabelField($"Config currently in preview : {_editorState.CurrentConfigurationForPreviewText}",
            EditorStyles.whiteLabel);

        _previewScrollPosition =
            EditorGUILayout.BeginScrollView(_previewScrollPosition, false, true, GUILayout.ExpandHeight(true));

        EditorGUILayout.TextArea(_editorState.CurrentConfigurationForPreview, GUILayout.ExpandHeight(true));

        GUILayout.EndScrollView();

        GUILayout.Space(10);
    }

    private void AddAndRegisterConfigPrefabPreview()
    {
        if (!_editorState.HasValidConfigurationSelected)
        {
            return;
        }

        var configItem = GetSelectedPrefabConfigItem(_editorState.CurrentConfigurationForPreviewText);

        var texturePath = GetConfigTexturepath(_editorState.CurrentConfigurationForPreviewText);

        if (string.IsNullOrWhiteSpace(texturePath))
        {
            return;
        }

        GUILayout.Space(10);

        try
        {
            ColorUtility.TryParseHtmlString(configItem.color, out _previewColor);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ColorField("Color", _previewColor);
            EditorGUI.EndDisabledGroup();
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }

        GUILayout.Space(10);

        EditorGUILayout.LabelField($"Texture currently in preview : {texturePath} ", EditorStyles.whiteLabel);

        _previewScrollPosition =
            EditorGUILayout.BeginScrollView(_previewScrollPosition, false, true, GUILayout.ExpandHeight(true));

        Texture2D texture = (Texture2D) EditorGUIUtility.Load(texturePath);

        EditorGUILayout.HelpBox(new GUIContent(texture));

        GUILayout.EndScrollView();

        GUILayout.Space(10);
    }

    private string GetConfigTexturepath(string configName)
    {
        var configItem = GetSelectedPrefabConfigItem(configName);

        if (configItem == null)
        {
            return string.Empty;
        }

        return Path.Combine(ResourcesPath, configItem.image);
    }

    private void AddAndRegisterPreviewControls(Rect previewRect)
    {
        GUILayout.BeginArea(previewRect);

        AddAndRegisterConfigPreview();

        AddAndRegisterConfigPrefabPreview();

        GUILayout.EndArea();
    }

    #endregion

    #region Apply Configuration

    private void AddAndRegisterApplyConfigurationControls(Rect applyConfigRect)
    {
        GUILayout.BeginArea(applyConfigRect);

        AddAndRegisterPrefabSearch();

        AddAndRegisterCreatedPrefabFromSelectedConfigButton();

        AddAndRegisterApplyConfigurationButton();

        GUILayout.Space(10);

        AddAndRegisterPrefabSearchMode();

        AddAndRegisterPrefabSearchResults();

        AddAndRegisterPrefabSelectedResults();

        GUILayout.EndArea();
    }

    private void AddAndRegisterApplyConfigurationButton()
    {
        if (_editorState.SelectedPrefabGuids == null || !_editorState.SelectedPrefabGuids.Any() ||
            !_editorState.HasValidConfigurationSelected)
        {
            return;
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Apply Configuration"))
        {
            ApplySelectedConfigurationToPrefabs();
        }

        GUILayout.Space(10);
    }

    private Dictionary<string, GameObject> GetSelectedPrefabsForModification()
    {
        var selectedPrefabs = new Dictionary<string, GameObject>();

        if (_editorState.SelectedPrefabGuids == null || !_editorState.SelectedPrefabGuids.Any())
        {
            return selectedPrefabs;
        }

        foreach (var prefabGuid in _editorState.SelectedPrefabGuids)
        {
            var prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);

            if (string.IsNullOrWhiteSpace(prefabPath))
            {
                continue;
            }

            var prefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefabObject == null)
            {
                continue;
            }

            selectedPrefabs.Add(prefabPath, prefabObject);
        }

        return selectedPrefabs;
    }

    private void ApplySelectedConfigurationToPrefabs()
    {
        try
        {
            var selectedPrefabs = GetSelectedPrefabsForModification();

            if (!selectedPrefabs.Any())
            {
                return;
            }

            var prefabConfigItem = GetSelectedPrefabConfigItem(_editorState.CurrentConfigurationForPreviewText);
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Apply Prefab Configuration");
            var undoGroupIndex = Undo.GetCurrentGroup();
            // Undo.RecordObjects((Object[]) selectedPrefabs.Select(sp => PrefabUtility.LoadPrefabContents(sp.Key)), "Prefab Configuration");
            //Undo.RecordObjects((Object[]) selectedPrefabs.Select(sp => PrefabUtility.LoadPrefabContents(sp.Key)), "Prefab Configuration");
            // PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            //Undo.RecordObjects(selectedPrefabs.Select(pre => pre.Value).ToArray(), "transform selected objects");
            foreach (var prefab in selectedPrefabs)
            {
                // Undo.RecordObject(prefab.Value, "Undo Prefab");
                ApplyConfigToPrefab(prefab, prefabConfigItem);
            }

            // AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(undoGroupIndex);
        }
        catch (Exception e)
        {
            Debug.Log("Errror " + e.Message);
        }
    }

    private void ApplyConfigToPrefab(KeyValuePair<string, GameObject> prefab, PrefabConfigItem prefabConfigItem)
    {
        if (prefabConfigItem == null)
        {
            return;
        }

        try
        {
            var go = prefab.Value;
            //var go = PrefabUtility.LoadPrefabContents(prefab.Key);
            Undo.RecordObject(go, "Prefab");
            var textComponents = go.GetComponentsInChildren<Text>();
            if (textComponents != null && textComponents.Any())
            {
                foreach (var textComponent in textComponents)
                {
                    Undo.RecordObject(textComponent, "Text");
                    textComponent.text = prefabConfigItem.text;
                }
            }

            var spriteRenderers = go.GetComponentsInChildren<SpriteRenderer>();
            if (spriteRenderers != null && spriteRenderers.Any())
            {
                foreach (var spriteRenderer in spriteRenderers)
                {
                    Undo.RecordObject(spriteRenderer.sprite, "Sprite");
                    Undo.RecordObject(spriteRenderer, "Color");
                    spriteRenderer.sprite =
                        AssetDatabase.LoadAssetAtPath(GetConfigTexturepath(prefabConfigItem.text), typeof(Sprite)) as
                            Sprite;
                    ColorUtility.TryParseHtmlString(prefabConfigItem.color, out _prefabColor);
                    spriteRenderer.color = _prefabColor;
                }
            }

            EditorUtility.SetDirty(go);
            //PrefabUtility.SavePrefabAsset(go);
            //PrefabUtility.SaveAsPrefabAsset(go, prefab.Key);
            //PrefabUtility.RecordPrefabInstancePropertyModifications(go);
            //Debug.Log($"Registering PrefB Modification and result was ");
            //PrefabUtility.RecordPrefabInstancePropertyModifications(go);
            //PrefabUtility.SaveAsPrefabAsset(go, prefab.Key);
            //  EditorUtility.SetDirty(go);
            //PrefabUtility.RecordPrefabInstancePropertyModifications(go);
            //PrefabUtility.ApplyPrefabInstance(go, InteractionMode.UserAction);
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }

    private void AddAndRegisterPrefabSearchMode()
    {
        GUILayout.BeginHorizontal();

        _editorState.ShowAllPrefabsInProject =
            EditorGUILayout.ToggleLeft("Show all prefabs in project", _editorState.ShowAllPrefabsInProject);

        if (_editorState.ShowAllPrefabsInProject)
        {
            _editorState.ShowAllPrefabsInDirectory = false;
            RefreshPrefabResults(AssetRootPath);
        }

        if (GUILayout.Button("Search in a directory"))
        {
            OpenPrefabDirectoryPanel();
        }

        GUILayout.EndHorizontal();
    }

    private void OpenPrefabDirectoryPanel()
    {
        try
        {
            var path = EditorUtility.OpenFolderPanel("Please select a folder for prefabs", AssetRootPath, "");

            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var directory = Application.dataPath;


            var relativePath = path.Substring(path.IndexOf("/" + AssetRootPath, StringComparison.Ordinal) + 1);


            _editorState.PrefabLocalDirectory = relativePath;
            _editorState.ShowAllPrefabsInDirectory = true;
            _editorState.ShowAllPrefabsInProject = false;
            RefreshPrefabResults(_currentPrefabDirectory);
        }

        catch (System.Exception ex)
        {
            Debug.Log(ex.Message);
        }
    }

    private void AddAndRegisterCreatedPrefabFromSelectedConfigButton()
    {
        GUILayout.Space(10);

        if (!_editorState.HasValidConfigurationSelected)
        {
            return;
        }

        if (GUILayout.Button("Create Prefab"))
        {
            CreatePrefab(_editorState.CurrentConfigurationForPreviewText);
        }
    }

    private void CreatePrefab(string itemText)
    {
        var prefabConfigItem = GetSelectedPrefabConfigItem(itemText);
        if (prefabConfigItem == null)
        {
            return;
        }

        var prefabName = prefabConfigItem.text;
        var go = new GameObject(prefabName);
        try
        {
            var textComponent = go.AddComponent<Text>();
            textComponent.text = prefabConfigItem.text;
            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite =
                AssetDatabase.LoadAssetAtPath(GetConfigTexturepath(itemText), typeof(Sprite)) as Sprite;
            ColorUtility.TryParseHtmlString(prefabConfigItem.color, out _prefabColor);
            spriteRenderer.color = _prefabColor;
            var prefabFullPath = Path.Combine(DefaultPrefabCreationPath, (go.name + ".prefab"));
            prefabFullPath = AssetDatabase.GenerateUniqueAssetPath(prefabFullPath);
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, prefabFullPath, InteractionMode.UserAction);
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }

        if (go == null)
        {
            return;
        }

        DestroyImmediate(go);
    }

    private void AddAndRegisterPrefabSearch()
    {
        GUILayout.Space(10);

        GUILayout.BeginHorizontal(GUI.skin.FindStyle("Toolbar"));

        EditorGUI.BeginChangeCheck();

        _prefabSearchString = GUILayout.TextField(_prefabSearchString,
            GUI.skin.FindStyle("ToolbarSeachTextField"));

        if (EditorGUI.EndChangeCheck())
        {
            _editorState.PrefabSearchString = _prefabSearchString;
            RefreshPrefabResults(_currentPrefabDirectory);
        }

        if (GUILayout.Button(string.Empty, GUI.skin.FindStyle("ToolbarSeachCancelButton")))
        {
            _prefabSearchString = string.Empty;
            _editorState.PrefabSearchString = _prefabSearchString;
            RefreshPrefabResults(_currentPrefabDirectory);
            GUI.FocusControl(null);
        }

        GUILayout.EndHorizontal();
    }

    private void AddAndRegisterPrefabSearchResults()
    {
        GUILayout.Space(10);

        if (_editorState.ShouldDisplayPrefabResults)
        {
            EditorGUILayout.LabelField($"Prefabs in the project : ", EditorStyles.whiteLabel);

            _prefabSearchListScrollPosition = EditorGUILayout.BeginScrollView(_prefabSearchListScrollPosition, true,
                true,
                GUILayout.ExpandHeight(true));

            foreach (var prefabItem in _prefabsInProject)
            {
                prefabItem.IsSelected = EditorGUILayout.ToggleLeft(prefabItem.DisplayFileName, prefabItem.IsSelected);
                _editorState.TogglePrefabSelection(prefabItem.IsSelected, prefabItem.Guid);
            }

            GUILayout.EndScrollView();
        }

        GUILayout.Space(10);
    }

    private void AddAndRegisterPrefabSelectedResults()
    {
        bool hasAnySelectedPrefab = _prefabsInProject.Any(p => p.IsSelected);
        if (!hasAnySelectedPrefab)
            return;

        GUILayout.Space(10);

        if (_editorState.ShouldDisplayPrefabResults)
        {
            EditorGUILayout.LabelField($"Prefabs selected for Modification : ", EditorStyles.whiteLabel);

            _prefabSelectedListScrollPosition = EditorGUILayout.BeginScrollView(_prefabSelectedListScrollPosition,
                false,
                true,
                GUILayout.ExpandHeight(true));

            foreach (var prefabItem in _prefabsInProject)
            {
                if (!prefabItem.IsSelected)
                {
                    continue;
                }

                prefabItem.IsSelected = EditorGUILayout.ToggleLeft(prefabItem.DisplayFileName, prefabItem.IsSelected);
                _editorState.TogglePrefabSelection(prefabItem.IsSelected, prefabItem.Guid);
            }

            GUILayout.EndScrollView();
        }

        GUILayout.Space(10);
    }

    #endregion

    private PrefabConfigItem GetSelectedPrefabConfigItem(string itemText)
    {
        return !string.IsNullOrWhiteSpace(itemText)
            ? _prefabConfiguration?.config?.FirstOrDefault(c => c.text.ContainsIgnoreCase(itemText))
            : null;
    }

    private void LogConfiguration()
    {
        Debug.LogWarning("Logging Prefab Configuration");
        foreach (var config in _prefabConfiguration.config)
        {
            Debug.LogWarning($" text = {config.text} , color = {config.color} and image = {config.image} ");
        }
    }

    private void RefreshPrefabResults(string path)
    {
        _prefabsInProject = GetPrefabsInPath(path);
    }

    private List<PrefabListItem> GetPrefabsInPath(string path)
    {
        var prefabList = new List<PrefabListItem>();
        var prefabGuids = AssetDatabase.FindAssets("t:prefab", new[] {path});
        if (!prefabGuids.Any())
            return prefabList;
        var prefabPaths = prefabGuids.Select(AssetDatabase.GUIDToAssetPath);
        if (!prefabGuids.Any())
            return prefabList;
        prefabList = prefabPaths.Select(filePath => new PrefabListItem()
        {
            FileName = Path.GetFileName(filePath),
            FilePath = filePath,
            IsSelected = false,
            Guid = AssetDatabase.AssetPathToGUID(filePath)
        }).ToList();

        if (_editorState.HasPrefabSearchText)
        {
            prefabList = prefabList.FindAll(f => f.FileName.ContainsIgnoreCase(_editorState.PrefabSearchString));
        }

        if (_editorState.SelectedPrefabGuids != null && _editorState.SelectedPrefabGuids.Any())
        {
            prefabList.ForEach(p => p.IsSelected = _editorState.SelectedPrefabGuids.Contains(p.Guid));
        }

        prefabList = prefabList.Where(p => HasComponentInPrefab<Text>(p.FilePath)).OrderBy(p => p.FileName)
            .ToList();
        return prefabList;
    }

    private static bool HasComponentInPrefab<T>(string path)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return prefab != null && prefab.GetComponentsInChildren<T>().Any();
    }
}