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

                var i = 0;

                var files = Directory.GetFiles(KumaConstants.STORAGE_PATH).Where(p => p.EndsWith(KumaConstants.EXTENSION));
                foreach (var file in files)
                {
                    // TODO: 適当
                    if (i == 3)
                    {
                        var name = Path.GetFileNameWithoutExtension(file);
                        Debug.Log("name:" + name);

                        // ここからボタンを増やす？ちょっとUI考えないといけないけど、この名前のUIのプレビューとかが出せると超うれしいなあ、、
                        var bytes = File.ReadAllBytes(file);
                        var frameData = FigmaFrameData.Parser.ParseFrom(bytes);
                        Debug.Log("frameData:" + frameData.Identifier);

                        var reader = new Reader(frameData);
                        reader.Read();
                        EditorWindow.GetWindow<KumaUIElementWindow>();
                        break;
                    }
                    i++;
                }
            }
        }

    }

    public class Reader
    {
        private readonly FigmaFrameData frameData;

        public Reader(FigmaFrameData frameData)
        {
            this.frameData = frameData;
        }

        internal void Read()
        {
            Debug.Log("frameData:" + ReadFigmaRect(frameData.AbsRect));
            foreach (var child in frameData.Children)
            {
                Debug.Log("child:" + child.Id + " absRect:" + ReadFigmaRect(child.AbsRect));

                // コンテンツ独自のパラメータの取り出し
                switch (child.ContentCase)
                {
                    case FigmaContent.ContentOneofCase.Rectangle:
                        var rectangle = child.Rectangle;
                        break;
                    case FigmaContent.ContentOneofCase.Text:
                        var text = child.Text;
                        break;
                    case FigmaContent.ContentOneofCase.None:
                        break;

                }
            }
        }

        private string ReadFigmaRect(FigmaRect figmaRect)
        {
            return "x:" + figmaRect.X + " y:" + figmaRect.Y + " w:" + figmaRect.Width + " h:" + figmaRect.Height;
        }
    }
}