#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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
        private List<string> _targetSearchTypeNames = new();
        private SearchType _searchType = 0;
        private bool _isCaseSensitive = false;
        private string _searchWord = string.Empty;
        private Vector2 _scrollPosition = Vector2.zero;
        private readonly List<GameObject> _allObjects = new();
        private readonly List<GameObject> _hierarchyObjects = new();
        private readonly List<Renderer> _renderers = new();

        [MenuItem("Tools/SearchGameObjectWindow")]
        public static void CreateTool() => GetWindow<SearchGameObjectWindow>("Search GameObject");

        private void Awake()
        {
            this.ReloadCache(true);
            this.Initialize();
        }

        private void OnDestroy()
        {
            this.ClearCache();
            _lastSectionGameObject = null;
        }

        private void OnFocus() => this.ReloadCache(false);

        private void OnValidate()
        {
            this.ReloadCache(true);
            this.Initialize();
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

        private void OnGUI()
        {
            // 検索条件のレイアウト処理を実行
            this.LayoutSearchCondition();

            // 検索処理実行
            var result = this.SearchObjects(
                new SearchCondition(
                    _searchWord ?? string.Empty,
                    _searchType,
                    _isCaseSensitive));

            int resultCount = 0;
            using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scrollViewScope.scrollPosition;

                foreach (var (gameObject, searchTargetName) in result)
                {
                    using (var scope = new TempGUIbackgroundColorScope(
                        _lastSectionGameObject == gameObject ? new Color32(90 ,181 ,250, 230) : GUI.backgroundColor))
                    using (var itemScope = new EditorGUILayout.HorizontalScope(GUI.skin.button))
                    {
                        EditorGUILayout.LabelField(_gameObjectThumbnailContent, THUMBNAIL_HEIGHT_OPTION, THUMBNAIL_WIDTH_OPTION);

                        using (var vertical = new EditorGUILayout.VerticalScope())
                        {
                            EditorGUILayout.LabelField(gameObject.name, EditorStyles.boldLabel);
                            EditorGUILayout.LabelField(searchTargetName);
                        }
                        var rect = GUILayoutUtility.GetLastRect();
                        var type = Event.current.type;
                        var position = Event.current.mousePosition;
                        if (type == EventType.MouseDown && rect.Contains(position))
                        {
                            Selection.activeGameObject = gameObject;
                            _lastSectionGameObject = gameObject;
                            this.Repaint();
                        }
                    }
                    resultCount++;
                }
            }
            EditorGUILayout.LabelField($@"Number of display {resultCount}", _numberOfDislpayStyle);
        }

        private void LayoutSearchCondition()
        {
            var searchWord = _searchWord;
            var searchType = _searchType;
            using (var vertical = new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                searchWord = EditorGUILayoutExtensions.TextFieldWithVariableFontSize(SEARCH_WORD_LABEL, searchWord, 18);
                _isCaseSensitive = EditorGUILayout.Toggle(IS_CASE_SENSITIVE, _isCaseSensitive);
            }
            using (var vertical = new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                var searchTypeValue = EditorGUILayoutExtensions.TabControl((int)searchType, _targetSearchTypeNames);
                searchType = EnumExtensions.CastInDefined<SearchType>(searchTypeValue);
            }
            if (searchWord != _searchWord || searchType != _searchType)
            {
                _searchWord = searchWord;
                _searchType = searchType;
                this.OnSearchConditionChanged();
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

        private void ClearCache()
        {
            _allObjects.Clear();
            _hierarchyObjects.Clear();
            _renderers.Clear();
        }

        private void ReloadCache(bool initializing)
        {
            if (initializing) this.ReloadSearchType();

            this.ClearCache();
            _allObjects.AddRange(Resources.FindObjectsOfTypeAll<GameObject>());
            _hierarchyObjects.AddRange(this.EnumerateHierarchyObjectsFromAllObjects());
            _renderers.AddRange(Resources.FindObjectsOfTypeAll<MeshRenderer>());
            _renderers.AddRange(Resources.FindObjectsOfTypeAll<SkinnedMeshRenderer>());
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
            var allObjects = _allObjects;
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
            var searchTarget = _hierarchyObjects;
            foreach (var obj in searchTarget)
            {
                if (obj != null && obj.name.IndexOf(condition.EnteredWord, condition.Comparison) >= 0)
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

                    if (_hierarchyObjects.Contains(obj))
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
                if (obj == null) continue;

                var components = obj.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (component == null) continue;

                    var componentName = component.GetType().Name;
                    if (componentName.IndexOf(condition.EnteredWord, condition.Comparison) >= 0)
                    {
                        yield return (obj, componentName);
                    }
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
            public bool IsCaseSensitive { get; }
            public bool IsEnteredWord => string.IsNullOrWhiteSpace(this.EnteredWord);
            public StringComparison Comparison => this.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            public StringComparer Comparer => this.IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

            public SearchCondition(string searchword, SearchType searchType, bool isCaseSensitive)
            {
                EnteredWord = searchword ?? string.Empty;
                Type = searchType;
                IsCaseSensitive = isCaseSensitive;
            }
        }
    }
}

#endif