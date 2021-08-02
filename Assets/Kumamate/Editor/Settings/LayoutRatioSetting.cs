using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kumamate
{
    public class LayoutRatioSetting : ScriptableObject
    {
        [SerializeField] public float OUTPUT_RATIO = 3f;// 出力値倍率 レイアウトのパラメータを画面に対してレイアウトするときの倍率。
        [SerializeField] public float DRAW_RATIO = 1.0f;// 描画倍率 レイアウトをウィンドウ内に描画するときの倍率。

        void OnEnable()
        {
            Reload();
        }

        private void Reload()
        {
            // TODO: 後で考える
        }

        private const string LayoutRatioSettingPath = "Assets/Kumamate/Editor/Settings/LayoutRatioSetting.asset";

        public static LayoutRatioSetting Create()
        {
            var candidate = AssetDatabase.LoadAssetAtPath<LayoutRatioSetting>(LayoutRatioSettingPath);
            if (candidate != null)
            {
                return candidate;
            }

            // ないので作成
            var layoutRatioSetting = CreateInstance<LayoutRatioSetting>();
            AssetDatabase.CreateAsset(layoutRatioSetting, LayoutRatioSettingPath);

            // 反映
            AssetDatabase.Refresh();

            return layoutRatioSetting;
        }
    }

    [CustomEditor(typeof(LayoutRatioSetting))]
    public class LayoutRatioSettingInspector : Editor
    {
        private LayoutRatioSetting layoutRatio;

        private float outputRatio;
        private float drawRatio;

        void OnEnable()
        {
            layoutRatio = (LayoutRatioSetting)target;
            outputRatio = layoutRatio.OUTPUT_RATIO;
            drawRatio = layoutRatio.DRAW_RATIO;
        }


        public override VisualElement CreateInspectorGUI()
        {
            var visualElement = new VisualElement();

            var scrollViewRoot = new ScrollView(ScrollViewMode.Vertical);
            scrollViewRoot.style.height = new StyleLength() { value = new Length(100f, LengthUnit.Percent) };

            visualElement.Add(scrollViewRoot);

            var keysAndValues = new VisualElement();

            // foreach (var item in fontDict)
            // {
            //     var fontEntry = item.Key;

            //     var lineElement = new VisualElement();
            //     lineElement.style.width = new StyleLength() { value = new Length(100f, LengthUnit.Percent) };
            //     lineElement.style.flexDirection = new StyleEnum<FlexDirection>() { value = FlexDirection.Row };
            //     lineElement.style.alignItems = new StyleEnum<Align>() { value = Align.Auto };
            //     lineElement.style.justifyContent = new StyleEnum<Justify>() { value = Justify.Center };
            //     {
            //         // ラベル、参考になるTextElement参照(newボタン欲しいな)
            //         var valVElement = new ObjectField();
            //         valVElement.label = fontEntry;
            //         valVElement.style.width = new StyleLength() { value = new Length(80f, LengthUnit.Percent) };
            //         valVElement.objectType = typeof(GameObject);
            //         // valVElement.RegisterValueChangedCallback() += go => { };// TODO: セットされた時のコールバック、そのうち解決しないと、外部から持って来れない。

            //         // 値が存在すれば、対象のprefabが存在するはずなので読み出す。
            //         if (!string.IsNullOrEmpty(item.Value))
            //         {
            //             // 対象が存在するはずなので、読み出しを試みる。
            //             var prefabPath = item.Value;
            //             var targetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            //             if (targetPrefab != null)
            //             {
            //                 valVElement.value = targetPrefab;
            //             }
            //             else
            //             {
            //                 Debug.LogWarning("fontEntry:" + fontEntry + " losts attached prefab. located at:" + prefabPath + " is now missing.");
            //             }
            //         }

            //         // 作成して追加するボタン
            //         var newButton = new UnityEngine.UIElements.Button() { text = "+" };
            //         newButton.style.width = new StyleLength() { value = new Length(10f, LengthUnit.Percent) };
            //         newButton.clicked += () =>
            //         {
            //             var newGOBase = new GameObject(fontEntry);
            //             if (!Directory.Exists(KumaConstants.FONTS_PATH))
            //             {
            //                 Directory.CreateDirectory(KumaConstants.FONTS_PATH);
            //             }

            //             var prefabPath = KumaConstants.FONTS_PATH + fontEntry + ".prefab";
            //             {
            //                 // prefabを作成し、任意のtext componentをつけるのを可能にする。
            //                 var prefab = PrefabUtility.SaveAsPrefabAsset(newGOBase, prefabPath);
            //                 valVElement.value = prefab;
            //             }
            //             DestroyImmediate(newGOBase);

            //             fontDict[fontEntry] = prefabPath;

            //             // 追加されたパスを元に保持情報を更新
            //             layoutRatio.UpdateEntry(fontDict);
            //         };

            //         lineElement.Add(valVElement);
            //         lineElement.Add(newButton);
            //     }
            //     keysAndValues.Add(lineElement);
            // }

            scrollViewRoot.Add(keysAndValues);

            return visualElement;
        }
    }
}