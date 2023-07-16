#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace SearchGameObjectWindow
{
    public sealed class SearchGameObjectWindow : EditorWindow
    {
        private static readonly string SEARCH_WORD_LABEL = "Search Word";
        private static readonly string IS_CASE_SENSITIVE = "Is Case Sensitive";
        private static readonly GUILayoutOption THUMBNAIL_HEIGHT_OPTION = GUILayout.Height(36);
        private static readonly GUILayoutOption THUMBNAIL_WIDTH_OPTION = GUILayout.Width(36);

        private GUIStyle _numberOfDislpayStyle;
        private GUIContent _gameObjectThumbnailContent;
        private GameObject _lastSectionGameObject;
        private TagManagerView _tagManager;
        private List<string> _targetSearchTypeNames = new();
        private SearchType _searchType = 0;
        private int _layerId = Layer.EverythingMask;
        private bool _isCaseSensitive = false;
        private string _searchWord = string.Empty;
        private Vector2 _scrollPosition = Vector2.zero;
        private Dictionary<Type, bool> _componentSelectedStatus = new();
        private Dictionary<Type, SafetyMethodInfo> _onEnableForEditorCache = new();
        private Vector2 _extraInspectorScrollPosition = Vector2.zero;
        private readonly List<GameObject> _hierarchyObjects = new();
        private readonly List<Renderer> _renderers = new();

        [MenuItem("Tools/SearchGameObjectWindow")]
        public static void CreateTool() => GetWindow<SearchGameObjectWindow>("Search GameObject");

        private void Awake() => this.ReloadCache(true);

        private void OnDestroy()
        {
            this.ClearCache();
            using (_tagManager)
                _tagManager = null;
            _lastSectionGameObject = null;
        }

        private void OnFocus() => this.ReloadCache(false);

        private void OnValidate()
        {
            this.ReloadCache(true);
            _lastSectionGameObject = null;
        }

        private void OnHierarchyChange()
        {
            this.ReloadCache(false);
            this.Repaint();
        }

        private void OnProjectChange()
        {
            this.ReloadCache(false);
            this.Repaint();
        }

        private void OnSearchConditionChanged()
        {
            _lastSectionGameObject = null;
        }

        private void OnSelectionChangedInResult()
        {
            _componentSelectedStatus.Clear();
        }

        private void OnGUI()
        {
            this.Initialize();

            // 検索条件のレイアウト処理を実行
            this.LayoutSearchCondition();

            // 検索処理実行
            var result = this.SearchObjects(
                new SearchCondition(
                    _searchWord ?? string.Empty,
                    _searchType,
                    _layerId,
                    _isCaseSensitive));

            this.LayoutSearchResult(new SearchResult(result));
        }

        private void LayoutSearchResult(SearchResult result)
        {
            using (var resultScope = new EditorGUILayout.HorizontalScope())
            {
                using (var resultItemScope = new EditorGUILayout.VerticalScope())
                {
                    // 検索結果のレイアウト処理を実行
                    this.LayoutSearchResultItem(result);
                }
                using (var optionScope = new EditorGUILayout.VerticalScope(GUILayout.MinWidth(400), GUILayout.Width(400)))
                {
                    // オプション表示用のレイアウト処理を実行
                    this.LayoutOption();
                }
            }
        }

        private void Initialize()
        {
            if (_gameObjectThumbnailContent == null)
            {
                var thumbnail = AssetPreview.GetMiniTypeThumbnail(typeof(GameObject));
                _gameObjectThumbnailContent = new GUIContent(thumbnail);
            }

            if (_numberOfDislpayStyle == null)
            {
                var style = new GUIStyle()
                {
                    alignment = TextAnchor.MiddleRight,
                };
                style.normal.textColor = EditorStyles.label.normal.textColor;
                style.focused.textColor = EditorStyles.label.focused.textColor;
                _numberOfDislpayStyle = style;
            }
        }

        private void LayoutSearchCondition()
        {
            var searchWord = _searchWord;
            var searchType = _searchType;
            var isCaseSensitive = _isCaseSensitive;
            var layerId = _layerId;
            using (var vertical = new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                searchWord = EditorGUILayoutExtensions.TextFieldWithVariableFontSize(SEARCH_WORD_LABEL, searchWord, 18);
                using (var innerHorizontal = new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel("Search Target");
                    var searchTypeValue = EditorGUILayoutExtensions.TabControl((int)searchType, _targetSearchTypeNames);
                    searchType = EnumExtensions.CastInDefined<SearchType>(searchTypeValue);
                }
                isCaseSensitive = EditorGUILayout.Toggle(IS_CASE_SENSITIVE, isCaseSensitive);

                var layers = _tagManager.GetCurrentLayerNames().ToArray();
                var layerIds = _tagManager.GetCurrentLayerIDs().ToArray();
                if (!_tagManager.UseLayer(layerId)) layerId = Layer.EverythingMask;

                layerId = EditorGUILayout.IntPopup("Layer", layerId, layers, layerIds);
            }
            if (searchWord != _searchWord || 
                searchType != _searchType || 
                isCaseSensitive != _isCaseSensitive ||
                layerId != _layerId)
            {
                _searchWord = searchWord;
                _searchType = searchType;
                _isCaseSensitive = isCaseSensitive;
                _layerId = layerId; ;
                this.OnSearchConditionChanged();
            }
        }

        private void LayoutSearchResultItem(SearchResult searchResult)
        {
            int resultCount = 0;
            using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scrollViewScope.scrollPosition;

                foreach (var (gameObject, searchTargetName) in searchResult.Result)
                {
                    using (var scope = new TempGUIbackgroundColorScope(
                        _lastSectionGameObject == gameObject ? new Color32(90, 181, 250, 230) : GUI.backgroundColor))
                    using (var itemScope = new EditorGUILayout.HorizontalScope(GUI.skin.button))
                    {
                        EditorGUILayout.LabelField(_gameObjectThumbnailContent, THUMBNAIL_HEIGHT_OPTION, THUMBNAIL_WIDTH_OPTION);
                        var iconRect = GUILayoutUtility.GetLastRect();

                        using (var vertical = new EditorGUILayout.VerticalScope())
                        {
                            EditorGUILayout.LabelField(gameObject.name, EditorStyles.boldLabel);
                            EditorGUILayout.LabelField(searchTargetName);
                        }
                        var nameLabelRect = GUILayoutUtility.GetLastRect();
                        var type = Event.current.type;
                        var position = Event.current.mousePosition;
                        if (type == EventType.MouseDown && iconRect.Union(nameLabelRect).Contains(position))
                        {
                            var currentActiveObject = _lastSectionGameObject;
                            Selection.activeGameObject = gameObject;
                            _lastSectionGameObject = gameObject;
                            if (currentActiveObject != gameObject)
                            {
                                this.OnSelectionChangedInResult();
                            }
                            this.Repaint();
                        }
                    }
                    resultCount++;
                }
            }
            EditorGUILayout.LabelField($@"Number of display {resultCount}", _numberOfDislpayStyle);
        }

        private void LayoutOption()
        {
            var searchWindowSelectionObject = _lastSectionGameObject;
            if (searchWindowSelectionObject == null) return;

            using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(_extraInspectorScrollPosition))
            {
                _extraInspectorScrollPosition = scrollViewScope.scrollPosition;
                foreach (var component in searchWindowSelectionObject.GetComponents<Component>())
                {
                    var componentType = component.GetType();
                    if (!_componentSelectedStatus.ContainsKey(componentType))
                    {
                        _componentSelectedStatus[componentType] = true;
                    }
                    _componentSelectedStatus[componentType] = EditorGUILayoutExtensions.FoldoutInspectorSimpleHeader(
                        _componentSelectedStatus[componentType],
                        _gameObjectThumbnailContent,
                        componentType.Name);

                    if (!_componentSelectedStatus[componentType]) continue;

                    if (component is Transform)
                    {
                        var transformEditor = (SimpleEditor.TransformInspectorSimpleEditor)Editor.CreateEditor(component, typeof(SimpleEditor.TransformInspectorSimpleEditor));
                        transformEditor.OnEnable();
                        transformEditor.OnInspectorGUI();
                    }
                    else
                    {
                        var editor = Editor.CreateEditor(component);
                        var editorType = editor.GetType();
                        if (!_onEnableForEditorCache.ContainsKey(editorType))
                        {
                            _onEnableForEditorCache[editorType] = new SafetyMethodInfo(editorType, "OnEnable");
                        }
                        try
                        {
                            _onEnableForEditorCache[editorType].Invoke(editor, null);
                            editor.OnInspectorGUI();
                        }
                        catch (InvalidCastException ice)
                        {
                            //TODO: CameraEditor.OnInspectorGUIでエラーが発生するため、調査中...
                            Debug.LogWarning(ice);
                        }
                    }
                }
            }
        }

        private void ClearCache()
        {
            _hierarchyObjects.Clear();
            _renderers.Clear();
        }

        private void ReloadCache(bool initializing)
        {
            if (initializing)
            {
                this.ReloadSearchType();
                _tagManager ??= new TagManagerView();
            }

            this.ClearCache();
            _hierarchyObjects.AddRange(this.EnumerateHierarchyObjectsFromAllObjects());
            _renderers.AddRange(Resources.FindObjectsOfTypeAll<MeshRenderer>());
            _renderers.AddRange(Resources.FindObjectsOfTypeAll<SkinnedMeshRenderer>());
            _tagManager?.UpdateCache();
        }

        private void ReloadSearchType()
        {
            _targetSearchTypeNames.Clear();
            foreach (var searchType in EnumExtensions.GetValues<SearchType>())
            {
                _targetSearchTypeNames.Add(searchType.ToAliasName());
            }
        }

        private IEnumerable<GameObject> EnumerateHierarchyObjectsFromAllObjects()
        {
            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach(var obj in allObjects)
            {
                if (this.IsGameObjectInHierarchyObjects(obj.hideFlags))
                {
                    yield return obj;
                }
            }
        }

        private bool IsGameObjectInHierarchyObjects(HideFlags flags)
        {
            var targetFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.NotEditable;

            if ((flags & targetFlags) != 0)
            {
                return false;
            }

            return (flags & HideFlags.HideAndDontSave) != HideFlags.HideAndDontSave;
        }

        private IEnumerable<(GameObject gameObject,string searchTargetName)> SearchObjects(SearchCondition condition)
        {
            // 空入力の場合には検索しない。
            if (condition.IsEnteredWord) return Array.Empty<(GameObject, string)>();

            switch (condition.Type)
            {
                case SearchType.GameObject:
                    return this.SearchObjectsByGameObjectName(condition);
                case SearchType.Material:
                case SearchType.Shader:
                    return this.SearchObjectsByMaterial(condition);
                case SearchType.Component:
                    return this.SearchObjectsByComponent(condition);
            }
            return Array.Empty<(GameObject, string)>();
        }

        private IEnumerable<(GameObject, string)> SearchObjectsByGameObjectName(SearchCondition condition)
        {
            var hierarchyObjects = _hierarchyObjects;
            foreach (var obj in hierarchyObjects)
            {
                if (obj == null) continue;

                if (condition.IsTargetLayer(obj.layer) && obj.name.IndexOf(condition.EnteredWord, condition.Comparison) >= 0)
                {
                    yield return (obj, obj.name);
                }
            }
        }

        private IEnumerable<(GameObject, string)> SearchObjectsByMaterial(SearchCondition condition)
        {
            foreach (var render in _renderers)
            {
                if (render == null) continue;

                foreach (var material in render.sharedMaterials)
                {
                    var searchTypeName = this.GetSeachTypeNameFromMaterial(material);
                    var index = searchTypeName.IndexOf(condition.EnteredWord, condition.Comparison);
                    if (index < 0) continue;

                    var obj = render.gameObject;
                    if (obj == null) continue;

                    if (condition.IsTargetLayer(obj.layer) && _hierarchyObjects.Contains(obj))
                    {
                        yield return (obj, searchTypeName);
                    }
                }
            }
        }

        private string GetSeachTypeNameFromMaterial(Material m)
        {
            if (m == null) return string.Empty;

            switch (_searchType)
            {
                case SearchType.Material:
                    return m.name;
                case SearchType.Shader:
                    if (m.shader == null) return string.Empty;
                    return m.shader.name;
            }

            return string.Empty;
        }

        private IEnumerable<(GameObject, string)> SearchObjectsByComponent(SearchCondition condition)
        {
            var hierachyObjects =  _hierarchyObjects;

            foreach(var obj in hierachyObjects)
            {
                if (obj == null || !condition.IsTargetLayer(obj.layer)) continue;

                var components = obj.GetComponents<Component>();
                var componentNamesBuilder = new StringBuilder();
                componentNamesBuilder.AppendJoin(",", this.FilterComponent(components, condition));
                var componentNames = componentNamesBuilder.ToString();

                if (string.IsNullOrWhiteSpace(componentNames)) continue;
                yield return (obj, componentNames);
            }
        }

        private IEnumerable<string> FilterComponent(IEnumerable<Component> components, SearchCondition condition)
        {
            foreach (var component in components)
            {
                if (component == null) continue;

                var componentName = component.GetType().Name;
                if (componentName.IndexOf(condition.EnteredWord, condition.Comparison) >= 0)
                {
                    yield return componentName;
                }
            }
        }

        private enum SearchType
        {
            [AliasName("Game Object")]
            GameObject = 0,
            [AliasName("Material")]
            Material = 1,
            [AliasName("Shader")]
            Shader = 2,
            [AliasName("Component")]
            Component = 3,
        }

        private sealed record SearchCondition
        {
            public string EnteredWord { get; }
            public SearchType Type { get; }
            public int LayerId { get; }
            public bool IsCaseSensitive { get; }
            public bool IsEnteredWord => string.IsNullOrWhiteSpace(this.EnteredWord);
            public StringComparison Comparison => this.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            public StringComparer Comparer => this.IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

            public SearchCondition(string searchword, SearchType searchType, int layerId, bool isCaseSensitive)
            {
                EnteredWord = searchword ?? string.Empty;
                Type = searchType;
                LayerId = layerId;
                IsCaseSensitive = isCaseSensitive;
            }

            public bool IsTargetLayer(int objectLayerId)
            {
                if (LayerId == Layer.EverythingMask) return true;
                return objectLayerId == this.LayerId;
            }

        }

        private sealed class SearchResult
        {
            public IEnumerable<(GameObject gameObject, string searchTargetName)> Result { get; }

            public SearchResult(IEnumerable<(GameObject gameObject, string searchTargetName)> result)
            {
                this.Result = result;
            }

        }

    }


}

#endif