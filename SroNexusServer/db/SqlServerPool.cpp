#include "SqlServerPool.hpp"
#include <iostream>
#include <sstream>

namespace db {

SqlConnection::SqlConnection() 
    : m_env(SQL_NULL_HENV), m_dbc(SQL_NULL_HDBC), m_stmt(SQL_NULL_HSTMT), m_connected(false) {
}

SqlConnection::~SqlConnection() {
    Disconnect();
}

bool SqlConnection::Connect(const std::string& connectionString) {
    if (m_connected) return true;
    
    // Allocate environment handle
    if (SQLAllocHandle(SQL_HANDLE_ENV, SQL_NULL_HANDLE, &m_env) != SQL_SUCCESS) {
        m_lastError = "Failed to allocate environment handle";
        return false;
    }
    
    // Set ODBC version
    if (SQLSetEnvAttr(m_env, SQL_ATTR_ODBC_VERSION, (void*)SQL_OV_ODBC3, 0) != SQL_SUCCESS) {
        SetError("Set ODBC version", m_env, SQL_HANDLE_ENV);
        SQLFreeHandle(SQL_HANDLE_ENV, m_env);
        m_env = SQL_NULL_HENV;
        return false;
    }
    
    // Allocate connection handle
    if (SQLAllocHandle(SQL_HANDLE_DBC, m_env, &m_dbc) != SQL_SUCCESS) {
        SetError("Allocate connection", m_env, SQL_HANDLE_ENV);
        SQLFreeHandle(SQL_HANDLE_ENV, m_env);
        m_env = SQL_NULL_HENV;
        return false;
    }
    
    // Connect to database
    SQLCHAR outstr[1024];
    SQLSMALLINT outstrlen;
    SQLRETURN ret = SQLDriverConnect(m_dbc, NULL, 
        (SQLCHAR*)connectionString.c_str(), SQL_NTS,
        outstr, sizeof(outstr), &outstrlen,
        SQL_DRIVER_NOPROMPT);
    
    if (ret != SQL_SUCCESS && ret != SQL_SUCCESS_WITH_INFO) {
        SetError("Connect to database", m_dbc, SQL_HANDLE_DBC);
        SQLFreeHandle(SQL_HANDLE_DBC, m_dbc);
        SQLFreeHandle(SQL_HANDLE_ENV, m_env);
        m_dbc = SQL_NULL_HDBC;
        m_env = SQL_NULL_HENV;
        return false;
    }
    
    m_connected = true;
    return true;
}

void SqlConnection::Disconnect() {
    if (m_stmt != SQL_NULL_HSTMT) {
        SQLFreeHandle(SQL_HANDLE_STMT, m_stmt);
        m_stmt = SQL_NULL_HSTMT;
    }
    if (m_dbc != SQL_NULL_HDBC) {
        SQLDisconnect(m_dbc);
        SQLFreeHandle(SQL_HANDLE_DBC, m_dbc);
        m_dbc = SQL_NULL_HDBC;
    }
    if (m_env != SQL_NULL_HENV) {
        SQLFreeHandle(SQL_HANDLE_ENV, m_env);
        m_env = SQL_NULL_HENV;
    }
    m_connected = false;
}

bool SqlConnection::Execute(const std::string& query) {
    if (!m_connected) {
        m_lastError = "Not connected to database";
        return false;
    }
    
    // Allocate statement handle if needed
    if (m_stmt == SQL_NULL_HSTMT) {
        if (SQLAllocHandle(SQL_HANDLE_STMT, m_dbc, &m_stmt) != SQL_SUCCESS) {
            SetError("Allocate statement", m_dbc, SQL_HANDLE_DBC);
            return false;
        }
    }
    
    // Execute query
    SQLRETURN ret = SQLExecDirect(m_stmt, (SQLCHAR*)query.c_str(), SQL_NTS);
    if (ret != SQL_SUCCESS && ret != SQL_SUCCESS_WITH_INFO) {
        SetError("Execute query", m_stmt, SQL_HANDLE_STMT);
        return false;
    }
    
    return true;
}

bool SqlConnection::ExecuteQuery(const std::string& query, std::vector<std::vector<std::string>>& results) {
    if (!m_connected) {
        m_lastError = "Not connected to database";
        return false;
    }
    
    results.clear();
    
    // Allocate statement handle if needed
    if (m_stmt == SQL_NULL_HSTMT) {
        if (SQLAllocHandle(SQL_HANDLE_STMT, m_dbc, &m_stmt) != SQL_SUCCESS) {
            SetError("Allocate statement", m_dbc, SQL_HANDLE_DBC);
            return false;
        }
    }
    
    // Execute query
    SQLRETURN ret = SQLExecDirect(m_stmt, (SQLCHAR*)query.c_str(), SQL_NTS);
    if (ret != SQL_SUCCESS && ret != SQL_SUCCESS_WITH_INFO) {
        SetError("Execute query", m_stmt, SQL_HANDLE_STMT);
        return false;
    }
    
    // Get column count
    SQLSMALLINT columns;
    SQLNumResultCols(m_stmt, &columns);
    
    // Fetch results
    while (SQLFetch(m_stmt) == SQL_SUCCESS) {
        std::vector<std::string> row;
        for (SQLSMALLINT i = 1; i <= columns; i++) {
            SQLCHAR buf[256];
            SQLLEN indicator;
            ret = SQLGetData(m_stmt, i, SQL_C_CHAR, buf, sizeof(buf), &indicator);
            if (ret == SQL_SUCCESS || ret == SQL_SUCCESS_WITH_INFO) {
                if (indicator == SQL_NULL_DATA) {
                    row.push_back("NULL");
                } else {
                    row.push_back(std::string((char*)buf));
                }
            } else {
                row.push_back("");
            }
        }
        results.push_back(row);
    }
    
    SQLFreeStmt(m_stmt, SQL_CLOSE);
    return true;
}

bool SqlConnection::PrepareStatement(const std::string& query) {
    if (!m_connected) {
        m_lastError = "Not connected to database";
        return false;
    }
    
    // Free previous statement if exists
    if (m_stmt != SQL_NULL_HSTMT) {
        SQLFreeHandle(SQL_HANDLE_STMT, m_stmt);
        m_stmt = SQL_NULL_HSTMT;
    }
    
    // Allocate new statement handle
    if (SQLAllocHandle(SQL_HANDLE_STMT, m_dbc, &m_stmt) != SQL_SUCCESS) {
        SetError("Allocate statement", m_dbc, SQL_HANDLE_DBC);
        return false;
    }
    
    // Prepare the statement
    SQLRETURN ret = SQLPrepare(m_stmt, (SQLCHAR*)query.c_str(), SQL_NTS);
    if (ret != SQL_SUCCESS && ret != SQL_SUCCESS_WITH_INFO) {
        SetError("Prepare statement", m_stmt, SQL_HANDLE_STMT);
        return false;
    }
    
    return true;
}

bool SqlConnection::BindParameter(int index, const std::string& value) {
    if (m_stmt == SQL_NULL_HSTMT) {
        m_lastError = "No prepared statement";
        return false;
    }
    
    SQLRETURN ret = SQLBindParameter(m_stmt, index, SQL_PARAM_INPUT, SQL_C_CHAR, 
                                     SQL_VARCHAR, value.length(), 0, 
                                     (SQLPOINTER)value.c_str(), value.length(), NULL);
    
    if (ret != SQL_SUCCESS && ret != SQL_SUCCESS_WITH_INFO) {
        SetError("Bind string parameter", m_stmt, SQL_HANDLE_STMT);
        return false;
    }
    
    return true;
}

bool SqlConnection::BindParameter(int index, int value) {
    if (m_stmt == SQL_NULL_HSTMT) {
        m_lastError = "No prepared statement";
        return false;
    }
    
    SQLRETURN ret = SQLBindParameter(m_stmt, index, SQL_PARAM_INPUT, SQL_C_LONG,
                                     SQL_INTEGER, 0, 0, &value, 0, NULL);
    
    if (ret != SQL_SUCCESS && ret != SQL_SUCCESS_WITH_INFO) {
        SetError("Bind int parameter", m_stmt, SQL_HANDLE_STMT);
        return false;
    }
    
    return true;
}

bool SqlConnection::BindParameter(int index, double value) {
    if (m_stmt == SQL_NULL_HSTMT) {
        m_lastError = "No prepared statement";
        return false;
    }
    
    SQLRETURN ret = SQLBindParameter(m_stmt, index, SQL_PARAM_INPUT, SQL_C_DOUBLE,
                                     SQL_DOUBLE, 0, 0, &value, 0, NULL);
    
    if (ret != SQL_SUCCESS && ret != SQL_SUCCESS_WITH_INFO) {
        SetError("Bind double parameter", m_stmt, SQL_HANDLE_STMT);
        return false;
    }
    
    return true;
}

bool SqlConnection::ExecutePrepared() {
    if (m_stmt == SQL_NULL_HSTMT) {
        m_lastError = "No prepared statement";
        return false;
    }
    
    SQLRETURN ret = SQLExecute(m_stmt);
    if (ret != SQL_SUCCESS && ret != SQL_SUCCESS_WITH_INFO) {
        SetError("Execute prepared statement", m_stmt, SQL_HANDLE_STMT);
        return false;
    }
    
    return true;
}

bool SqlConnection::ExecutePreparedQuery(std::vector<std::vector<std::string>>& results) {
    if (!ExecutePrepared()) {
        return false;
    }
    
    results.clear();
    
    // Get column count
    SQLSMALLINT columns;
    SQLNumResultCols(m_stmt, &columns);
    
    // Fetch results
    while (SQLFetch(m_stmt) == SQL_SUCCESS) {
        std::vector<std::string> row;
        for (SQLSMALLINT i = 1; i <= columns; i++) {
            SQLCHAR buf[256];
            SQLLEN indicator;
            SQLRETURN ret = SQLGetData(m_stmt, i, SQL_C_CHAR, buf, sizeof(buf), &indicator);
            if (ret == SQL_SUCCESS || ret == SQL_SUCCESS_WITH_INFO) {
                if (indicator == SQL_NULL_DATA) {
                    row.push_back("NULL");
                } else {
                    row.push_back(std::string((char*)buf));
                }
            } else {
                row.push_back("");
            }
        }
        results.push_back(row);
    }
    
    SQLFreeStmt(m_stmt, SQL_CLOSE);
    return true;
}

void SqlConnection::SetError(const std::string& context, SQLHANDLE handle, SQLSMALLINT type) {
    SQLCHAR sqlstate[6];
    SQLCHAR message[256];
    SQLINTEGER native;
    SQLSMALLINT len;
    
    if (SQLGetDiagRec(type, handle, 1, sqlstate, &native, message, sizeof(message), &len) == SQL_SUCCESS) {
        std::stringstream ss;
        ss << context << " failed: " << message << " (SQLSTATE: " << sqlstate << ")";
        m_lastError = ss.str();
    } else {
        m_lastError = context + " failed: Unknown error";
    }
}

// SqlServerPool implementation
bool SqlServerPool::Initialize(const std::string& connectionString, size_t poolSize) {
    std::lock_guard<std::mutex> lock(m_mutex);
    
    if (m_initialized) return true;
    
    m_connectionString = connectionString;
    
    for (size_t i = 0; i < poolSize; ++i) {
        auto conn = std::make_shared<SqlConnection>();
        if (!conn->Connect(connectionString)) {
            std::cerr << "Failed to create connection " << i << ": " << conn->GetLastError() << std::endl;
            // Continue trying to create other connections
        } else {
            m_pool.push_back(conn);
            m_available.push_back(conn);
        }
    }
    
    if (m_available.empty()) {
        std::cerr << "Failed to create any database connections" << std::endl;
        return false;
    }
    
    m_initialized = true;
    return true;
}

void SqlServerPool::Shutdown() {
    std::lock_guard<std::mutex> lock(m_mutex);
    
    m_available.clear();
    m_pool.clear();
    m_initialized = false;
}

std::shared_ptr<SqlConnection> SqlServerPool::GetConnection() {
    std::lock_guard<std::mutex> lock(m_mutex);
    
    if (!m_initialized || m_available.empty()) {
        return nullptr;
    }
    
    auto conn = m_available.back();
    m_available.pop_back();
    
    // Verify connection is still valid
    if (!conn->IsConnected()) {
        conn->Connect(m_connectionString);
    }
    
    return conn;
}

void SqlServerPool::ReturnConnection(std::shared_ptr<SqlConnection> conn) {
    if (!conn) return;
    
    std::lock_guard<std::mutex> lock(m_mutex);
    m_available.push_back(conn);
}

bool SqlServerPool::Execute(const std::string& query) {
    auto conn = GetConnection();
    if (!conn) return false;
    
    bool result = conn->Execute(query);
    ReturnConnection(conn);
    return result;
}

bool SqlServerPool::ExecuteQuery(const std::string& query, std::vector<std::vector<std::string>>& results) {
    auto conn = GetConnection();
    if (!conn) return false;
    
    bool result = conn->ExecuteQuery(query, results);
    ReturnConnection(conn);
    return result;
}

} // namespace db
