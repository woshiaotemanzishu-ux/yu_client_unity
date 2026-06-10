using System.Collections.Generic;

namespace Shenxiao.EditorTools.Lanhu
{
    public sealed class LanhuPackage
    {
        public string module;
        public string assetRoot;
        public List<LanhuView> views = new List<LanhuView>();
    }

    public sealed class LanhuView
    {
        public string name;
        public string title;
        public string layer;
        public bool local;
        public int width = 720;
        public int height = 1280;
        public List<LanhuNode> nodes = new List<LanhuNode>();
    }

    public sealed class LanhuNode
    {
        public string name;
        public string type;
        public string text;
        public string color;
        public string image;
        public string source;
        public float? x;
        public float? y;
        public float? width;
        public float? height;
        public float? alpha;
        public float? fontSize;
        public bool? visible;
        public bool? raycast;
        public List<LanhuNode> children = new List<LanhuNode>();
    }

    public sealed class LanhuImportReport
    {
        public int views;
        public int prefabs;
        public int bindScripts;
        public int copiedImages;
        public readonly List<string> missingImages = new List<string>();
        public readonly List<string> pendingComponents = new List<string>();
        public readonly List<string> warnings = new List<string>();
    }

    internal sealed class LanhuBindField
    {
        public string fieldName;
        public string componentTypeName;
        public string nodePath;
        public System.Type componentType;
    }
}
