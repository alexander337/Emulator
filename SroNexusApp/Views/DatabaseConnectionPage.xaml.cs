using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SroNexusApp.Views
{
    public partial class DatabaseConnectionPage : Page
    {
        private string _connectionString = "";
        private bool _isConnected = false;

        public DatabaseConnectionPage()
        {
            InitializeComponent();
            cmbAuth.SelectionChanged += CmbAuth_SelectionChanged;
            UpdateConnectionString();
        }

        private void CmbAuth_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool isSqlAuth = cmbAuth.SelectedIndex == 0;
            lblUsername.IsEnabled = isSqlAuth;
            lblPassword.IsEnabled = isSqlAuth;
            txtUsername.IsEnabled = isSqlAuth;
            txtPassword.IsEnabled = isSqlAuth;
            UpdateConnectionString();
        }

        private void UpdateConnectionString()
        {
            var builder = new SqlConnectionStringBuilder();
            builder.DataSource = txtServer.Text;
            builder.InitialCatalog = txtDatabase.Text;
            
            if (cmbAuth.SelectedIndex == 0) // SQL Auth
            {
                builder.IntegratedSecurity = false;
                builder.UserID = txtUsername.Text;
                builder.Password = txtPassword.Password;
            }
            else // Windows Auth
            {
                builder.IntegratedSecurity = true;
            }
            
            if (chkTrustCert.IsChecked == true)
            {
                builder.TrustServerCertificate = true;
            }
            
            _connectionString = builder.ConnectionString;
            txtConnectionString.Text = _connectionString;
        }

        private async void BtnTestConnection_Click(object sender, RoutedEventArgs e)
        {
            UpdateConnectionString();
            txtStatus.Text = "Testing connection...";
            txtStatus.Foreground = System.Windows.Media.Brushes.Orange;
            
            bool success = await TestConnection();
            
            if (success)
            {
                txtStatus.Text = "Connection successful!";
                txtStatus.Foreground = System.Windows.Media.Brushes.Green;
                btnConnect.IsEnabled = true;
            }
            else
            {
                txtStatus.Text = "Connection failed. Check your settings.";
                txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                btnConnect.IsEnabled = false;
            }
        }

        private async Task<bool> TestConnection()
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            UpdateConnectionString();
            
            if (await TestConnection())
            {
                _isConnected = true;
                txtStatus.Text = "Connected to database";
                txtStatus.Foreground = System.Windows.Media.Brushes.Green;
                btnInitSchema.IsEnabled = true;
                
                // Save connection to config
                SaveConnectionToConfig();
            }
        }

        private void SaveConnectionToConfig()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "SroNexusServer", "config", "sronexus.json");
                
                string json = File.ReadAllText(configPath);
                JObject config = JObject.Parse(json);
                
                config["Database"] = new JObject
                {
                    ["Server"] = txtServer.Text,
                    ["Database"] = txtDatabase.Text,
                    ["Authentication"] = cmbAuth.SelectedIndex == 0 ? "SQL" : "Windows",
                    ["Username"] = txtUsername.Text,
                    ["Password"] = txtPassword.Password, // Note: In production, encrypt this
                    ["TrustServerCertificate"] = chkTrustCert.IsChecked == true
                };
                
                File.WriteAllText(configPath, config.ToString(Formatting.Indented));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save config: {ex.Message}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void BtnInitSchema_Click(object sender, RoutedEventArgs e)
        {
            progressBar.Visibility = Visibility.Visible;
            txtProgress.Text = "Initializing database schema...";
            
            try
            {
                await InitializeSchema();
                txtStatus.Text = "Schema initialized successfully!";
                txtStatus.Foreground = System.Windows.Media.Brushes.Green;
                MessageBox.Show("Database schema has been initialized successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                txtStatus.Text = "Schema initialization failed";
                txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                MessageBox.Show($"Schema initialization failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                progressBar.Visibility = Visibility.Collapsed;
                txtProgress.Text = "";
            }
        }

        private async Task InitializeSchema()
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                
                // Create databases if they don't exist
                await CreateDatabaseIfNotExists(conn, "SRO_VT_SHARD");
                await CreateDatabaseIfNotExists(conn, "SRO_VT_ACCOUNT");
                
                // Run schema creation scripts
                string schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Database", "SqlServer");
                
                // Create tables for Account database
                await RunScript(conn, Path.Combine(schemaPath, "AccountDB_Schema.sql"), "SRO_VT_ACCOUNT");
                
                // Create tables for Shard database  
                await RunScript(conn, Path.Combine(schemaPath, "ShardDB_Schema.sql"), "SRO_VT_SHARD");
                
                // Load initial data
                await RunScript(conn, Path.Combine(schemaPath, "InitialData.sql"), "SRO_VT_SHARD");
            }
        }

        private async Task CreateDatabaseIfNotExists(SqlConnection conn, string dbName)
        {
            var checkCmd = new SqlCommand($"SELECT database_id FROM sys.databases WHERE Name = '{dbName}'", conn);
            var result = await checkCmd.ExecuteScalarAsync();
            
            if (result == null)
            {
                var createCmd = new SqlCommand($"CREATE DATABASE [{dbName}]", conn);
                await createCmd.ExecuteNonQueryAsync();
                txtProgress.Text = $"Created database: {dbName}";
            }
        }

        private async Task RunScript(SqlConnection conn, string scriptPath, string database)
        {
            if (!File.Exists(scriptPath))
            {
                // Create a basic schema if file doesn't exist
                await CreateBasicSchema(conn, database);
                return;
            }
            
            string script = File.ReadAllText(scriptPath);
            
            // Switch to target database
            var useCmd = new SqlCommand($"USE [{database}]", conn);
            await useCmd.ExecuteNonQueryAsync();
            
            // Split by GO statements
            string[] batches = script.Split(new[] { "\r\nGO\r\n", "\nGO\n" }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var batch in batches)
            {
                if (!string.IsNullOrWhiteSpace(batch))
                {
                    var cmd = new SqlCommand(batch, conn);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task CreateBasicSchema(SqlConnection conn, string database)
        {
            // Create basic tables if schema files don't exist
            var useCmd = new SqlCommand($"USE [{database}]", conn);
            await useCmd.ExecuteNonQueryAsync();
            
            if (database == "SRO_VT_ACCOUNT")
            {
                // Create accounts table
                string createAccounts = @"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='accounts' AND xtype='U')
                    CREATE TABLE accounts (
                        id INT IDENTITY(1,1) PRIMARY KEY,
                        username NVARCHAR(50) UNIQUE NOT NULL,
                        password NVARCHAR(100) NOT NULL,
                        email NVARCHAR(100),
                        last_login_ip NVARCHAR(50),
                        last_login_time DATETIME,
                        is_logged BIT DEFAULT 0,
                        created_at DATETIME DEFAULT GETDATE()
                    )";
                var cmd = new SqlCommand(createAccounts, conn);
                await cmd.ExecuteNonQueryAsync();
            }
            else if (database == "SRO_VT_SHARD")
            {
                // Create character table
                string createCharacters = @"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='characters' AND xtype='U')
                    CREATE TABLE characters (
                        id INT IDENTITY(1,1) PRIMARY KEY,
                        account_id INT NOT NULL,
                        name NVARCHAR(50) UNIQUE NOT NULL,
                        level INT DEFAULT 1,
                        experience BIGINT DEFAULT 0,
                        gold BIGINT DEFAULT 0,
                        position_x FLOAT DEFAULT 0,
                        position_y FLOAT DEFAULT 0,
                        position_z FLOAT DEFAULT 0,
                        created_at DATETIME DEFAULT GETDATE()
                    )";
                var cmd = new SqlCommand(createCharacters, conn);
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}
