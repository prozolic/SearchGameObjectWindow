#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace SearchGameObjectWindow
{
    public sealed class SearchGameObjectWindow : EditorWindow
    {
        private static readonly string SEARCH_WORD_LABEL = "Search Word";
        private static readonly string IS_CASE_SENSITIVE = "Is Case Sensitive";

        private string[] _targetTypeNames;
        private SearchType _searchType = 0;
        private bool _isCaseSensitive = false;
        private string _searchWord = string.Empty;
        private Vector2 _scrollPosition = Vector2.zero;
        private readonly List<GameObject> _allObjects = new();
        private readonly List<GameObject> _hierarchyObjects = new();
        private readonly List<Renderer> _renderers = new();

        [MenuItem("Tools/SearchGameObjectWindow")]
        public static void CreateTool() => GetWindow<SearchGameObjectWindow>("Search GameObject");

        private void Awake() => this.ReloadCache(true);

        private void OnDestroy() => this.ClearCache();

        private void OnFocus() => this.ReloadCache(false);

        private void OnValidate() => this.ReloadCache(true);

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

        private void OnGUI()
        {
            using (var vertical = new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                _searchWord = EditorGUILayoutExtensions.TextFieldWithVariableFontSize(SEARCH_WORD_LABEL, _searchWord, 18);
                _isCaseSensitive = EditorGUILayout.Toggle(IS_CASE_SENSITIVE, _isCaseSensitive);
            }
            using (var vertical = new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                var typeNames = _targetTypeNames;
                var searchTypeValue = EditorGUILayoutExtensions.TabControl((int)_searchType, typeNames);
                _searchType = EnumExtensions.CastInDefined<SearchType>(searchTypeValue);
            }

            // åüçıèàóùé¿çs
            var result = this.SearchObjects(
                new SearchCondition(
                    _searchWord ?? string.Empty,
                    _searchType,
                    _isCaseSensitive)).ToArray();

            using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scrollViewScope.scrollPosition;
                foreach (var r in result)
                {
                    if (GUILayout.Button($@"{r.gameObject.name}({r.searchTargetName})"))
                    {
                        Selection.activeGameObject = r.gameObject;
                    }
                }
            }
            var style = new GUIStyle()
            {
                alignment = TextAnchor.MiddleRight,
            };
            style.normal.textColor = EditorStyles.label.normal.textColor;
            style.focused.textColor = EditorStyles.label.focused.textColor;
            EditorGUILayout.LabelField($@"Number of display {result.Length}", style);
        }

        private void ClearCache()
        {
            _allObjects.Clear();
            _hierarchyObjects.Clear();
            _renderers.Clear();
        }

        private void ReloadCache(bool initializing)
        {
            this.ClearCache();

            if (initializing)
            {
                this.ReloadSearchInfo();
            }

            _allObjects.AddRange(Resources.FindObjectsOfTypeAll<GameObject>());
            _hierarchyObjects.AddRange(_allObjects.Where(o => this.IsGameObjectInHierarchyObjects(o.hideFlags)));
            _renderers.AddRange(Resources.FindObjectsOfTypeAll<MeshRenderer>());
            _renderers.AddRange(Resources.FindObjectsOfTypeAll<SkinnedMeshRenderer>());
        }

        private void ReloadSearchInfo()
        {
            _targetTypeNames = EnumExtensions.GetValues<SearchType>().Select(e => e.ToAliasName()).ToArray();
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
            // ãÛì¸óÕÇÃèÍçáÇ…ÇÕåüçıÇµÇ»Ç¢ÅB
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
            foreach (var render in _renderers.Where(r => r != null))
            {
                foreach (var material in render.sharedMaterials)
                {
                    var searchTypeName = this.GetSeachTypeNameFromMaterial(material);
                    var index = searchTypeName.IndexOf(condition.EnteredWord, condition.Comparison);
                    if (index < 0) break;

                    var obj = render.gameObject;
                    if (obj == null) break;

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

            foreach(var obj in hierachyObjects.Where(o => o != null))
            {
                var components = obj.GetComponents<Component>();
                foreach (var component in components.Where(c => c != null))
                {
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