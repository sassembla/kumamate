using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

// figmaに記録されているフォント情報とUnity上でセットするフォント情報を紐づける箇所。
namespace Kumamate
{
    public class FontMapping : ScriptableObject
    {
        [SerializeField] private string[] fontEntries = new string[0];
        [SerializeField] private string[] fontAssetContainedObjectPaths = new string[0];

        public Dictionary<string, string> entryAndObjectPathDict = new Dictionary<string, string>();

        void OnEnable()
        {
            ReloadDict();
        }

        private void ReloadDict()
        {
            // 辞書を構築する
            entryAndObjectPathDict = new Dictionary<string, string>();
            for (var i = 0; i < fontEntries.Length; i++)
            {
                var fontEntry = fontEntries[i];

                if (i < fontAssetContainedObjectPaths.Length)
                {
                    entryAndObjectPathDict[fontEntry] = fontAssetContainedObjectPaths[i];
                    continue;
                }

                entryAndObjectPathDict[fontEntry] = string.Empty;
            }
        }

        public string GetFontEntryStr(string fontPostScriptName, string fontName, int fontWeight)
        {
            return fontPostScriptName + "_" + fontName + "_" + fontWeight;
        }

        public void AddEntry(string fontPostScriptName, string fontName, int fontWeight)
        {
            var currentEntries = fontEntries.ToList();
            var fontEntryStr = GetFontEntryStr(fontPostScriptName, fontName, fontWeight);

            // 既知ではない組み合わせが増えたので、エントリーの更新を行い、辞書も更新する。
            if (!currentEntries.Contains(fontEntryStr))
            {
                currentEntries.Add(fontEntryStr);
                fontEntries = currentEntries.ToArray();

                // 辞書の更新
                ReloadDict();
            }
        }

        private const string FontMappingPath = "Assets/Kumamate/Editor/Settings/FontMapping.asset";

        public static FontMapping Create()
        {
            var candidate = AssetDatabase.LoadAssetAtPath<FontMapping>(FontMappingPath);
            if (candidate != null)
            {
                return candidate;
            }

            // ないので作成
            var fontMapping = CreateInstance<FontMapping>();
            AssetDatabase.CreateAsset(fontMapping, FontMappingPath);

            // 反映
            AssetDatabase.Refresh();

            return fontMapping;
        }

        public void UpdateEntry(Dictionary<string, string> result)
        {
            // 更新を行う
            fontEntries = result.Keys.ToArray();
            fontAssetContainedObjectPaths = result.Values.ToArray();

            // 辞書を再構築
            ReloadDict();
        }

        public bool TryChooseFontInfo<T>(string fontPostScriptName, string fontName, int fontWeight, out T component) where T : Component
        {
            var entry = GetFontEntryStr(fontPostScriptName, fontName, fontWeight);

            // キー自体がない
            if (!entryAndObjectPathDict.ContainsKey(entry))
            {
                component = null;
                return false;
            }

            var valPath = entryAndObjectPathDict[entry];

            // valが空文字
            if (string.IsNullOrEmpty(valPath))
            {
                component = null;
                return false;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<T>(valPath);

            // prefabがmissing
            if (prefab == null)
            {
                component = null;
                return false;
            }

            component = prefab.GetComponent<T>();
            return true;
        }
    }

    [CustomEditor(typeof(FontMapping))]
    public class FontMappingInspector : Editor
    {
        private FontMapping fontMapping;

        private Dictionary<string, string> fontDict;

        void OnEnable()
        {
            fontMapping = (FontMapping)target;
            fontDict = fontMapping.entryAndObjectPathDict;
        }


        public override VisualElement CreateInspectorGUI()
        {
            var visualElement = new VisualElement();

            var scrollViewRoot = new ScrollView(ScrollViewMode.Vertical);
            scrollViewRoot.style.height = new StyleLength() { value = new Length(100f, LengthUnit.Percent) };

            visualElement.Add(scrollViewRoot);

            var keysAndValues = new VisualElement();

            foreach (var item in fontDict)
            {
                var fontEntry = item.Key;

                var lineElement = new VisualElement();
                lineElement.style.width = new StyleLength() { value = new Length(100f, LengthUnit.Percent) };
                lineElement.style.flexDirection = new StyleEnum<FlexDirection>() { value = FlexDirection.Row };
                lineElement.style.alignItems = new StyleEnum<Align>() { value = Align.Auto };
                lineElement.style.justifyContent = new StyleEnum<Justify>() { value = Justify.Center };
                {
                    // ラベル、参考になるTextElement参照(newボタン欲しいな)
                    var valVElement = new ObjectField();
                    valVElement.label = fontEntry;
                    valVElement.style.width = new StyleLength() { value = new Length(80f, LengthUnit.Percent) };
                    valVElement.objectType = typeof(GameObject);
                    // valVElement.RegisterValueChangedCallback() += go => { };// TODO: セットされた時のコールバック、そのうち解決しないと、外部から持って来れない。

                    // 値が存在すれば、対象のprefabが存在するはずなので読み出す。
                    if (!string.IsNullOrEmpty(item.Value))
                    {
                        // 対象が存在するはずなので、読み出しを試みる。
                        var prefabPath = item.Value;
                        var targetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                        if (targetPrefab != null)
                        {
                            valVElement.value = targetPrefab;
                        }
                        else
                        {
                            Debug.LogWarning("fontEntry:" + fontEntry + " losts attached prefab. located at:" + prefabPath + " is now missing.");
                        }
                    }

                    // 作成して追加するボタン
                    var newButton = new UnityEngine.UIElements.Button() { text = "+" };
                    newButton.style.width = new StyleLength() { value = new Length(10f, LengthUnit.Percent) };
                    newButton.clicked += () =>
                    {
                        var newGOBase = new GameObject(fontEntry);
                        var prefabPath = KumaConstants.FONTS_PATH + fontEntry + ".prefab";
                        {
                            // prefabを作成し、任意のtext componentをつけるのを可能にする。
                            var prefab = PrefabUtility.SaveAsPrefabAsset(newGOBase, prefabPath);
                            valVElement.value = prefab;
                        }
                        DestroyImmediate(newGOBase);

                        fontDict[fontEntry] = prefabPath;

                        // 追加されたパスを元に保持情報を更新
                        fontMapping.UpdateEntry(fontDict);
                    };

                    lineElement.Add(valVElement);
                    lineElement.Add(newButton);
                }
                keysAndValues.Add(lineElement);
            }

            scrollViewRoot.Add(keysAndValues);

            return visualElement;
        }
    }
}