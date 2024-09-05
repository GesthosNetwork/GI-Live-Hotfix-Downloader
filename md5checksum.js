const crypto = require('crypto');
const fs = require('fs');
const path = require('path');

function checkFile(filePath, expectedMd5, expectedSize) {
    if (!fs.existsSync(filePath)) {
        console.log(`File not found, skipping: ${filePath}`);
        return;
    }

    const fileContent = fs.readFileSync(filePath);
    const fileSize = fs.statSync(filePath).size;
    const fileMd5 = crypto.createHash('md5').update(fileContent).digest('hex');

    if (fileMd5 === expectedMd5 && fileSize === expectedSize) {
        console.log(`File ${filePath} is OK!`);
    } else {
        console.log(`File ${filePath} does NOT match the expected values.`);
        console.log(`Expected MD5: ${expectedMd5}, Found MD5: ${fileMd5}`);
        console.log(`Expected Size: ${expectedSize}, Found Size: ${fileSize}`);
        console.log();
        nonMatchingCount++;
    }
}


const searchFolder = path.join(__dirname, );
const dataVersionsFileName = 'data_versions';

function findFile(dir, fileName, callback) {
    fs.readdir(dir, (err, files) => {
        if (err) {
            return console.error(`Error reading directory ${dir}: ${err.message}`);
       }

        let pending = files.length;
        if (!pending) return callback([]);

        files.forEach(file => {
            const filePath = path.join(dir, file);

            fs.stat(filePath, (err, stats) => {
                if (err) {
                    console.error(`Error stating file ${filePath}: ${err.message}`);
                    if (!--pending) callback([]);
                    return;
                }

                if (stats.isDirectory()) {
                    findFile(filePath, fileName, (res) => {
                        if (res.length) callback(res);
                        if (!--pending) callback([]);
                    });
                } else if (file === fileName) {
                    callback([filePath]);
                } else {
                    if (!--pending) callback([]);
                }
            });
        });
    });
}

let nonMatchingCount = 0;

findFile(searchFolder, dataVersionsFileName, (files) => {
    if (files.length === 0) {
        return;
    }

    files.forEach(dataVersionsPath => {
        try {
            const dataVersions = fs.readFileSync(dataVersionsPath, 'utf8');
            const lines = dataVersions.trim().split('\n');

            lines.forEach(line => {
                try {
                    const data = JSON.parse(line);
                    const filePath = path.join(path.dirname(dataVersionsPath), data.remoteName);
                    checkFile(filePath, data.md5, data.fileSize);
                } catch (parseError) {
                    console.error(`Error parsing line: ${parseError.message}`);
                }
            });

            if (nonMatchingCount === 0) {
                console.log('All files OK!');
				console.log();
            } else {
                console.log(`Total files that did not match: ${nonMatchingCount}`);
				console.log();
            }
        } catch (readError) {
            console.error(`Error reading data_versions: ${readError.message}`);
        }
		console.log('Operation Completed!');
    });
});

module.exports = { checkFile, };