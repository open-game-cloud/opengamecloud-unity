using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Net.OpenGameCloud {

    public class WsMessage {
        public string type;
    }

    public class WsActiveUser {
        public string userId;
        public object metadata;

        public T GetMetadata<T>() => ((JToken)metadata).ToObject<T>();
    }

    public class WsSessionEvent {
        public int? id;
        public string projectId;
        public string sessionId;
        public string userId;
        public string eventType;
        public string createdAt;
        public object payload;
        public string[] targetUserIds;

        public T GetPayload<T>() => ((JToken)payload).ToObject<T>();
    }

    public class WsJoinedSessionMessage : WsMessage {
        public string sessionId;
        public string userId;
        public WsActiveUser[] activeUsers;
        public object sessionData;
        public int fromEventId;
        public WsSessionEvent[] historyEvents;

        public T GetSessionData<T>() => ((JToken)sessionData).ToObject<T>();
    }

    public class WsPlayerJoinedMessage : WsMessage {
        public string userId;
        public object metadata;

        public T GetMetadata<T>() => ((JToken)metadata).ToObject<T>();
    }

    public class WsPlayerLeftMessage : WsMessage {
        public string userId;
    }

    public class WsSessionEventMessage : WsMessage {
        [JsonProperty("event")]
        public WsSessionEvent sessionEvent;
    }

    public class WsMetadataUpdatedMessage : WsMessage {
        public string userId;
        public object metadata;

        public T GetMetadata<T>() => ((JToken)metadata).ToObject<T>();
    }

    public class WsErrorMessage : WsMessage {
        public string error;
    }

    public class WsPingMessage : WsMessage {
        public WsPingMessage() => type = "ping";
    }

    public class WsJoinSessionMessage : WsMessage {
        public string projectId;
        public string sessionId;
        public string apiKey;
        public string userToken;
        public object metadata;
        public string password;

        public WsJoinSessionMessage(string projectId, string sessionId, string apiKey,
            string userToken, object metadata = null, string password = null) {
            type = "join_session";
            this.projectId = projectId;
            this.sessionId = sessionId;
            this.apiKey = apiKey;
            this.userToken = userToken;
            this.metadata = metadata;
            this.password = password;
        }
    }

    public class WsSendEventMessage : WsMessage {
        public string projectId;
        public string sessionId;
        public string eventType;
        public object payload;
        public string[] targetUserIds;

        public WsSendEventMessage(string projectId, string sessionId, string eventType,
            object payload = null, string[] targetUserIds = null) {
            type = "send_event";
            this.projectId = projectId;
            this.sessionId = sessionId;
            this.eventType = eventType;
            this.payload = payload;
            this.targetUserIds = targetUserIds;
        }
    }

    public class WsUpdateSessionDataMessage : WsMessage {
        public string projectId;
        public string sessionId;
        public object data;
        public int lastEventId;

        public WsUpdateSessionDataMessage(string projectId, string sessionId, object data, int lastEventId) {
            type = "update_session_data";
            this.projectId = projectId;
            this.sessionId = sessionId;
            this.data = data;
            this.lastEventId = lastEventId;
        }
    }

    public class WsUpdateMetadataMessage : WsMessage {
        public string projectId;
        public string sessionId;
        public object metadata;

        public WsUpdateMetadataMessage(string projectId, string sessionId, object metadata) {
            type = "update_metadata";
            this.projectId = projectId;
            this.sessionId = sessionId;
            this.metadata = metadata;
        }
    }
}
