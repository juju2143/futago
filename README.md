# Futago: a Gemini client for existing HTTP browsers

Futago adds Gemini protocol support to web browsers supporting the webExtensions API. You will need to install a host application so Futago can open raw TLS sockets.

## Features

* Extremely configurable and customizable
* Integrated to your favourite HTTP browser
* No proxies
* Source code highlighter
* ANSI escape codes support for colored text!
* As standalone as possible

## The host

The host is basically a simple TLS client that initiates a Gemini connection and forwards the output to the web browser for further processing and parsing, as its unable to do it itself for security reasons. It's all there is to it.

### Compile

Install .NET 6, then in the `host` folder:

```sh
dotnet restore
dotnet build
```

### Install

Google Chrome:
```sh
dotnet run -- install
```

For different browsers:

On Linux and MacOS, locate your browser's `NativeMessagingHosts` directory (in this example Chromium), then:
```sh
dotnet run -- install --dir ~/.config/chromium/NativeMessagingHosts # Linux
dotnet run -- install --dir ~/Library/Application Support/Chromium/NativeMessagingHosts # MacOS
```

On Windows, the path does not matter since the browser uses the registry to locate the manifest file. By default, it's written in the current directory. You can use `--all` to install the app for all users instead of the current user only.

For more options see
```sh
dotnet run -- install --help
```

### Uninstall

Same notes as the above. Use the same `--dir`, `--name` and `--all` options you used to install it.

```sh
dotnet run -- uninstall
```

## The extension

Compatible with the most popular browsers like Chrome, Firefox and Edge, it communicates with the host application to initiate a Gemini connection, as its unable to do it itself for security reasons, then parses the result and creates a webpage out of it.

### Install

- Install the host app as above
- Install Node.js (or at least npm)
- `npm install` to download third-party js libraries
- Go to `chrome://extensions`
- Enable developer mode
- Click on "Load unpackaged"
- Navigate to this repo's `app` directory
- Have fun

## TODO

* Proper options page & better UI
* Gemini protocol registration?
* Gopher support