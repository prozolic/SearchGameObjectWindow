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
        private static readonly string[] _targetRangeName = new string[] { "All", "Hierarchy" };
        private static readonly string[] _targetTypeName = new string[] { "GameObject", "Material", "Shader" };

        private int _targetRange = 0;
        private int _targetType = 0;
        private bool _isCaseSensitive = false;
        private string _searchWord = string.Empty;
        private Vector2 _scrollPosition = Vector2.zero;
        private readonly List<GameObject> _allObjects = new();
        private readonly List<GameObject> _hierarchyObjects = new();
        private readonly List<Renderer> _renderers = new();

        [MenuItem("Tools/SearchGameObjectWindow")]
        public static void CreateTool() => GetWindow<SearchGameObjectWindow>("Search GameObject");

        private void Awake() => this.ReloadCache();

        private void OnDestroy() => this.ClearCache();

        private void OnFocus() => this.ReloadCache();

        private void OnHierarchyChange()
        {
            this.ReloadCache();
            this.Repaint();
        }

        private void OnProjectChange()
        {
            this.ReloadCache();
            this.Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayoutExtensions.DropShadowLabel("Search Gameobject by word.");
            using (var vertical = new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                _searchWord = EditorGUILayout.TextField("Search word", _searchWord);
                _isCaseSensitive = EditorGUILayout.Toggle("Is Case Sensitive", _isCaseSensitive);
            }
            using (var vertical = new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("Search range");
                _targetRange = GUILayout.SelectionGrid(_targetRange, _targetRangeName, _targetRangeName.Length);
            }
            using (var vertical = new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("Search type");
                _targetType = GUILayout.SelectionGrid(_targetType, _targetTypeName, _targetTypeName.Length);
            }
            GUILayout.Space(5);

            // åüçıèàóùé¿çs
            var result = this.SearchObjects(
                new SearchCondition(
                    _searchWord ?? string.Empty,
                    _targetRange,
                    _targetType,
                    _isCaseSensitive)).ToArray();

            var style = new GUIStyle()
            {
                alignment = TextAnchor.MiddleRight,
            };
            style.normal.textColor = EditorStyles.label.normal.textColor;
            style.focused.textColor = EditorStyles.label.focused.textColor;
            EditorGUILayout.LabelField($@"Number of display {result.Length}", style);

            using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scrollViewScope.scrollPosition;
                foreach (var obj in result)
                {
                    if (GUILayout.Button(obj.name))
                    {
                        Selection.activeGameObject = obj;
                    }
                }
            }
        }

        private void ClearCache()
        {
            _allObjects.Clear();
            _hierarchyObjects.Clear();
            _renderers.Clear();
        }

        private void ReloadCache()
        {
            this.ClearCache();
            _allObjects.AddRange(Resources.FindObjectsOfTypeAll<GameObject>());
            _hierarchyObjects.AddRange(_allObjects.Where(o => this.IsGameObjectInHierarchyObjects(o.hideFlags)));
            _renderers.AddRange(Resources.FindObjectsOfTypeAll<MeshRenderer>());
            _renderers.AddRange(Resources.FindObjectsOfTypeAll<SkinnedMeshRenderer>());
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

        private IEnumerable<GameObject> SearchObjects(SearchCondition condition)
        {
            // ãÛì¸óÕÇÃèÍçáÇ…ÇÕåüçıÇµÇ»Ç¢ÅB
            if (condition.IsEnteredWord) return Array.Empty<GameObject>();

            switch (condition.Type)
            {
                case SearchType.GameObject:
                    return this.SearchObjectsByGameObjectName(condition);
                case SearchType.Material:
                case SearchType.Shader:
                    return this.SearchObjectsByMaterial(condition);
            }
            return Array.Empty<GameObject>();
        }

        private IEnumerable<GameObject> SearchObjectsByGameObjectName(SearchCondition condition)
        {
            var searchTarget = condition.IsHierarchyOnly ? _hierarchyObjects : _allObjects;
            return searchTarget.Where(o =>
                o != null && o.name.IndexOf(condition.EnteredWord, condition.Comparison) >= 0
            );
        }

        private IEnumerable<GameObject> SearchObjectsByMaterial(SearchCondition condition)
        {
            foreach (var render in _renderers.Where(r => r != null))
            {
                foreach (var material in render.sharedMaterials)
                {
                    var index = this.GetSearchTargetTagName(material).IndexOf(condition.EnteredWord, condition.Comparison);
                    if (index < 0) break;

                    var obj = render.gameObject;
                    if (obj == null) break;

                    if (condition.IsHierarchyOnly)
                    {
                        if (_hierarchyObjects.Contains(obj))
                        {
                            yield return obj;
                        }
                        break;
                    }
                    yield return obj;
                }
            }
        }

        private string GetSearchTargetTagName(Material m)
        {
            if (m == null) return string.Empty;

            switch ((SearchType)_targetType)
            {
                case SearchType.Material:
                    return m.name;
                case SearchType.Shader:
                    if (m.shader == null) return string.Empty;
                    return m.shader.name;
            }

            return string.Empty;
        }


        private enum SearchRange
        {
            All = 0,
            Hierarchy = 1
        }

        private enum SearchType
        {
            GameObject = 0,
            Material = 1,
            Shader = 2,
        }

        private sealed record SearchCondition
        {
            public string EnteredWord { get; }
            public SearchRange Range { get; }
            public SearchType Type { get; }
            public bool IsCaseSensitive { get; }
            public bool IsEnteredWord => string.IsNullOrWhiteSpace(this.EnteredWord);
            public bool IsHierarchyOnly => this.Range == SearchRange.Hierarchy;
            public StringComparison Comparison => this.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            public SearchCondition(string searchword, int searchRange, int searchType, bool isCaseSensitive)
            {
                EnteredWord = searchword ?? string.Empty;
                Range = (SearchRange)searchRange;
                Type = (SearchType)searchType;
                IsCaseSensitive = isCaseSensitive;
            }
        }
    }
}

#endif