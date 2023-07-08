#if UNITY_EDITOR

namespace SearchGameObjectWindow
{
    internal sealed record Layer
    {
        public readonly static Layer Everything = new(~0, "EveryThing");
        public readonly static int EverythingMask = Everything.Id;
        public readonly static string EverythingName = Everything.Name;

        public int Id { get; }
        public string Name { get; }

        public Layer(int Id, string name)
        {
            this.Id = Id;
            this.Name = name;
        }
    }
}
#endif
