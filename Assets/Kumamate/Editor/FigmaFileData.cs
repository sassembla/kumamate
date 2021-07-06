using System;
using UnityEngine;

namespace Kumamate
{
    public class FileNameEscape
    {
        public static string Escape(string source)
        {
            // C#のfile create APIは/をエスケープする手段がないので/を-にする。
            // macのfile create系は:を自動的に/に変換するので、事前に-にする。
            return source.Replace("/", "-").Replace(":", "-");
        }
    }

    [Serializable]
    public class FigmaFrameData
    {
        [SerializeField] public string id;
        [SerializeField] public FigmaRect absRect;
        [SerializeField] public FigmaContent[] children;
        public FigmaFrameData(string name, string id, FigmaRect absRect, FigmaContent[] children)
        {
            this.id = FileNameEscape.Escape(name + "=" + id);
            this.absRect = absRect;
            this.children = children;
        }
    }

    [Serializable]
    public class FigmaContent
    {
        [SerializeField] public string type;
        [SerializeField] public string id;
        [SerializeField] public FigmaRect absRect;
        [SerializeField] public FigmaContent[] children;

        [SerializeField] public FigmaRectangle isRectangle;
        [SerializeField] public FigmaText isText;

        public FigmaContent(
            string type, string name, string id, FigmaRect absRect, FigmaContent[] children,
            object content = null
        )
        {
            this.type = type;
            this.id = FileNameEscape.Escape(name + "=" + id);
            this.absRect = absRect;
            this.children = children;

            // コンテンツであれば、種類ごとにパラメータを持つ。
            if (content is FigmaRectangle)
            {
                this.isRectangle = (FigmaRectangle)content;
            }
            if (content is FigmaText)
            {
                this.isText = (FigmaText)content;
            }
        }
    }


    [Serializable]
    public class FigmaRectangle
    {
        [SerializeField] public float r;
        [SerializeField] public float g;
        [SerializeField] public float b;
        [SerializeField] public float a;

        public FigmaRectangle(float r, float g, float b, float a)
        {
            // this.isRectangle = true;
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }
    }


    [Serializable]
    public class FigmaText
    {
        [SerializeField] public string characters;
        [SerializeField] public string fontPostScriptName;
        [SerializeField] public int fontSize;
        [SerializeField] public float lineHeightPercent;
        [SerializeField] public float r;
        [SerializeField] public float g;
        [SerializeField] public float b;
        [SerializeField] public float a;

        public FigmaText(string characters, string fontPostScriptName, int fontSize, float lineHeightPercent, float r, float g, float b, float a)
        {
            this.characters = characters;
            this.fontPostScriptName = fontPostScriptName;
            this.fontSize = fontSize;
            this.lineHeightPercent = lineHeightPercent;
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }
    }


    [Serializable]
    public class FigmaRect
    {
        [SerializeField] public float x;
        [SerializeField] public float y;
        [SerializeField] public float width;
        [SerializeField] public float height;
        public FigmaRect(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }
    }
}