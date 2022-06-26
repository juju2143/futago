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

## The extension

Compatible with the most popular browsers like Chrome, Firefox and Edge, it communicates with the host application to initiate a Gemini connection, as its unable to do it itself for security reasons, then parses the result and creates a webpage out of it.

## TODO

* Proper options page & better UI
* gemini protocol registration?