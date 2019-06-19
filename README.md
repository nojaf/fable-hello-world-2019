# Fable Hello world in 2019

## Setup

https://fable.io/fable-doc/3-steps/setup.html

Install paket globally:
> dotnet tool install --global Paket

> yarn init --yes
> yarn add -D fable-compiler fable-splitter
> yarn add -D browser-sync

> paket init

Change paket.dependencies:

```
storage: none
framework: netstandard2.0
nuget Fable.Browser.Dom
```

> paket install

> paket generate-load-scripts -t fsx

Create index.html

```
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <meta http-equiv="X-UA-Compatible" content="ie=edge">
    <title>Fable Exercise</title>
</head>
<body>
    <script src="/dist/App.js" type="module"></script>
</body>
</html>
```

Create App.fsx

```
#load ".paket/load/main.group.fsx"

printfn "Fable compiled this"
```

Create server.js

```
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
```

Workaround for https://github.com/BrowserSync/browser-sync/issues/1414

Add npm scripts:

```
    "compile": "fable-splitter App.fsx -o dist -w",
    "sync": "node server.js"
```

Browser to localhost and open console.

## Exercise

https://codepen.io/jorgecardoso/post/0-basics-html-css-javascript#exercise-3-7

Show exercise 3