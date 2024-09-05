## GI Live Hotfix Downloader

**GI Live Hotfix Downloader** is a tool for managing and verifying Live Hotfix data patches downloaded when you log in to the game. Typically, these patches are located in the `GenshinImpact_Data/Persistent` folder. Some data will delete in the next update. So, If we don't backup live updates (suffixes and versions from query region info), we will loose cool data forever.

## Getting Started

### Requirements
- Install [Node.js](https://nodejs.org/en)

### How to Use
1. **Configure Environment Variables**: 
   - Change `MAIN_URL`, `VERSION`, and `CLIENT_PATH` in the `.env` file to valid values.
   
2. **Install Dependencies**:
   - Open a terminal and run:
     ```bash
     npm install
     ```

3. **Start the Download**:
   - Click on `start.bat` to begin the download process.

## Available Main URLs
- https://autopatchhk.yuanshen.com
- https://mirror.autopatch-reversedrooms.workers.dev/anime-cn (for version 3.4_live only)

## Available Clients
- StandaloneWindows64
- Android
- iOS
- PS4
- PS5 (Not available before 1.5_live)
