# SlateyMP

Servers and Libraries for UDP-based multiplayer games written in C3

https://slatey.tv

#### Configuration

Login and Realm servers use environment variables for configuration. The .vscode/launch.json is set up to use a .env file for Login and Realm for dev purposes. These files are excluded from the git repository however. Example files are below.

SlateyMP.Server.Login/.env
~~~~
LOGIN_SERVER_DB="server=localhost;database=meteordb;uid=root;password=password;SslMode=None"
LOGIN_SERVER_ADDRESS="0.0.0.0"
LOGIN_SERVER_PORT="11000"
~~~~

SlateyMP.Server.Realm/.env
~~~~
REALM_SERVER_DB="server=localhost;database=meteordb;uid=root;password=password;SslMode=None"
REALM_SERVER_ADDRESS="0.0.0.0"
REALM_SERVER_PORT="11001"
~~~~
