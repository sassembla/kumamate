using System;
using System.IO;
using Kumamate;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

// TODO: 実験中のクラス、最終的にはUIの生成を助けるロケーター穴の生成を大量に行い、セットアップして参照されているオブジェクトのあらゆる値を更新する。
public class KumaUIElementWindow : EditorWindow
{
    private string filePath;
    // TODO: このへんにスロットのインスタンスもガッと持ちそう(そうしないとコンパイルで消される)

    public void OnEnable()
    {
        Debug.Log("onEnable filePath:" + filePath);
        if (string.IsNullOrEmpty(filePath))
        {
            // パラメータがセットされていないので何もしない
            return;
        }

        ReloadView(filePath);
    }


    public void Setup(string _filePath)
    {
        filePath = _filePath;
        ReloadView(filePath);
    }

    private void ReloadView(string filePath)
    {
        var name = Path.GetFileNameWithoutExtension(filePath);

        var bytes = File.ReadAllBytes(filePath);
        var frameData = FigmaFrameData.Parser.ParseFrom(bytes);

        this.rootVisualElement.Clear();

        // 一番外側のスクロール要素を出す、これをつけないと要素が画面外に行ってしまうと表示できない。
        var scrollViewRoot = new ScrollView(ScrollViewMode.Vertical);
        scrollViewRoot.style.height = new StyleLength() { value = new Length(100f, LengthUnit.Percent) };

        this.rootVisualElement.Add(scrollViewRoot);


        // 中身の親となる箱を組み立てていく
        // TODO: これ等倍でやるとマジで難しいので、なんとかしてウィンドウのアスペクトみたいなものを用意したいところ。w,hを持たせたrelativeみたいなのがやりたい。
        var rootArea = new VisualElement();
        rootArea.style.width = new StyleLength() { value = new Length(frameData.AbsRect.Width, LengthUnit.Pixel) };
        rootArea.style.height = new StyleLength() { value = new Length(frameData.AbsRect.Height, LengthUnit.Pixel) };
        rootArea.Add(new Label("p:" + frameData.Identifier));

        scrollViewRoot.Add(rootArea);

        /*
            これを再現する。
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements" editor-extension-mode="False">
<ui:VisualElement style="flex-direction: row; align-items: auto; justify-content: center;">
    <uie:ObjectField name="the-uxml-field" style="width: 33%;"/>
    <ui:Button text="Button" display-tooltip-when-elided="true" style="width: 33%; -unity-text-align: middle-center; white-space: nowrap;" />
    <ui:Button text="Button" display-tooltip-when-elided="true" style="width: 33%; -unity-text-align: middle-center; white-space: nowrap;" />
</ui:VisualElement>
<ui:VisualElement style="flex-direction: row; align-items: auto; justify-content: center;">
    <ui:Button text="Button" display-tooltip-when-elided="true" style="width: 50%; -unity-text-align: middle-center; white-space: nowrap;" />
    <ui:Button text="Button" display-tooltip-when-elided="true" style="width: 50%; -unity-text-align: middle-center; white-space: nowrap;" />
</ui:VisualElement>
<uie:ObjectField label="UXMLaaaa Field" name="the-uxml-field" />
</ui:UXML>
        */

        var baseRect = new FigmaRect()
        {
            X = 0,
            Y = 0,
            Width = 0,
            Height = 0
        };
        // ここから先でchildrenをガンガンレイアウトする。
        foreach (var child in frameData.Children)
        {
            DoLayout(rootArea, baseRect, child, 1);
            // Debug.Log("child:" + ReadFigmaRect(child.AbsRect) + " child-children:" + child.Children.Count);
        }

        return;

        // var leftArea = new VisualElement();
        // rootArea.Add(leftArea);
        // leftArea.style.width = new StyleLength() { value = new Length(50f, LengthUnit.Percent) };
        // leftArea.Add(new TextField("Tex"));
        // leftArea.Add(new MinMaxSlider("MMSlid"));
        // leftArea.Add(new Button() { text = "BBBB" });
        // leftArea.Add(new Button() { text = "BBBB" });
        // leftArea.Add(new Button() { text = "BBBB" });
        // leftArea.Add(new Button() { text = "BBBB" });
        // leftArea.Add(new Button() { text = "BBBB" });
        // leftArea.Add(new Button() { text = "BBBB" });
        // leftArea.Add(new Button() { text = "BBBB" });
        // leftArea.Add(new Button() { text = "BBBB" });
        // leftArea.Add(new Button() { text = "BBBB" });
        // leftArea.Add(new Button() { text = "BBBB" });
        // leftArea.Add(new Button() { text = "BBBB" });
        // leftArea.Add(new Button() { text = "BBBB" });
        // leftArea.Add(new Button() { text = "BBBB" });
        // leftArea.Add(new Button() { text = "BBBB" });
        // leftArea.Add(new Button() { text = "BBBB" });
        // leftArea.Add(new Button() { text = "BBBB" });
        // leftArea.Add(new Button() { text = "BBBB" });
        // leftArea.Add(new Button() { text = "BBBB" });
        // leftArea.Add(new Button() { text = "BBBB" });


        // var rightArea = new VisualElement();
        // rootArea.Add(rightArea);
        // rightArea.style.position = new StyleEnum<Position>() { value = Position.Absolute };// うーん、3つ並べてえいって感じじゃないのかな、、
        // rightArea.style.left = new StyleLength() { value = new Length(50, LengthUnit.Percent) };// アンカリングをコードでやるのやめてくれ、、せめてもっと綺麗にセットさせてくれ、、、
        // rightArea.style.width = new StyleLength() { value = new Length(50, LengthUnit.Percent) };
        // rightArea.style.height = new StyleLength() { value = new Length(100, LengthUnit.Percent) };

        // var scrollView = new ScrollView(ScrollViewMode.Vertical);
        // rightArea.Add(scrollView);
        // scrollView.style.height = new StyleLength() { value = new Length(100f, LengthUnit.Percent) };// ここでスクロールビュー高さが決まっていた
        // for (var i = 0; i < 100; i++)
        // {
        //     scrollView.Add(new Label("label:" + i));
        // }
    }

