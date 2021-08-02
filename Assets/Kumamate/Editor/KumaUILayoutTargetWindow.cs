using System.IO;
using Kumamate;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

// TODO: 実験中のクラス、最終的にはUIの生成を助けるロケーター穴の生成を大量に行い、セットアップして参照されているオブジェクトのあらゆる値を更新する。
public class KumaUILayoutTargetWindow : EditorWindow
{
    private string filePath;

    private FontMapping fontMapping;

    // TODO: 変動できるようにする。コンパイル可能なものを書き換える方が正直いいのではと言うのはある。
    private const float OUTPUT_RATIO = 3f;// 出力値倍率 レイアウトのパラメータを画面に対してレイアウトするときの倍率。
    private const float DRAW_RATIO = 1.0f;// 描画倍率 レイアウトをウィンドウ内に描画するときの倍率。

    public void OnEnable()
    {
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
        var rootArea = new VisualElement();
        rootArea.style.width = new StyleLength() { value = new Length(frameData.AbsRect.Width * DRAW_RATIO, LengthUnit.Pixel) };
        rootArea.style.height = new StyleLength() { value = new Length(frameData.AbsRect.Height * DRAW_RATIO, LengthUnit.Pixel) };

        scrollViewRoot.Add(rootArea);

        var baseRect = new FigmaRect()
        {
            X = 0,
            Y = 0,
            Width = 0,
            Height = 0
        };

        // childのレイアウト + 未知のフォント情報の収集を行う
        foreach (var child in frameData.Children)
        {
            DoLayout(rootArea, baseRect, child, 1);
        }
    }

    // マウスオーバーしている範囲で、最も深度が深い = 手前にあるオブジェクトにフォーカスが行くように規定している。
    private static int currentDeepestDepth;

    private void DoLayout(VisualElement parent, FigmaRect parentRect, FigmaContent child, int depth)
    {
        var absPos = child.AbsRect.Mul(DRAW_RATIO);
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

                // D&Dでdropする時のアクションを書いている。
                // TODO: 綺麗に落ちないため、色々調整したい。
                currentArea.RegisterCallback<DragEnterEvent>(
                    a =>
                    {
                        Selection.activeObject = DragAndDrop.objectReferences[0];
                        // 選択中としてカラーを変更する
                        currentArea.style.backgroundColor = new StyleColor() { value = new Color(currentArea.style.color.value.r + 0.3f, currentArea.style.color.value.g + 0.3f, currentArea.style.color.value.b + 0.3f, currentArea.style.color.value.a + 0.1f) };

                        // 最も深い深度まで、depth情報を更新する。
                        if (depth < currentDeepestDepth)
                        {
                            return;
                        }
                        currentDeepestDepth = depth;
                    }
                );
                currentArea.RegisterCallback<DragLeaveEvent>(
                    _ =>
                    {
                        // 選択が終わったのでカラーを戻す
                        currentArea.style.backgroundColor = currentArea.style.color;
                        currentDeepestDepth = -1;
                    }
                );
                currentArea.RegisterCallback<DragExitedEvent>(
                    _ =>
                    {
                        // 現在D&D状態のmouseがオーバーレイしている一番深いレイヤーに対して処理を行う。
                        if (currentDeepestDepth == depth)
                        {
                            // 現在D&D処理中のオブジェクトに対して、レイアウト処理を行う。
                            foreach (var dropedObjRef in DragAndDrop.objectReferences)
                            {
                                // レイアウトを合わせる
                                if (dropedObjRef is GameObject go)
                                {
                                    AdoptLayout(go, child);

                                    // objField.value = go;// これは効くが、特にやらない方が良さそう。

                                    // D&Dが終わったので対象のオブジェクトを選択状態にする。
                                    // TODO: というのがしたいんだが、一定時間したら勝手に選択が戻るの何、、、D&DのOnEndがありそうだな〜。 ちゃんとD&D実装すると良さそう。
                                    // Selection.objects = new UnityEngine.Object[] { go };// これでもダメ
                                    // Selection.activeTransform = go.transform;
                                    // Selection.activeInstanceID = go.GetInstanceID();
                                }
                            }
                        }

                        // 処理が終わったのでカラーを戻す
                        currentArea.style.backgroundColor = currentArea.style.color;
                    }
                );

                // テキストだったらマップを作る
                if (child.Type == "TEXT")
                {
                    if (fontMapping == null)
                    {
                        fontMapping = FontMapping.Create();
                    }
                    var textContent = child.Text;
                    fontMapping.AddEntry(textContent.FontPostScriptName, textContent.FontName, textContent.FontWeight);
                }

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

            // TODO: ここでアンカーを一瞬解除し、戻す、、ということがしたいが、そのままやると座標系が狂う。変換 -> 編集が1fだとまずいらしい。うーん、、まあ頑張って計算するしかない。またか。
            var baseAnchor = (rectTrans.anchorMin, rectTrans.anchorMax, rectTrans.pivot);

            rectTrans.anchorMin = new Vector2(0, 1);
            rectTrans.anchorMax = new Vector2(0, 1);
            rectTrans.pivot = new Vector2(0, 1);
            rectTrans.anchoredPosition = new Vector2(absPos.X * OUTPUT_RATIO, absPos.Y * OUTPUT_RATIO);
            rectTrans.sizeDelta = new Vector2(absPos.Width * OUTPUT_RATIO, absPos.Height * OUTPUT_RATIO);

            rectTrans.anchorMin = baseAnchor.anchorMin;
            rectTrans.anchorMax = baseAnchor.anchorMax;
            rectTrans.pivot = baseAnchor.pivot;
        }

