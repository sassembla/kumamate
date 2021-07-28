using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using Google.Protobuf;
using Kumamate.MiniJSON;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Kumamate
{

    /*
        figmaへとkumamate appとしてアクセスし、次の手順でレイアウト情報を取得する。
        1. figmaからクライアントコードを取得
        2. 取得したクライアントコードを元に、figmaからaccessTokenを取得
        3. accessTokenと取得したいfigma file URLから、レイアウト情報を取得

        取得したものは保存しておいて、画面名から思い出しができるようにしておくと良さそう。
    */
    public class KumaDocumentDownloader
    {
        private const int port = 9888;

        private const string ClientID = "DfyB0kK2LiFeoa3cTyymAG";
        private const string ClientSecret = "dgDSVcJzxkULNG6jaY0D68Ehl7tJ7g";
        private const string RedirectURI = "https://sassembla.github.io/kumamate";

        private const string ClientCodeUrl = "https://www.figma.com/oauth?client_id={0}&redirect_uri={1}&scope=file_read&state={2}&response_type=code";
        private const string OAuthUrl = "https://www.figma.com/api/oauth/token?client_id={0}&client_secret={1}&redirect_uri={2}&code={3}&grant_type=authorization_code";

        private const string FileGetUrl = "https://api.figma.com/v1/files/{0}";
        private const string FileGetUrlWithNodeId = "https://api.figma.com/v1/files/{0}/nodes?ids={1}";

        private enum FigmaAccessState
        {
            None,
            GettingClientCode,
            GettingAccessToken,
            GettingFileInfo,
            Done,

            GettingClientCodeFailed,
            GettingAccessTokenFailed,
            GettingFileInfoFailed,
        }

        private static FigmaAccessState state = FigmaAccessState.None;

        public static void StartDownload(string figmaFileUrl)
        {
            switch (state)
            {
                case FigmaAccessState.None:
                case FigmaAccessState.Done:
                case FigmaAccessState.GettingClientCodeFailed:
                case FigmaAccessState.GettingAccessTokenFailed:
                case FigmaAccessState.GettingFileInfoFailed:
                    break;
                case FigmaAccessState.GettingAccessToken:
                    Debug.Log("アクセス中です。ブラウザでfigmaへのアクセスを許可するかどうかが出ているので、よく考えてから許可するか拒否してください。");
                    return;
                default:
                    Debug.LogError("アクセス中です。");
                    return;
            }

            // TODO: これは事前にどうするといいのかわかりそうなもんだよな、、ファイルキャッシュとかができればそれを元になんとかしたい。
            var (fileName, nodeId, apiUrl) = ParseFigmaShareURL(figmaFileUrl);

            // アクセスを開始する。
            StartEditorCoroutine(AccessToFigma(apiUrl));
        }


        // figma share URLからfileNameや取得用のAPI URLを取得する。
        private static (string fileName, string nodeId, string apiUrl) ParseFigmaShareURL(string shareUrl)
        {
            var substrings = shareUrl.Split('/');
            var length = substrings.Length;

            var _fileName = substrings[length - 2];

            // node-idを含むURLの場合、URLを切り替える。
            if (substrings[length - 1].Contains("node-id"))
            {
                var _nodeId = substrings[length - 1].Split(new string[] { "?node-id=" }, StringSplitOptions.RemoveEmptyEntries)[1];
                var fileWithNodeApiUrl = String.Format(FileGetUrlWithNodeId, _fileName, _nodeId);
                return (_fileName, _nodeId, fileWithNodeApiUrl);
            }

            var singleFileApiUrl = String.Format(FileGetUrl, _fileName);
            return (_fileName, string.Empty, singleFileApiUrl);
        }

        // figmaから取得したjsonからaccess_tokenを取り出すための内部型
        [Serializable]
        private class AccessTokenFromFigma
        {
            [SerializeField] public string access_token;
        }

        private static IEnumerator RequestAccessToFigma(string clientCode, Action<string> onAccessTokenReceived)
        {
            var form = new WWWForm();
            var request = String.Format(OAuthUrl, ClientID, ClientSecret, RedirectURI, clientCode);
            using (var req = UnityWebRequest.Post(request, form))
            {
                req.SendWebRequest();

                while (!req.isDone)
                {
                    yield return null;
                }

                if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.DataProcessingError)
                {
                    Debug.LogError("figmaからaccessTokenを取得するのに失敗した:" + req.error);
                    yield break;
                }

                if (req.responseCode == 200)
                {
                    var resultStr = req.downloadHandler.text;

                    // responseからaccess_tokenのみを取り出す。
                    var accessTokenFromFigma = JsonUtility.FromJson<AccessTokenFromFigma>(resultStr);
                    onAccessTokenReceived(accessTokenFromFigma.access_token);
                    yield break;
                }

                Debug.LogError("get access token request failed, responseCode:" + req.responseCode + " reason:" + req.downloadHandler.text);
            }
        }

        // figmaからファイル情報を取得する。
        private static IEnumerator GetFileInformationFromFigma(string accessToken, string apiURL, Action<string> onFileInfoReceived)
        {
            var form = new WWWForm();
            using (var req = UnityWebRequest.Get(apiURL))
            {
                req.SetRequestHeader("Authorization", $"Bearer {accessToken}");
                req.SendWebRequest();
                while (!req.isDone)
                {
                    yield return null;
                }

                if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.DataProcessingError)
                {
                    Debug.LogError("figmaからファイル情報を取得するのに失敗した:" + req.error);
                    yield break;
                }

                if (req.responseCode == 200)
                {
                    var result = req.downloadHandler.text;
                    onFileInfoReceived(result);
                    yield break;
                }

                Debug.LogError("get file request failed, responseCode:" + req.responseCode + " reason:" + req.downloadHandler.text);
            }
        }

        private static IEnumerator AccessToFigma(string apiUrl)
        {
            // ここでconnectionIdを作り、このIDがついているレスポンスを待ち受ける。
            var connectionId = UnityEngine.Random.Range(0, Int32.MaxValue).ToString();

            // サーバを開始して待つ
            state = FigmaAccessState.GettingClientCode;

            var clientCode = string.Empty;

            // clientCodeを取得する。
            {
                var formattedOauthUrl = String.Format(ClientCodeUrl, ClientID, RedirectURI, connectionId);

                // figmaをブラウザで開き、リクエストがリダイレクト先に到達するのを待つ。
                Application.OpenURL(formattedOauthUrl);

                var waitResponse = true;

                using (var server = new HTTPServerForGettingClientCode(
                    connectionId,
                    clientCodeStr =>
                    {
                        // クライアントコードを受け取ったので待ちを終了する。
                        clientCode = clientCodeStr;
                        waitResponse = false;
                    }
                ))
                {
                    // サーバの開始
                    server.Start("http://+:" + port + "/");

                    // レスポンスを受け取るか、最大3分待つ。
                    var startTime = DateTime.UtcNow;
                    while (waitResponse)
                    {
                        if (3 < (DateTime.UtcNow - startTime).TotalMinutes)
                        {
                            waitResponse = false;
                            state = FigmaAccessState.GettingAccessTokenFailed;
                            Debug.LogError("3分経過してもfigmaからClientCodeが取得できなかったため、Editor上のサーバを停止する。");
                            yield break;
                        }
                        yield return null;
                    }
                }

                if (string.IsNullOrEmpty(clientCode))
                {
                    state = FigmaAccessState.GettingAccessTokenFailed;
                    Debug.LogError("figmaから取得したレスポンスが異常だったため、Editor上のサーバを停止する。");
                    yield break;
                }
            }

            // accessTokenを取得する。
            state = FigmaAccessState.GettingAccessToken;
            var accessToken = string.Empty;
            {
                var access = RequestAccessToFigma(
                    clientCode,
                    token =>
                    {
                        accessToken = token;
                    }
                );

                while (access.MoveNext())
                {
                    yield return null;
                }

                if (string.IsNullOrEmpty(accessToken))
                {
                    state = FigmaAccessState.GettingAccessTokenFailed;
                    Debug.LogError("失敗4 accessTokenの取得に失敗した");
                    yield break;
                }
            }

            // ファイル情報を取得する。
            state = FigmaAccessState.GettingFileInfo;
            {
                var fileInfoJson = string.Empty;

                var access = GetFileInformationFromFigma(
                    accessToken,
                    apiUrl,
                    fileInfoStr =>
                    {
                        fileInfoJson = fileInfoStr;
                    }
                );

                while (access.MoveNext())
                {
                    yield return null;
                }

                if (string.IsNullOrEmpty(fileInfoJson))
                {
                    state = FigmaAccessState.GettingFileInfoFailed;
                    Debug.LogError("失敗5 ファイル情報の取得に失敗した");
                    yield break;
                }

                // 保存したものを名前で一覧読みできるといいな。そんでどうやって入力窓を作るか考えよう。
                WriteFigmaLayoutFile(fileInfoJson);
            }
            state = FigmaAccessState.Done;
        }



        // Editorで使えるCoroutineのStart関数
        private static void StartEditorCoroutine(IEnumerator cor)
        {
            EditorApplication.CallbackFunction coroutineAct = null;
            coroutineAct = () =>
            {
                if (!cor.MoveNext())
                {
                    EditorApplication.update -= coroutineAct;// 取り除く
                }
            };

            EditorApplication.update += coroutineAct;
        }

        private static void WriteFigmaLayoutFile(string jsonStr)
        {
            var root = Json.Deserialize(jsonStr) as Dictionary<string, object>;
            foreach (var item in root)
            {
                switch (item.Key)
                {
                    case "nodes":
                        var nodes = root["nodes"] as Dictionary<string, object>;
                        foreach (var node in nodes)
                        {
                            var nodeDict = node.Value as Dictionary<string, object>;
                            foreach (var nodeItem in nodeDict)
                            {
                                var nodeKey = nodeItem.Key;
                                switch (nodeKey)
                                {
                                    case "document":
                                        var docDict = nodeItem.Value as Dictionary<string, object>;
                                        var type = docDict["type"] as string;
                                        // 書き出しを行う。
                                        switch (type)
                                        {
                                            // このあたりは総じてframeとして扱う。figma上でも特に区別されているわけでもない。
                                            case "FRAME":
                                            case "COMPONENT":
                                                var topLevelFrame = FigmaFileDataReader.ReadFrame(docDict);
                                                var topLevelFrameFileName = topLevelFrame.Identifier;
                                                WriteToFile(topLevelFrame, topLevelFrameFileName);
                                                break;
                                            // canvas/groupは複数のframe類を内包することがあり、分解して各frameを起点とした座標系を持つようにしている。
                                            // そうしないとものすごい規模の座標値がくる。
                                            case "CANVAS":
                                            case "GROUP":
                                                var frames = FigmaFileDataReader.ReadCanvas(docDict);
                                                foreach (var frame in frames)
                                                {
                                                    var frameFileName = frame.Identifier;
                                                    WriteToFile(frame, frameFileName);
                                                }
                                                break;
                                            default:
                                                Debug.LogError("unsupported toplevel type:" + type);
                                                break;
                                        }

                                        break;
                                    case "components":
                                    case "schemaVersion":
                                    case "styles":
                                        // 要素を含まないため無視している
                                        break;
                                    default:
                                        Debug.LogError("unsupported nodeKey:" + nodeKey);
                                        break;
                                }
                            }
                        }
                        break;
                    default:
                        // Debug.LogError("unsupported type:" + item.Key);
                        break;
                }
            }
        }

        private static void WriteToFile(FigmaFrameData topLevelFrame, string topLevelFrameFileName)
        {
            // デバッグ用のjson出力
            // using (var sw = new StreamWriter("test"))
            // {
            //     sw.WriteLine(topLevelFrame.ToString());
            // }

            // ファイルとして出力する。
            var buffer = new byte[topLevelFrame.CalculateSize()];
            using (var output = new CodedOutputStream(buffer))
            {
                topLevelFrame.WriteTo(output);
                File.WriteAllBytes(KumaConstants.STORAGE_PATH + topLevelFrameFileName + KumaConstants.EXTENSION, buffer);
            }
        }
    }

    public class HTTPServerForGettingClientCode : IDisposable
    {
        private readonly string connectionId;
        private readonly Action<string> onClientCodeReceived;
        public HTTPServerForGettingClientCode(string connectionId, Action<string> onClientCodeReceived)
        {
            this.connectionId = connectionId;
            this.onClientCodeReceived = onClientCodeReceived;
        }

        private HttpListener listener;
        private bool disposedValue;

        public void Start(params string[] prefixes)
        {
            listener = new HttpListener();
            foreach (var prefix in prefixes)
            {
                listener.Prefixes.Add(prefix);
            }

            listener.Start();
            listener.BeginGetContext(OnRequested, null);
        }

        private void OnRequested(IAsyncResult ar)
        {
            if (!listener.IsListening)
            {
                return;
            }

            var context = listener.EndGetContext(ar);
            listener.BeginGetContext(OnRequested, listener);

            try
            {
                // get accessのみを受け付ける
                if (ProcessGetRequest(context))
                {
                    return;
                }
            }
            catch
            {
                // 何もしない
            }
        }

        private bool ProcessGetRequest(HttpListenerContext context)
        {
            var request = context.Request;
            if (request.HttpMethod == HttpMethod.Get.ToString())
            {
                // pass.
            }
            else
            {
                return false;
            }

            var url = request.Url.AbsoluteUri;

            // url末尾がconnectionIDと一致するのを期待する。
            if (url.EndsWith("state=" + connectionId))
            {
                // http://localhost:8080/?code=7rWbTAso632T0QPYyOo6jSUly&state=786217330
                var codeAndStateQuery = url.Split('?');
                if (codeAndStateQuery.Length != 2)
                {
                    Debug.LogError("失敗1 queryがない");
                    return false;
                }

                // code=7rWbTAso632T0QPYyOo6jSUly&state=786217330
                var queries = codeAndStateQuery[1].Split('&');
                if (codeAndStateQuery.Length != 2)
                {
                    Debug.LogError("失敗2 query数が足りない、必ず2つあるはず");
                    return false;
                }

                var keyAndValue = queries[0].Split('=');
                if (keyAndValue.Length != 2)
                {
                    Debug.LogError("失敗3 キーバリュー形式ではない文字列がきた");
                    return false;
                }

                var clientCode = keyAndValue[1];

                // clientCodeを返す
                onClientCodeReceived(clientCode);
            }

            using (var response = context.Response)
            {
                response.StatusCode = (int)HttpStatusCode.OK;
                var outputStream = response.OutputStream;

                // TODO: 閉じるボタンとかを実装したいが、できないのでは、、？
                var buffer = @"
<!DOCTYPE html>
<head>
</head>
<body onload='closeWindow()'>
	figma to UnityEditor data transfer is finished. please close this tab manually.
</body>
            ";
                var bufferBytes = Encoding.UTF8.GetBytes(buffer);
                outputStream.Write(bufferBytes, 0, bufferBytes.Length);
            }

            return true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // サーブをやめる
                    listener.Stop();
                    listener.Close();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}