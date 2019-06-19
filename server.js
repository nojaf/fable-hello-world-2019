const fs = require("fs");
const path = require("path");
const bs = require("browser-sync").create();

function isModuleRequest(url) {
    const pieces = url.split("/");
    return pieces && pieces[pieces.length - 1].indexOf(".") === -1;
}

bs.init({
    server: "./",
    open: false,
    watch: true,
    middleware: [
        function (req, res, next) {
            if (isModuleRequest(req.url) && req.url !== "/") {
                const content = fs.readFileSync(path.join(__dirname, `${req.url}.js`), 'utf8');
                res.writeHead(200, {'Content-Type': 'application/javascript'});
                res.write(content);
                res.end();
            } else {
                next();
            }
        }
    ]
});