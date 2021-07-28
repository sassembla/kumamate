using System;
using System.IO;
using Kumamate;
using TMPro;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UI;
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

    private static int currentDepth;

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

        switch (child.Type)
        {
            case "INSTANCE":
            case "FRAME":
            case "TEXT":
            case "RECTANGLE":
            case "ELLIPSE":
                // カラーはランダムにしてるが、困ったら変えよう。
                currentArea.style.color = new StyleColor() { value = new Color(UnityEngine.Random.Range(0f, 1f), ((byte)UnityEngine.Random.Range(0f, 1f)), UnityEngine.Random.Range(0f, 1f), 0.1f * depth) };
                currentArea.style.backgroundColor = currentArea.style.color;


                // var label = new Label("type:" + child.Type);
                // currentArea.Add(label);// TODO: 適当につけてる、ここが変わると良さそう

                // currentArea.RegisterCallback<DragPerformEvent>(a =>
                // {
                //     Debug.Log("a:" + a);// これだけ呼ばれないとか？ 呼ばれねえ-w なるほどどうして？ -> これはdrag「された側」が持ってないといけないコードだ、たぶん。
                // });

                // オブジェクトフィールド、あんまり使い心地がよくなかった。
                // var objField = new ObjectField();
                // objField.objectType = typeof(GameObject);
                // objField.SetEnabled(true);
                // objField.RegisterCallback<ChangeEvent<GameObject>>(changedObject =>
                // {
                //     Debug.Log("changedObject:" + changedObject);
                // });
                // objField.style.width = currentArea.style.width;
                // objField.style.height = currentArea.style.height;
                // // objField.style.backgroundColor = new StyleColor() { value = new Color(0f, 0f, 0f, 0f) };
                // objField.style.backgroundImage = new StyleBackground() { value = new Background() };
                // // objField.
                // currentArea.Add(objField);


                currentArea.RegisterCallback<DragEnterEvent>(
                    a =>
                    {
                        // 選択中としてカラーを変更する
                        currentArea.style.backgroundColor = new StyleColor() { value = new Color(currentArea.style.color.value.r + 0.3f, currentArea.style.color.value.g + 0.3f, currentArea.style.color.value.b + 0.3f, currentArea.style.color.value.a + 0.1f) };

                        // 最も深い深度まで、depth情報を更新する。
                        if (depth < currentDepth)
                        {
                            return;
                        }
                        currentDepth = depth;
                    }
                );
                currentArea.RegisterCallback<DragLeaveEvent>(
                    a =>
                    {
                        // 選択が終わったのでカラーを戻す
                        currentArea.style.backgroundColor = currentArea.style.color;
                        currentDepth = -1;
                    }
                );
                currentArea.RegisterCallback<DragExitedEvent>(
                    a =>
                    {
                        Debug.Log("here:" + DragAndDrop.objectReferences.Length + " current:" + a.target + " child:" + child.Id);// ここがレイヤー数分呼ばれるってことか。縦に貫通してるんだな。ってことは、一番上じゃないとダメ、ってやんなきゃだね。depthで上書きしよう。
                        foreach (var dropedObjRef in DragAndDrop.objectReferences)
                        {
                            // レイアウトを合わせる
                            if (dropedObjRef is GameObject go && currentDepth == depth)
                            {
                                AdoptLayout(go, child);

                                // objField.value = go;// これは効くが、特にやらない方が良さそう。

                                // D&Dが終わったので対象のオブジェクトを選択状態にする。
                                // TODO: というのがしたいんだが、戻るの何、、、D&DのOnEndがありそうだな〜。
                                // Selection.objects = new UnityEngine.Object[] { go };// これでもダメ
                                // Selection.activeTransform = go.transform;
                                // Selection.activeInstanceID = go.GetInstanceID();
                            }
                        }

                        // カラーを戻す
                        currentArea.style.backgroundColor = currentArea.style.color;
                    }
                );

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

    // contentのレイアウトなどの情報をtargetに対して適応する。
    private void AdoptLayout(GameObject target, FigmaContent content)
    {
        // 位置系は全てのオブジェクトで設定する。
        if (target.TryGetComponent<RectTransform>(out var rectTrans))
        {
            var absPos = content.AbsRect;

            // TODO: ここでアンカーを一瞬解除し、戻す、、ということがしたいが、うーん、、まあ頑張って計算するしかない。またか。
            var baseAnchor = (rectTrans.anchorMin, rectTrans.anchorMax, rectTrans.pivot);

            rectTrans.anchorMin = new Vector2(0, 1);
            rectTrans.anchorMax = new Vector2(0, 1);
            rectTrans.pivot = new Vector2(0, 1);
            rectTrans.anchoredPosition = new Vector2(absPos.X * 3, absPos.Y * 3);
            rectTrans.sizeDelta = new Vector2(absPos.Width * 3, absPos.Height * 3);

            // rectTrans.anchorMin = baseAnchor.anchorMin;
            // rectTrans.anchorMax = baseAnchor.anchorMax;
            // rectTrans.pivot = baseAnchor.pivot;
        }

        switch (content.Type)
        {
            case "TEXT":
                var textContent = content.Text;
                if (target.TryGetComponent<Text>(out var textComponent))
                {
                    textComponent.text = textContent.Characters;
                    textComponent.color = new Color(textContent.R, textContent.G, textContent.B, textContent.A);
                    textComponent.fontSize = textContent.FontSize * 3;
                    // TODO: フォントファミリーをどうするかなー、、
                }
                else if (target.TryGetComponent<TMP_Text>(out var tmpTextComponent))
                {
                    tmpTextComponent.text = textContent.Characters;
                    tmpTextComponent.color = new Color(textContent.R, textContent.G, textContent.B, textContent.A);
                    tmpTextComponent.fontSize = textContent.FontSize * 3;
                    // TODO: フォントファミリーをどうするかなー、、
                }
                break;
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