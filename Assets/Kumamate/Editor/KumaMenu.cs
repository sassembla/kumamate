using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Kumamate
{
    public class KumaMenu : EditorWindow
    {

        [MenuItem("Window/Kumamate/Open")]
        static void Open()
        {
            var window = (KumaMenu)EditorWindow.GetWindow(typeof(KumaMenu));
            window.Show();
        }

        [SerializeField] private string figmaFileUrl;

        // TODO: そのうちUIElementsにする
        // private void OnEnable()
        // {
        //     this.rootVisualElement.Clear();
        // }

        // UI表示
        void OnGUI()
        {
            figmaFileUrl = EditorGUILayout.TextField("Figma Share URL", figmaFileUrl);

            // share urlの先のfigmaデータをdownloadし、frame単位でpbとして書き出す。
            if (GUILayout.Button("Get File Data From Share URL"))
            {
                KumaDocumentDownloader.StartDownload(figmaFileUrl);
            }

            // TODO: 保存済みのファイルからファイル名一覧を読み出す
            // ここからボタンを増やす？ちょっとUI考えないといけないけど、この名前のUIのプレビューとかが出せると超うれしいなあ、、まあ名前とかDLが終わったら即とかの方が体験が良さそう。
            if (GUILayout.Button("1番目のファイルを読む(テスト実装)"))
            {
                var currentCachedFileNames = new List<string>();// TODO: ここでは本来、リストを表示してユーザーがどの画面のレイアウトをしたいかを選ぶところにしたい。ところ。

                var i = 0;

                var files = Directory.GetFiles(KumaConstants.STORAGE_PATH).Where(p => p.EndsWith(KumaConstants.EXTENSION));
                foreach (var file in files)
                {
                    // TODO: 適当に限定的な要素をみる
                    if (i == 0)
                    {
                        var window = EditorWindow.GetWindow<KumaUILayoutTargetWindow>();
                        window.Setup(file);
                        break;
                    }
                    i++;
                }
            }
        }

    }


}