    private void DoLayout(VisualElement parent, FigmaRect parentRect, FigmaContent child, int depth)
    {
        var absPos = child.AbsRect;
        var relativePos = absPos.Minus(parentRect);

        var lineWidth = 0.1f;
        var currentArea = new VisualElement();
        currentArea.style.position = new StyleEnum<Position>() { value = Position.Absolute };
        currentArea.style.left = new StyleLength() { value = new Length(relativePos.X, LengthUnit.Pixel) };
        currentArea.style.top = new StyleLength() { value = new Length(-relativePos.Y, LengthUnit.Pixel) };
        currentArea.style.width = new StyleLength() { value = new Length(absPos.Width - lineWidth * 2, LengthUnit.Pixel) };
        currentArea.style.height = new StyleLength() { value = new Length(absPos.Height - lineWidth * 2, LengthUnit.Pixel) };

        // TODO: ランダムにカラーをつけてるが、とりあえず不要な要素には色をつけないようにしたい。ここで除外するのが良さそう。
        currentArea.style.backgroundColor = new StyleColor() { value = new Color(UnityEngine.Random.Range(0f, 1f), ((byte)UnityEngine.Random.Range(0f, 1f)), UnityEngine.Random.Range(0f, 1f), 0.1f * depth) };
        switch (child.Type)
        {
            case "INSTANCE":
            case "FRAME":
            case "TEXT":
            case "RECTANGLE":
            case "ELLIPSE":
                currentArea.Add(new Label("type:" + child.Type));// TODO: 適当につけてる、ここが変わると良さそう
                // var objField = new ObjectF
                var borderColor = new StyleColor(Color.red);
                currentArea.style.borderTopColor = borderColor;
                currentArea.style.borderRightColor = borderColor;
                currentArea.style.borderBottomColor = borderColor;
                currentArea.style.borderLeftColor = borderColor;
                currentArea.style.borderTopWidth = lineWidth;
                currentArea.style.borderRightWidth = lineWidth;
                currentArea.style.borderBottomWidth = lineWidth;
                currentArea.style.borderLeftWidth = lineWidth;
                break;
            default:
                // デフォは透明にする
                currentArea.style.backgroundColor = new StyleColor() { value = new Color(UnityEngine.Random.Range(0f, 1f), ((byte)UnityEngine.Random.Range(0f, 1f)), UnityEngine.Random.Range(0f, 1f), 0f) };
                break;
        }

        parent.Add(currentArea);

        depth = depth + 1;
        foreach (var cousin in child.Children)
        {
            DoLayout(currentArea, absPos, cousin, depth);
        }
    }

    private string ReadFigmaRect(FigmaRect figmaRect)
    {
        return "x:" + figmaRect.X + " y:" + figmaRect.Y + " w:" + figmaRect.Width + " h:" + figmaRect.Height;
    }
}


public static class FigmaRectExtension
{
    public static FigmaRect Minus(this FigmaRect target, FigmaRect minus)
    {
        var result = new FigmaRect()
        {
            X = target.X - minus.X,
            Y = target.Y - minus.Y,
            Width = target.Width,
            Height = target.Height,
        };
        return result;
    }
}