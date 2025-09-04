# FragHub.DiscordBot

![.NET](https://img.shields.io/badge/.NET-9.0-blue)  ![License](https://img.shields.io/badge/license-MIT-lightgrey)  ![Status](https://img.shields.io/badge/status-hobby%20project-orange)

A hobby project that brings together **game server management**, **music playback**, and **user stats tracking** into a single **Discord bot** — built with a focus on **Clean Architecture** and **minimal coupling** between systems.  

## ✨ Features (Planned & In Progress)  
- 🎮 **Game Server Control**  
  - Start / stop / monitor supported servers  
  - Execute common admin commands from Discord  

- 🎵 **Music Player**  
  - Queue, play, pause, skip tracks  
  - Discord controller with embedded queue and recommendations (Lastfm)  

- 📊 **User Stats**  
  - Track activity (messages, commands, game time, etc.)  
  - Leaderboards and custom dashboards  

## 🏗️ Architecture Goals  
This project is not just about features, but also about **learning and practicing good software design**.  

- **Clean Architecture**:  
  - Core logic is independent of Discord or any specific service.  
  - External systems (Discord API, game servers, music players, databases) are implemented as **adapters**, not baked into the core.  

- **Minimal Coupling**:  
  - Systems (music, games, stats) can evolve separately.  
  - Easy to replace or mock dependencies.  
  - Encourages testing and experimentation.  

- **Dependency Injection**:  
  - Interfaces define contracts in the core.  
  - Infrastructure provides the actual implementations.


## 🚀 Getting Started  
### Prerequisites  
- .NET 9
- Discord Bot Token (create one at the [Discord Developer Portal](https://discord.com/developers/applications))
- Lavalink Server for Music Features (https://github.com/lavalink-devs/Lavalink)
- Persistent Backend (tbd)

### Setup  
```bash
git clone https://github.com/travis-stanley/FragHub.DiscordBot.git
cd FragHub.DiscordBot
dotnet build
```

### Configuration
Configuration is read from the .env file in the root directory of the Host layer. An example .env file is provided with the expected variables.
```env
# This file is an example of using a .env to configure the FragHub Host application.
# Create a file named .env in the same directory as this file and fill in the values below.

# Do not check .env files into version control systems like git.
# You should use .gitignore to exclude it from being tracked.

# The discord bot token is obtained from the Discord Developer Portal, https://discord.com/developers/applications
DISCORD_TOKEN=

# Name of the text channel where the bot will respond to commands. The first channel found THAT CONTAINS this name will be used.
DISCORD_TEXT_CHANNEL_NAME=Music

# Lavalink backend host ip and port. See https://github.com/lavalink-devs/Lavalink
LAVALINK_HOST=0.0.0.0
LAVALINK_PORT=2333
LAVALINK_PW=youshallnotpass
LAVALINK_LABEL=MusicBot

# Lastfm api token for pulling music recommendations. See https://last.fm/api
LASTFM_TOKEN=123456

# The discord guild id for development purposes. Commands are registered in this guild immediately.
# This is useful for testing commands without waiting for global command propagation.
# See https://discord.com/developers/docs/interactions/application-commands#registering-a-command
DISCORD_DEVELOPMENT_GUILD_ID=1234567890

# Data store connection string.
SQL_CONNECTION_STRING=Server=127.0.0.1;...
```

## 📜 License
MIT License — free to use, modify, and share.
