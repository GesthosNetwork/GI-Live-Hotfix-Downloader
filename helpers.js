const crypto = require('crypto');
const fs = require('fs');
const util = require('util');
const stream = require('stream');
const https = require('https');
const http = require('http');
const { finished } = require('node:stream/promises');

const unlinkIfExists = (filePath) => fs.existsSync(filePath) ? fs.unlinkSync(filePath) : '';
const isDelZeroFiles = false;

function checkAndDelZeroFile(filePath) {
    if (!isDelZeroFiles) return;

    if (fs.existsSync(filePath) && fs.statSync(filePath).size === 0) {
        console.log(`empty ${filePath}, unlinking...`);
        fs.unlinkSync(filePath);
    }
}

function timeout(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

function removeControlChars(str) {
    return str.replace(/[\u0000-\u001F\u007F-\u009F]/g, "");
}

function formatFileSize(bytes, decimalPoint) {
    if (bytes === 0) return '0 Bytes';
    let k = 1000,
        dm = decimalPoint || 2,
        sizes = ['Bytes', 'KB', 'MB', 'GB'],
        i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
}

async function forceDownload(link, fileFullPath) {
    link = trimAny(removeControlChars(link));
    fileFullPath = trimAny(removeControlChars(fileFullPath));

    if (isDelZeroFiles) checkAndDelZeroFile(fileFullPath);

    const fileFolder = fileFullPath.split('/').slice(0, -1).join('/');
    console.log('FileFullPath =>', fileFullPath);

    if (!fs.existsSync(fileFullPath)) {
        console.log(`Creating dir:`, fileFolder);
        fs.mkdirSync(fileFolder, { recursive: true });
        try {
            const tmpExt = '.~tmp';
            const fileFullTempPath = fileFullPath + tmpExt;

            console.log(`Creating temp file =>`, fileFullTempPath);
            const isTmpExists = fs.existsSync(fileFullTempPath);
            const fileStream = fs.createWriteStream(fileFullTempPath, isTmpExists ? { flags: 'a' } : {});
            let stats = isTmpExists ? fs.statSync(fileFullTempPath) : { size: 0 };

            const loader = link.indexOf('https') === 0 ? https : http;
            let downloaded = false;

            fileStream.on("end", () => {
                downloaded = true;
            });

            console.log(`Downloading ${link}`);

            let received_bytes = stats.size, total_bytes = 0;

            function makeRequest() {
                const location = new URL(link);
                const headers = {
                    'Range': `bytes=${stats.size}-`
                };

                const options = {
                    hostname: location.hostname,
                    path: location.pathname,
                    headers: headers
                };

                return loader.get(options).on('response', (response) => {
                    if (response.headers['content-length']) {
                        total_bytes = parseInt(response.headers['content-length']) + stats.size;
                    } else {
                        console.warn(`Content-Length not provided, using existing file size`);
                        total_bytes = stats.size;
                    }

                    console.log("Total size: ", formatFileSize(total_bytes));

                    response
                        .on('error', (err) => {
                            console.error('An error occurred', err);
                        })
                        .on('data', function (chunk) {
                            received_bytes += chunk.length;
                            const progress = total_bytes > 0 ? (received_bytes * 100 / total_bytes).toFixed(2) : 'unknown';
                            console.log(`[${link}] progress: ${formatFileSize(received_bytes)}/${formatFileSize(total_bytes)}, ${progress}%`);
                            if (received_bytes >= total_bytes)
                                downloaded = true;
                        })
                        .pipe(fileStream);
                });
            }

            let request = makeRequest();
            let last_received_bytes = 0;
            let last_time_diff = Date.now();

            while (!downloaded) {
                await timeout(100);

                if (Date.now() - last_time_diff > 5000) {
                    last_time_diff = Date.now();

                    if (last_received_bytes !== received_bytes)
                        last_received_bytes = received_bytes;
                    else {
                        console.warn(`Network error check your internet connection, trying to resume...`);
                        stats = fs.statSync(fileFullTempPath);
                        received_bytes = stats.size;
                        total_bytes = 0;
                        request = makeRequest();
                    }
                }
            }

            console.log(`Downloaded, Removing ${tmpExt} ext`);
            fs.renameSync(fileFullTempPath, fileFullPath);
            unlinkIfExists(fileFullTempPath);
        } catch (e) {
            console.log('error:', e);
        }
    } else {
        console.log(`Skip! already exists => ${fileFullPath}`);
    }
}

function trimAny(str, chars = ' ') {
    let start = 0, end = str.length;

    while (start < end && chars.indexOf(str[start]) >= 0)
        ++start;

    while (end > start && chars.indexOf(str[end - 1]) >= 0)
        --end;

    return (start > 0 || end < str.length) ? str.substring(start, end) : str;
}

function getFileName(path) {
    return trimAny(path, ' /\\').split('\\').pop().split('/').pop();
}

function getFileExt(path) {
    const match = path.match(/\.([^\./\?]+)($|\?)/);
    return match ? match[1] : null;
}

module.exports = { forceDownload, trimAny, getFileName, getFileExt, removeControlChars, };
