using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Threading.Tasks;
using NativeWebSocket;
using UnityEngine;

namespace Net.OpenGameCloud {

    public class SessionsModule {
        private readonly OgcClient _client;
        private WebSocket _webSocket;
        private string _sessionId;
        private int _lastEventId;
        private const int PingDelay = 30000;

        public event Action<WsJoinedSessionMessage> OnJoinedSession;
        public event Action<WsPlayerJoinedMessage> OnPlayerJoin;
        public event Action<WsPlayerLeftMessage> OnPlayerLeft;
        public event Action<WsSessionEventMessage> OnSessionEvent;
        public event Action<WsMetadataUpdatedMessage> OnMetadataUpdated;
        public event Action<string> OnError;

        // Return current session state when the server requests a state sync for a joining player.
        public Func<object> GetCurrentSessionData;

        internal SessionsModule(OgcClient client) => _client = client;

        public async Task<SessionData> Create(string label = null, object metadata = null, string password = null) {
            var url = $"{_client.Host}/projects/{_client.ProjectId}/sessions";
            var json = await _client.HttpRequest("POST", url, new { label, metadata, password });
            return json != null ? JsonConvert.DeserializeObject<SessionData>(json) : null;
        }

        public async Task<SessionData[]> List() {
            var url = $"{_client.Host}/projects/{_client.ProjectId}/sessions";
            var json = await _client.HttpRequest("GET", url);
            if (json == null) return null;
            return JObject.Parse(json)["items"]?.ToObject<SessionData[]>();
        }

        public async Task<SessionData> QuickJoin(object metadata = null) {
            var url = $"{_client.Host}/projects/{_client.ProjectId}/sessions/quickjoin";
            var json = await _client.HttpRequest("POST", url, new { metadata });
            return json != null ? JsonConvert.DeserializeObject<SessionData>(json) : null;
        }

        public async Task Connect(string sessionId, object metadata = null, string password = null) {
            _sessionId = sessionId;
            _webSocket = new WebSocket(_client.WebsocketUrl);

            _webSocket.OnOpen += async () => {
                Debug.Log("[OpenGameCloud] WebSocket connected");
                await _webSocket.SendText(JsonConvert.SerializeObject(
                    new WsJoinSessionMessage(_client.ProjectId, sessionId, _client.ApiKey, _client.UserToken, metadata, password)
                ));
            };

            _webSocket.OnMessage += bytes => ParseMessage(Encoding.UTF8.GetString(bytes));
            _webSocket.OnError += error => Debug.LogError($"[OpenGameCloud] WebSocket error: {error}");
            _webSocket.OnClose += code => Debug.Log($"[OpenGameCloud] WebSocket closed: {code}");

            await _webSocket.Connect();
        }

        public void SendEvent(string eventType, object payload = null, string[] targetUserIds = null) {
            if (!IsConnected) return;
            _ = _webSocket.SendText(JsonConvert.SerializeObject(
                new WsSendEventMessage(_client.ProjectId, _sessionId, eventType, payload, targetUserIds)));
        }

        public void UpdateSessionData(object data) {
            if (!IsConnected) return;
            _ = _webSocket.SendText(JsonConvert.SerializeObject(
                new WsUpdateSessionDataMessage(_client.ProjectId, _sessionId, data, _lastEventId)));
        }

        public void UpdateMetadata(object metadata) {
            if (!IsConnected) return;
            _ = _webSocket.SendText(JsonConvert.SerializeObject(
                new WsUpdateMetadataMessage(_client.ProjectId, _sessionId, metadata)));
        }

        public async Task Disconnect() {
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                await _webSocket.Close();
        }

        public bool IsConnected => _webSocket != null && _webSocket.State == WebSocketState.Open;

        internal void DispatchMessages() {
#if !UNITY_WEBGL || UNITY_EDITOR
            _webSocket?.DispatchMessageQueue();
#endif
        }

        private void ParseMessage(string json) {
            var msg = JsonConvert.DeserializeObject<WsMessage>(json);
            switch (msg.type) {
                case "session_ready":
                    var joined = JsonConvert.DeserializeObject<WsJoinedSessionMessage>(json);
                    _lastEventId = joined.fromEventId;
                    OnJoinedSession?.Invoke(joined);
                    _ = KeepAlive();
                    break;
                case "request_state":
                    var stateData = GetCurrentSessionData?.Invoke() ?? new { };
                    _ = _webSocket.SendText(JsonConvert.SerializeObject(
                        new WsUpdateSessionDataMessage(_client.ProjectId, _sessionId, stateData, _lastEventId)));
                    break;
                case "user_joined":
                    OnPlayerJoin?.Invoke(JsonConvert.DeserializeObject<WsPlayerJoinedMessage>(json));
                    break;
                case "user_left":
                    OnPlayerLeft?.Invoke(JsonConvert.DeserializeObject<WsPlayerLeftMessage>(json));
                    break;
                case "session_event":
                    var eventMsg = JsonConvert.DeserializeObject<WsSessionEventMessage>(json);
                    if (eventMsg.sessionEvent?.id.HasValue == true)
                        _lastEventId = eventMsg.sessionEvent.id.Value;
                    OnSessionEvent?.Invoke(eventMsg);
                    break;
                case "metadata_updated":
                    OnMetadataUpdated?.Invoke(JsonConvert.DeserializeObject<WsMetadataUpdatedMessage>(json));
                    break;
                case "error":
                    var err = JsonConvert.DeserializeObject<WsErrorMessage>(json);
                    OnError?.Invoke(err.error);
                    Debug.LogError($"[OpenGameCloud] Server error: {json}");
                    break;
            }
        }

        private async Task KeepAlive() {
            while (IsConnected) {
                await Task.Delay(PingDelay);
                if (IsConnected)
                    await _webSocket.SendText(JsonConvert.SerializeObject(new WsPingMessage()));
            }
        }
    }
}
