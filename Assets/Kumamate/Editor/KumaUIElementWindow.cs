using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

// TODO: 実験中のクラス、最終的にはUIの生成を助けるロケーター穴の生成を大量に行い、セットアップして参照されているオブジェクトのあらゆる値を更新する。
public class KumaUIElementWindow : EditorWindow
{
    public void OnEnable()
    {
        // とりあえずロードしてみる -> うまくいく、よかった
        var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/a.uxml");
        rootVisualElement.Add(tree.CloneTree());

        // 読み込んだ物体の中身を探索して名前やハンドラを入れていくことは可能っぽい。
        foreach (var child in rootVisualElement.Children())
        {
            Debug.Log("child:" + child);
        }


    }

    void Something()
    {
        // 一番外側のスクロール要素を出す、これをつけないと
        var scrollViewRoot = new ScrollView(ScrollViewMode.Vertical);
        scrollViewRoot.style.height = new StyleLength() { value = new Length(100f, LengthUnit.Percent) };

        this.rootVisualElement.Add(scrollViewRoot);

        var rootArea = new VisualElement();
        scrollViewRoot.Add(rootArea);

        var leftArea = new VisualElement();
        rootArea.Add(leftArea);
        leftArea.style.width = new StyleLength() { value = new Length(50f, LengthUnit.Percent) };
        leftArea.Add(new TextField("Tex"));
        leftArea.Add(new MinMaxSlider("MMSlid"));
        leftArea.Add(new Button() { text = "BBBB" });
        leftArea.Add(new Button() { text = "BBBB" });
        leftArea.Add(new Button() { text = "BBBB" });
        leftArea.Add(new Button() { text = "BBBB" });
        leftArea.Add(new Button() { text = "BBBB" });
        leftArea.Add(new Button() { text = "BBBB" });
        leftArea.Add(new Button() { text = "BBBB" });
        leftArea.Add(new Button() { text = "BBBB" });
        leftArea.Add(new Button() { text = "BBBB" });
        leftArea.Add(new Button() { text = "BBBB" });
        leftArea.Add(new Button() { text = "BBBB" });
        leftArea.Add(new Button() { text = "BBBB" });
        leftArea.Add(new Button() { text = "BBBB" });
        leftArea.Add(new Button() { text = "BBBB" });
        leftArea.Add(new Button() { text = "BBBB" });
        leftArea.Add(new Button() { text = "BBBB" });
        leftArea.Add(new Button() { text = "BBBB" });
        leftArea.Add(new Button() { text = "BBBB" });
        leftArea.Add(new Button() { text = "BBBB" });


        var rightArea = new VisualElement();
        rootArea.Add(rightArea);
        rightArea.style.position = new StyleEnum<Position>() { value = Position.Absolute };// うーん、3つ並べてえいって感じじゃないのかな、、
        rightArea.style.left = new StyleLength() { value = new Length(50, LengthUnit.Percent) };// アンカリングをコードでやるのやめてくれ、、せめてもっと綺麗にセットさせてくれ、、、
        rightArea.style.width = new StyleLength() { value = new Length(50, LengthUnit.Percent) };
        rightArea.style.height = new StyleLength() { value = new Length(100, LengthUnit.Percent) };

        var scrollView = new ScrollView(ScrollViewMode.Vertical);
        rightArea.Add(scrollView);
        scrollView.style.height = new StyleLength() { value = new Length(100f, LengthUnit.Percent) };// ここでスクロールビュー高さが決まっていた
        for (var i = 0; i < 100; i++)
        {
            scrollView.Add(new Label("label:" + i));
        }
    }
}