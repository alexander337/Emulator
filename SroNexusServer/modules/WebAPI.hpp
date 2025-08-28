#pragma once
#include <string>
#include <functional>
#include <unordered_map>
#include <memory>
#include <boost/beast.hpp>
#include <boost/asio.hpp>
#include <nlohmann/json.hpp>

namespace sro {

// RESTful Web API for external tools and monitoring
class WebAPI {
public:
    using json = nlohmann::json;
    namespace beast = boost::beast;
    namespace http = beast::http;
    namespace net = boost::asio;
    using tcp = net::ip::tcp;

    // HTTP request handler
    using RequestHandler = std::function<json(const json& params, const std::string& body)>;

    // WebSocket connection for real-time updates
    class WebSocketSession : public std::enable_shared_from_this<WebSocketSession> {
    public:
        WebSocketSession(tcp::socket socket);
        void Run();
        void Send(const json& message);
        void Close();
        
    private:
        beast::websocket::stream<tcp::socket> m_ws;
        beast::flat_buffer m_buffer;
        std::queue<std::string> m_writeQueue;
        std::mutex m_writeMutex;
        
        void OnAccept(beast::error_code ec);
        void DoRead();
        void OnRead(beast::error_code ec, std::size_t bytes);
        void DoWrite();
        void OnWrite(beast::error_code ec, std::size_t bytes);
    };

    // HTTP session handler
    class HttpSession : public std::enable_shared_from_this<HttpSession> {
    public:
        HttpSession(tcp::socket socket, WebAPI* api);
        void Run();
        
    private:
        tcp::socket m_socket;
        beast::flat_buffer m_buffer;
        http::request<http::string_body> m_request;
        http::response<http::string_body> m_response;
        WebAPI* m_api;
        
        void DoRead();
        void OnRead(beast::error_code ec, std::size_t bytes);
        void ProcessRequest();
        void DoWrite();
        void OnWrite(beast::error_code ec, std::size_t bytes, bool close);
    };

    // API endpoints
    struct Endpoint {
        std::string method;
        std::string path;
        RequestHandler handler;
        std::vector<std::string> requiredParams;
        std::string description;
        bool requiresAuth;
        std::vector<std::string> requiredPermissions;
    };

    // Authentication token
    struct AuthToken {
        std::string token;
        std::string username;
        std::vector<std::string> permissions;
        std::chrono::steady_clock::time_point expiry;
        std::string ipAddress;
    };

    // Rate limiting
    struct RateLimit {
        uint32_t requestsPerMinute;
        uint32_t requestsPerHour;
        uint32_t burstSize;
        std::chrono::steady_clock::time_point windowStart;
        uint32_t currentRequests;
    };

public:
    static WebAPI& Instance() {
        static WebAPI instance;
        return instance;
    }
    
    // Server control
    bool Start(uint16_t port);
    void Stop();
    bool IsRunning() const { return m_running; }
    
    // Endpoint registration
    void RegisterEndpoint(const Endpoint& endpoint);
    void UnregisterEndpoint(const std::string& method, const std::string& path);
    
    // Built-in endpoints
    void RegisterDefaultEndpoints();
    
    // Authentication
    std::string GenerateToken(const std::string& username, const std::vector<std::string>& permissions);
    bool ValidateToken(const std::string& token);
    void RevokeToken(const std::string& token);
    
    // WebSocket management
    void BroadcastToWebSockets(const json& message);
    void SendToWebSocket(const std::string& sessionId, const json& message);
    
    // Rate limiting
    void SetRateLimit(const std::string& ipAddress, const RateLimit& limit);
    bool CheckRateLimit(const std::string& ipAddress);
    
    // CORS configuration
    void SetCORSOrigin(const std::string& origin) { m_corsOrigin = origin; }
    void SetCORSEnabled(bool enabled) { m_corsEnabled = enabled; }
    
private:
    WebAPI() = default;
    ~WebAPI();
    
    // Server components
    std::unique_ptr<net::io_context> m_ioc;
    std::unique_ptr<tcp::acceptor> m_acceptor;
    std::thread m_serverThread;
    bool m_running;
    
    // Endpoints
    std::unordered_map<std::string, Endpoint> m_endpoints;
    std::mutex m_endpointMutex;
    
    // Sessions
    std::unordered_map<std::string, std::shared_ptr<WebSocketSession>> m_wsSessions;
    std::mutex m_sessionMutex;
    
    // Authentication
    std::unordered_map<std::string, AuthToken> m_tokens;
    std::mutex m_tokenMutex;
    
    // Rate limiting
    std::unordered_map<std::string, RateLimit> m_rateLimits;
    std::mutex m_rateLimitMutex;
    
    // CORS
    std::string m_corsOrigin;
    bool m_corsEnabled;
    
    // Server loop
    void Accept();
    void OnAccept(beast::error_code ec, tcp::socket socket);
    
    // Default endpoint handlers
    json HandleStatus(const json& params, const std::string& body);
    json HandleServerInfo(const json& params, const std::string& body);
    json HandlePlayerList(const json& params, const std::string& body);
    json HandlePlayerInfo(const json& params, const std::string& body);
    json HandleServerControl(const json& params, const std::string& body);
    json HandleMetrics(const json& params, const std::string& body);
    json HandleLogs(const json& params, const std::string& body);
    json HandleDatabase(const json& params, const std::string& body);
    json HandleMarket(const json& params, const std::string& body);
    json HandleEvents(const json& params, const std::string& body);
};

// GraphQL support for complex queries
class GraphQLHandler {
public:
    struct Schema {
        std::string query;
        std::string mutation;
        std::string subscription;
    };
    
    static json ExecuteQuery(const std::string& query, const json& variables = {});
    static void RegisterSchema(const Schema& schema);
    
private:
    static Schema m_schema;
    static json ResolveField(const std::string& field, const json& args);
};

// Webhook system for external integrations
class WebhookSystem {
public:
    struct Webhook {
        std::string url;
        std::string event;
        std::string method;
        json headers;
        json payload;
        bool active;
        uint32_t retryCount;
        std::chrono::seconds retryDelay;
    };
    
    static void RegisterWebhook(const Webhook& webhook);
    static void TriggerWebhook(const std::string& event, const json& data);
    static void ProcessWebhookQueue();
    
private:
    static std::vector<Webhook> m_webhooks;
    static std::queue<std::pair<Webhook, json>> m_queue;
    static std::mutex m_queueMutex;
    static std::thread m_processorThread;
    
    static void SendWebhook(const Webhook& webhook, const json& data);
};

// Server-Sent Events for real-time updates
class SSEHandler {
public:
    class SSEConnection {
    public:
        SSEConnection(tcp::socket socket);
        void Send(const std::string& event, const json& data);
        void Close();
        
    private:
        tcp::socket m_socket;
        bool m_connected;
    };
    
    static void AddConnection(std::shared_ptr<SSEConnection> connection);
    static void RemoveConnection(std::shared_ptr<SSEConnection> connection);
    static void Broadcast(const std::string& event, const json& data);
    
private:
    static std::vector<std::shared_ptr<SSEConnection>> m_connections;
    static std::mutex m_connectionMutex;
};

} // namespace sro
