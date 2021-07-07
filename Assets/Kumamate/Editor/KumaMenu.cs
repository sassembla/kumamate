using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Kumamate
{
    public class KumaMenu : EditorWindow
    {

        [MenuItem("Window/Kumamate")]
        static void Open()
        {
            var window = (KumaMenu)EditorWindow.GetWindow(typeof(KumaMenu));
            window.Show();
        }

        private string figmaFileUrl = "https://www.figma.com/file/EkwynraOS5tfbAyBBbflGE/Untitled?node-id=1%3A2";// TODO: 消す

        // UI表示
        // TODO: ここでUIElementsを試そう
        void OnGUI()
        {
            figmaFileUrl = EditorGUILayout.TextField("Figma Share URL", figmaFileUrl);

            // share urlの先のfigmaデータをdownloadし、frame単位でpbとして書き出す。
            if (GUILayout.Button("Get File Data From Share URL"))
            {
                KumaDocumentDownloader.StartDownload(figmaFileUrl);
            }

            // TODO: 保存済みのファイルからファイル名一覧を読み出す
            if (GUILayout.Button("test read"))
            {
                var currentCachedFileNames = new List<string>();

                var files = Directory.GetFiles(KumaConstants.STORAGE_PATH).Where(p => p.EndsWith(KumaConstants.EXTENSION));
                // var fileCount = 
                foreach (var file in files)
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    Debug.Log("name:" + name);
                    // ここからボタンを増やす？ちょっとUI考えないといけないけど、この名前のUIのプレビューとかが出せると超うれしいなあ、、
                }
            }
        }
    }
}