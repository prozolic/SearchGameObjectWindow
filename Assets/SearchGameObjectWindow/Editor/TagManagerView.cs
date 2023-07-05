#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;


namespace SearchGameObjectWindow
{
    public sealed class TagManagerView : IDisposable
    {
        private bool _disposed = false;
        private SerializedObject _tagmanager;
        private List<Layer> _layers = new();

        public TagManagerView()
        {
            var tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManager.Length == 0) return;

            _tagmanager = new SerializedObject(tagManager[0]);
            this.UpdateCache();
        }

        public void UpdateCache()
        {
            _layers.Clear();
            _tagmanager.Update();

            _layers.Add(Layer.Everything);
            using (var layers = _tagmanager.FindProperty("layers"))
            {
                for (int layerId = 0; layerId < layers.arraySize; layerId++)
                {
                    var layer = layers.GetArrayElementAtIndex(layerId);
                    if (string.IsNullOrWhiteSpace(layer.stringValue)) continue;
                    _layers.Add(new Layer(layerId, layer.stringValue));
                }
            }
        }

        public IEnumerable<int> GetCurrentLayerIDs()
        {
            foreach (var layer in _layers)
            {
                yield return layer.Id;
            }
        }

        public IEnumerable<string> GetCurrentLayerNames()
        {
            foreach (var layer in _layers)
            {
                yield return layer.Name;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            using (_tagmanager)
                _tagmanager = null;

        }

        public sealed record Layer
        {
            public int Id { get; }
            public string Name { get; }

            public Layer(int Id, string name) 
            {
                this.Id = Id;
                this.Name = name;
            }
            public readonly static Layer Everything = new(~0, "EveryThing");
            public readonly static int EverythingMask = Everything.Id;
            public readonly static string EverythingName = Everything.Name;
        }
    }
}
#endif