        // コンテンツごとのパラメータ入力を行う
        switch (content.Type)
        {
            case "TEXT":
                var textContent = content.Text;

                if (target.TryGetComponent<Text>(out var textComponent))
                {
                    textComponent.text = textContent.Characters;
                    textComponent.color = new Color(textContent.R, textContent.G, textContent.B, textContent.A);
                    textComponent.fontSize = (int)(textContent.FontSize * OUTPUT_RATIO);

                    // styleやfont weightなどの情報はcomponentから引き出す。
                    if (fontMapping.TryChooseFontInfo<Text>(textContent.FontPostScriptName, textContent.FontName, textContent.FontWeight, out var mappedTextComponent))
                    {
                        textComponent.font = mappedTextComponent.font;
                        textComponent.fontStyle = mappedTextComponent.fontStyle;
                    }
                    else
                    {
                        Debug.LogWarning("フォントエントリー:" + fontMapping.GetFontEntryStr(textContent.FontPostScriptName, textContent.FontName, textContent.FontWeight) + " が初期状態のまま存在している。FontMapping.assetを編集すると、次回以降のセットに対して反応するようになる。");
                    }
                }
                else if (target.TryGetComponent<TMP_Text>(out var tmpTextComponent))
                {
                    tmpTextComponent.text = textContent.Characters;
                    tmpTextComponent.color = new Color(textContent.R, textContent.G, textContent.B, textContent.A);
                    tmpTextComponent.fontSize = textContent.FontSize * OUTPUT_RATIO;

                    // styleやfont weightなどの情報はcomponentから引き出す。
                    if (fontMapping.TryChooseFontInfo<TMP_Text>(textContent.FontPostScriptName, textContent.FontName, textContent.FontWeight, out var mappedTmpComponent))
                    {
                        tmpTextComponent.font = mappedTmpComponent.font;
                        tmpTextComponent.fontStyle = mappedTmpComponent.fontStyle;
                    }
                    else
                    {
                        Debug.LogWarning("フォントエントリー:" + fontMapping.GetFontEntryStr(textContent.FontPostScriptName, textContent.FontName, textContent.FontWeight) + " が初期状態のまま存在している。FontMapping.assetを編集すると、次回以降のセットに対して反応するようになる。");
                    }
                }
                break;
            case "ELLIPSE":
            case "FRAME":
            case "RECTANGLE":
            case "INSTANCE":
                break;
            default:
                Debug.LogError("未知のタイプ:" + content.Type);
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
    public static FigmaRect Mul(this FigmaRect target, float ratio)
    {
        var result = new FigmaRect()
        {
            X = target.X * ratio,
            Y = target.Y * ratio,
            Width = target.Width * ratio,
            Height = target.Height * ratio,
        };

        return result;
    }

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