using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/*
    figmaへとkumamate appとしてアクセスし、次の手順でレイアウト情報を取得する。
    1. figmaからクライアントコードを取得
    2. 取得したクライアントコードを元に、figmaからaccessTokenを取得
    3. accessTokenと取得したいfigma file URLから、レイアウト情報を取得

    取得したものは保存しておいて、画面名から思い出しができるようにしておくと良さそう。
*/
public class KumaDocumentDownloader : EditorWindow
{
    private const int port = 9888;
    private static KumaSetting setting;
    private const string settingPath = "Assets/Kumamate/Editor/Storage/setting.json";


    private const string ClientID = "DfyB0kK2LiFeoa3cTyymAG";
    private const string ClientSecret = "dgDSVcJzxkULNG6jaY0D68Ehl7tJ7g";
    private const string RedirectURI = "https://sassembla.github.io/kumamate";

    private const string OAuthUrl = "https://www.figma.com/api/oauth/token?client_id={0}&client_secret={1}&redirect_uri={2}&code={3}&grant_type=authorization_code";
    private const string ClientCodeUrl = "https://www.figma.com/oauth?client_id={0}&redirect_uri={1}&scope=file_read&state={2}&response_type=code";


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

    [MenuItem("Window/Kumamate")]
    static void Open()
    {
        var window = (KumaDocumentDownloader)EditorWindow.GetWindow(typeof(KumaDocumentDownloader));
        window.Show();

        // セッティングのロードを行う
        setting = LoadSetting();
    }

    private static KumaSetting LoadSetting()
    {
        if (!File.Exists(settingPath))
        {
            return new KumaSetting();
        }

        var jsonStr = File.ReadAllText(settingPath);
        return JsonUtility.FromJson<KumaSetting>(jsonStr);
    }

    // UI表示
    void OnGUI()
    {
        if (GUILayout.Button("Get File Data From Figma"))
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

            // リクエストを開始する
            // TODO: tokenの期限切れとかを加味できればもっとゆるくてもいいのかもしれないけど、すごい勢いでタイムアウトするので、毎回clientCode取得してもいいかもしれない。
            OpenFigmaThenRequestGrant();
        }
    }

    private void OpenFigmaThenRequestGrant()
    {
        // ここでconnectionIdを作り、このIDがついているレスポンスを待ち受ける。
        var connectionId = UnityEngine.Random.Range(0, Int32.MaxValue).ToString();
        var formattedOauthUrl = String.Format(ClientCodeUrl, ClientID, RedirectURI, connectionId);

        // figmaをブラウザで開き、リクエストがリダイレクト先に到達するのを待つ。
        Application.OpenURL(formattedOauthUrl);

        // サーバを開始して待つ
        StartReceiving(connectionId, "https://www.figma.com/file/EkwynraOS5tfbAyBBbflGE/Untitled?node-id=1%3A2");
    }

    [Serializable]
    private class AccessTokenFromFigma
    {
        [SerializeField] public string access_token;
    }

    private IEnumerator RequestAccessToFigma(string clientCode, Action<string> onAccessTokenReceived)
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
            }
        }
    }

    // figmaからファイル情報を取得する。
    private IEnumerator GetFileInformationFromFigma(string accessToken, string fileUrl, Action<string> onFileInfoReceived)
    {
        // URLに応じてURLを調整する。
        var apiURL = string.Empty;
        {
            var substrings = fileUrl.Split('/');
            var length = substrings.Length;

            // node-idを含むURLの場合、URLを切り替える。
            bool isNodeUrl = substrings[length - 1].Contains("node-id");

            var _fileName = substrings[length - 2];

            if (isNodeUrl)
            {
                var _nodeId = substrings[length - 1].Split(new string[] { "?node-id=" }, StringSplitOptions.RemoveEmptyEntries)[1];
                apiURL = $"https://api.figma.com/v1/files/{_fileName}/nodes?ids={_nodeId}";
            }
            else
            {
                apiURL = $"https://api.figma.com/v1/files/{_fileName}";
            }
        }

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
            }
        }
    }

    // figmaからファイル情報を取得するまで一連の処理を行う。
    private void StartReceiving(string connectionId, string targetFileUrl)
    {
        state = FigmaAccessState.GettingClientCode;

        IEnumerator accessToFigma()
        {
            var clientCode = string.Empty;

            // clientCodeを取得する。
            {
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
                    targetFileUrl,
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

                // TODO: fileUrlに対して情報が取得できたので、ローカルに保存する。
                Debug.Log("fileInfoJson:" + fileInfoJson);
            }
        }

        // アクセスを開始する。
        StartEditorCoroutine(accessToFigma());
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

    private static HTTPServerForGettingClientCode server;
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