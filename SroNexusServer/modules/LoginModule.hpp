#pragma once
#include <string>
#include <iostream>
#include <memory>
#include <boost/asio.hpp>
#include <server_interface.hpp>
#include <server_connection_interface.hpp>
#include <server_state_handshake_interface.hpp>

class LoginConnection;

class LoginModule : public srv::IServer {
public:
    LoginModule();
    virtual ~LoginModule();

    bool Initialize(int port);
    void Start();
    void Run();
    void Stop();

protected:
    // IServer overrides
    virtual bool OnInitialize() override;
    virtual void OnConfigure(const std::map<std::string,std::string>& config_entries) override;
    virtual void OnRemoveConnection(const uint32_t ID) override;
    virtual void CreateConnection() override;

private:
    bool m_running;
};

// Login connection handling Gateway+Master logic
class LoginConnection : public srv::IConnection {
public:
    LoginConnection(uint32_t id, boost::asio::io_service& io_service, srv::IServer* srv);
    virtual ~LoginConnection();
};
