#pragma once
#include <string>
#include <iostream>
#include <memory>
#include <boost/asio.hpp>
#include <server_interface.hpp>
#include <server_connection_interface.hpp>

class GameConnection;

class GameModule : public srv::IServer {
public:
    GameModule();
    virtual ~GameModule();

    bool Initialize(int port);
    void Start();
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

// Game connection handling Agent logic
class GameConnection : public srv::IConnection {
public:
    GameConnection(uint32_t id, boost::asio::io_service& io_service, srv::IServer* srv);
    virtual ~GameConnection();
};
