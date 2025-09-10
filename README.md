## GI Live Hotfix Downloader

**GI Live Hotfix Downloader** is a tool for managing and verifying Live Hotfix data patches downloaded when you log in to the game. Typically, these patches are located in the `GenshinImpact_Data/Persistent` folder. Some data will delete in the next update. So, If we don't backup live updates (suffixes and versions from query region info), we will loose cool data forever.


## Compile Instructions

1. Install [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)  
2. Run `compile.bat`  
3. Output will be in `bin` folder.

### How to Use
1. **Configure config.json**: 
   - Edit `Version Game (branch)`, `Clients Game (client)`, and`Main Url (url)`.
   
2. **Start the Download**:
   - Click `fetchrel.exe` to begin the download process.
   - Click `release.exe` to generate GenshinImpact_Data/Persistent.

## Available Main URLs
- https://autopatchhk.yuanshen.com
- https://archive.org/download/genshin-autopatch-3.2 (for version 3.2_live only)
- https://mirror.autopatch-reversedrooms.workers.dev/anime-cn (for version 3.4_live only)

## Available Clients
- StandaloneWindows64
- Android
- iOS
- PS4
- PS5 (Not available before 1.5_live)

## Additional Note
- Only for 3.2_live iOS, use `res 11611739_50a8ffbbbd`
- We are currently missing silence and data for 1.1_live and 2.1_live
- You can choose either of the two config.json formats
```json
   
   format 1
   "1.0_live": {
       "res": "1284249_ba7ad33643",
       "silence": "1393824_2599c61c7b",
       "data": "1358691_cdc3f383ef"
     }

   format 2
   "1.0_live": {
    "res": {
      "revision": "1284249_ba7ad33643",
      "audio": "en", 
      "base": true
    },
    "silence": {
      "revision": "1393824_2599c61c7b"
    },
    "data": {
      "revision": "1358691_cdc3f383ef"
    }
  }
```
