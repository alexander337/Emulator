#pragma once
#include <windows.h>
#include <sql.h>
#include <sqlext.h>
#include <string>
#include <vector>
#include <memory>
#include <mutex>

namespace db {

class SqlConnection {
public:
    SqlConnection();
    ~SqlConnection();
    
    bool Connect(const std::string& connectionString);
    void Disconnect();
    bool IsConnected() const { return m_connected; }
    
    bool Execute(const std::string& query);
    bool ExecuteQuery(const std::string& query, std::vector<std::vector<std::string>>& results);
    
    std::string GetLastError() const { return m_lastError; }
    
private:
    SQLHENV m_env;
    SQLHDBC m_dbc;
    SQLHSTMT m_stmt;
    bool m_connected;
    std::string m_lastError;
    
    void SetError(const std::string& context, SQLHANDLE handle, SQLSMALLINT type);
};

class SqlServerPool {
public:
    static SqlServerPool& Instance() {
        static SqlServerPool instance;
        return instance;
    }
    
    bool Initialize(const std::string& connectionString, size_t poolSize = 5);
    void Shutdown();
    
    std::shared_ptr<SqlConnection> GetConnection();
    void ReturnConnection(std::shared_ptr<SqlConnection> conn);
    
    bool Execute(const std::string& query);
    bool ExecuteQuery(const std::string& query, std::vector<std::vector<std::string>>& results);
    
private:
    SqlServerPool() = default;
    ~SqlServerPool() { Shutdown(); }
    
    SqlServerPool(const SqlServerPool&) = delete;
    SqlServerPool& operator=(const SqlServerPool&) = delete;
    
    std::string m_connectionString;
    std::vector<std::shared_ptr<SqlConnection>> m_pool;
    std::vector<std::shared_ptr<SqlConnection>> m_available;
    std::mutex m_mutex;
    bool m_initialized = false;
};

} // namespace db
