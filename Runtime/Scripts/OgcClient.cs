using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using UnityEngine.Networking;

namespace Net.OpenGameCloud {

    public class OgcClient : MonoBehaviour {
        private static OgcClient _instance;

        public static AuthModule Auth => _instance._auth;
        public static SessionsModule Sessions => _instance._sessions;
        public static HighScoresModule HighScores => _instance._highscores;
        public static PlayerDataModule PlayerData => _instance._savegames;

        [Header("Host")]
        public string Host = "https://api.opengamecloud.com";
        public string WebsocketUrl = "wss://api.opengamecloud.com/ws";

        [Header("Project Credentials")]
        public string ApiKey = "<REPLACE WITH API KEY>";
        public string ProjectId = "<REPLACE WITH PROJECT ID>";

        internal string UserToken { get; set; }

        private AuthModule _auth;
        private SessionsModule _sessions;
        private HighScoresModule _highscores;
        private PlayerDataModule _savegames;

        private void Awake() {
            if (_instance != null) {
                Destroy(gameObject);
                Debug.LogError("Only one instance of OgcClient is allowed!");
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            _auth = new AuthModule(this);
            _sessions = new SessionsModule(this);
            _highscores = new HighScoresModule(this);
            _savegames = new PlayerDataModule(this);
        }

        private void Update() => _sessions.DispatchMessages();

        private async void OnApplicationQuit() => await _sessions.Disconnect();

        internal async Task<string> HttpRequest(string method, string url, object body = null) {
            using var req = new UnityWebRequest(url, method);
            if (body != null) {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body)));
                req.SetRequestHeader("Content-Type", "application/json");
            }
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("X-API-Key", ApiKey);
            if (UserToken != null)
                req.SetRequestHeader("Authorization", $"Bearer {UserToken}");

            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success) {
                Debug.LogError($"[OgcClient] {method} {url} failed: {req.error}\n{req.downloadHandler?.text}");
                return null;
            }
            return req.downloadHandler.text;
        }
    }
}
