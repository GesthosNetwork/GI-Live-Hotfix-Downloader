const fs = require('fs');
const path = require('path');
const helpers = require('./helpers');

function loadEnvVariables(envPath) {
    const envFile = fs.readFileSync(envPath, 'utf-8');
    const envVars = envFile.split('\n');

    envVars.forEach(line => {
        const [key, value] = line.split('=');
        if (key && value) {
            process.env[key.trim()] = value.trim();
        }
    });
}

loadEnvVariables(path.join(__dirname, '.env'));

const mainUrl = process.env.MAIN_URL;
const version = process.env.VERSION;
const clientPath = process.env.CLIENT_PATH;
const versions = require(path.join(__dirname, 'version.release', version));

const paths = {
    res: {
        Mode: 'client_game_res',
        Clients: [clientPath],
        Mappers: [
            'res_versions_external',
            'res_versions_medium',
            'res_versions_streaming',
            'release_res_versions_external',
            'release_res_versions_medium',
            'release_res_versions_streaming',
            'base_revision',
            'script_version',
            'AudioAssets/audio_versions',
        ]
    },
    clientSilence: {
        Mode: 'client_design_data',
        Clients: ['client_silence/General/AssetBundles'],
        Mappers: ['data_versions']
    },
    client: {
        Mode: 'client_design_data',
        Clients: ['client/General/AssetBundles'],
        Mappers: ['data_versions']
    },
};

const resolvers = {
    AudioAssets: ['pck'],
    VideoAssets: ['cuepoint', 'usm'],
    AssetBundles: ['blk'],
};

async function downloadFile(url, savePath) {
    try {
        console.log(`Starting download for => ${url}`);
        await helpers.forceDownload(url, savePath);
        const downloadedFileSize = fs.statSync(savePath).size;
        console.log(`Downloaded file size: ${downloadedFileSize} bytes`);
        console.log();
    } catch (error) {
        console.error(`Error downloading ${url}. Error: ${error.message}`);
    }
}

(async () => {
    for (const [version, versionDatas] of Object.entries(versions.list)) {
        for (const versionData of versionDatas) {
            for (const [liveType, liveData] of Object.entries(versionData)) {
                const pathData = paths[liveType];
                for (const client of pathData.Clients) {
                    for (const mapper of pathData.Mappers) {
                        const fileFolder = `${pathData.Mode}/${version}/output_${liveData.Version}_${liveData.Suffix}/${client}`;
                        const mapperUrl = `${mainUrl}/${fileFolder}/${mapper}`;

                        const saveFileFolder = `${__dirname}/downloads/${fileFolder}`;
                        if (!fs.existsSync(saveFileFolder)) {
                            fs.mkdirSync(saveFileFolder, { recursive: true });
                        }

                        const saveFilePath = `${saveFileFolder}/${mapper}`;
                        await downloadFile(mapperUrl, saveFilePath);

                        if (['script_version', 'base_revision'].includes(mapper)) continue;

                        const mapperLines = fs.readFileSync(saveFilePath).toString().split("\n");

                        for (const line of mapperLines) {
                            if (!line) continue;

                            try {
                                const mapperData = JSON.parse(line);
                                const remoteName = mapperData.remoteName;

                                if (remoteName === 'svc_catalog') continue;

                                const ext = remoteName.split('.').pop();
                                let extFolder = '';

                                for (const [resolveFolder, resolveExts] of Object.entries(resolvers)) {
                                    if (resolveExts.includes(ext)) {
                                        extFolder = resolveFolder;
                                        break;
                                    }
                                }

                                if (!extFolder) {
                                    console.log(`Can't detect extFolder for ext: ${ext}, remoteName: ${remoteName}, it's OK but check it yourself. In the current case saving to the root folder instead of extFolder`);
                                }

                                if (extFolder && saveFileFolder.indexOf(extFolder) > -1) {
                                    extFolder = '';
                                }

                                const gameFileSavePath = `${saveFileFolder}/${extFolder}/${remoteName}`;
                                const gameFileUrl = `${mainUrl}/${fileFolder}/${extFolder}/${remoteName}`.replace(`${fileFolder}//`, `${fileFolder}/`);

                                await downloadFile(gameFileUrl, gameFileSavePath);

                            } catch (error) {
                                console.error(`Error parsing line: ${line}. Error: ${error.message}`);
                            }
                        }
                    }
                }
            }
        }
    }
const md5checksum = require('./md5checksum');
})();
