document.addEventListener('DOMContentLoaded', function () {
    chrome.storage.sync.get({
        defaultPage: 'gemini://gemini.circumlunar.space/',
        redirectLimit: 5,
        css: '',
        faviconCacheTime: 3600000,
    }, main);
});

var options = null;
var url = null;
var parsedURL = null

function main(opts) {
    options = opts;

    url = location.search;
    if(url.indexOf("?") < 0 || url.length <= 1)
        location.search = "?"+options.defaultPage;
    else
        url = url.substring(1);

    try { parsedURL = new URL(url); }
    catch(e)
    {
        location.search = "?"+options.defaultPage;
        // TODO: go to search engine? tell user the url is wrong? try to figure it out?
    }

    var css = createNode('style', options.css);
    document.head.appendChild(css);
    document.title = url;

    var port = chrome.runtime.connectNative('ca.a39.futago');
    var header = "";
    var data = "";
    port.onMessage.addListener(function(response) {
        console.log(response);
        if(response.error)
        {
            addNode('h1', response.error);
            port.disconnect();
        }
        else if(response.header)
            header = response.header.trim();
        else if(response.data)
            data += response.data;
        else if(response.end)
        {
            parseGemini(header, data);
            port.disconnect();
        }
        else if(response.favicon !== undefined)
        {
            setFavicon(response.favicon);

            if(!localStorage.cache) localStorage.cache = '{}';
            var cache = JSON.parse(localStorage.cache);
            var newURL = parsedURL;    // copy parsed URL because
            newURL.protocol = 'http:'; // I shouldn't have to do this wtf
            cache[newURL.host] = {
                "favicon": response.favicon.trim(),
                "time": Date.now()
            }
            localStorage.cache = JSON.stringify(cache);
        }
    });
    port.onDisconnect.addListener(function() {
        // we probably got an error before we got to the end so let's dump what we have so far
        var err;
        if(err = chrome.runtime.lastError)
        {
            if(data.length > 0) parseGemini(header, data);
            else addNode('h1', err.message);
            console.error("Error: " + err.message);
        }
    });
    var fetchFavicon = true;
    if(localStorage.cache)
    {
        var cache = JSON.parse(localStorage.cache);
        var newURL = parsedURL;    // copy parsed URL because
        newURL.protocol = 'http:'; // I shouldn't have to do this wtf
        cachedIcon = cache[newURL.host];
        if(cachedIcon)
        if(Date.now() - cachedIcon.time < options.faviconCacheTime)
        {
            fetchFavicon = false;
            setFavicon(cachedIcon.favicon);
        }
    }
    port.postMessage({
        "url": url,
        "favicon": fetchFavicon
    }); // TODO: send client certificate here as well
    console.log("Started request to " + url);
}

function parseGemini(header, body)
{
    try
    {
        // TODO: support other protocols?
        var status = header.match(/^([0-9]{2})( (.*))?/);
    }
    catch(e)
    {
        addNode('h1', "00 Invalid protocol");
        return;
    }
    console.log(header);
    var code = status[1];
    var meta = status[3];
    if(code[0] != '3') delete sessionStorage.redirect;
    switch(code[0])
    {
        case "1": // 10 input, 11 password input
            addNode('h1', meta);
            var input = document.createElement('input');
            input.setAttribute('type', code=='11'?'password':'text');
            input.setAttribute('size', 50);
            document.body.appendChild(input);
            var button = createNode('button', "Submit");
            button.addEventListener('click', submit);
            document.body.appendChild(button);
            break;
        case "2": // success
            var mime = meta.split(";");
            var type = mime.shift();
            mime.forEach((v)=>{
                var t = v.trim().split('=');
                switch(t[0])
                {
                    case "lang":
                        document.querySelector('html').setAttribute("lang", t[1]);
                        break;
                    case "charset":
                        document.querySelector('meta').setAttribute("charset", t[1]);
                        break;
                }
            })
            var name = new URL(url).pathname.split("/").pop();
            var datauri = "data:"+meta+";name="+name+";base64,"+body;
            switch(type.trim())
            {
                case "text/gemini":
                    var page = decodeURIComponent(escape(atob(body)));
                    var lines = page.split("\n");
                    parseGmi(lines);
                    break;
                case "text/html":
                    var page = decodeURIComponent(escape(atob(body)));
                    document.open(meta, "replace");
                    document.write(page);
                    // TODO: rewrite links?
                    document.close();
                    break;
                // TODO: markdown? more types?
                default:
                    if(type.startsWith("text/"))
                    {
                        var page = decodeURIComponent(escape(atob(body)));
                        addNode('pre', page);
                    }
                    else if(type.startsWith("image/"))
                    {
                        var img = document.createElement('img');
                        img.src = datauri;
                        document.body.appendChild(img);
                    }
                    else if(type.startsWith("audio/"))
                    {
                        var audio = document.createElement('audio');
                        audio.setAttribute("controls", true);
                        audio.setAttribute("type", type);
                        audio.src = datauri;
                        document.body.appendChild(audio);
                    }
                    else if(type.startsWith("video/"))
                    {
                        var video = document.createElement('video');
                        audio.setAttribute("controls", true);
                        audio.setAttribute("type", type);
                        video.src = datauri;
                        document.body.appendChild(video);
                    }
                    else
                    {
                        /*var embed = document.createElement('iframe');
                        embed.setAttribute("src", "data:"+meta+";name="+name+";base64,"+body);
                        embed.setAttribute("type", meta);
                        embed.setAttribute("border", 0);
                        embed.setAttribute("width", "100%");
                        embed.setAttribute("height", "100%");
                        document.querySelector('html').style.height = "100%";
                        document.body.style.height = "100%";
                        document.body.style.margin = 0;
                        document.body.style.overflow = "hidden";
                        document.body.appendChild(embed);*/
                        chrome.downloads.download({
                            url: datauri,
                            saveAs: true,
                            filename: name
                        });
                    }
            }
            break;
        case "3": // redir 30 temp, 31 perm
            if(sessionStorage.redirect !== undefined)
                sessionStorage.redirect = 0;
            else
                ++sessionStorage.redirect;
            if(+sessionStorage.redirect >= +options.redirectLimit)
            {
                addNode('h1', code + " Too many redirects");
                addNodeA(resolveLink(meta, true), meta);
                delete sessionStorage.redirect;
            }
            else
                location = resolveLink(meta, true);
            break;
        default: // 4x, 5x and 6x codes
            addNode('h1', header);
    }
    // TODO: context menus
    //var context = chrome.contextMenus.create({title: "Save raw..."})
}

// TODO: put parsers in its own classes and make it rely less on document.body?
function parseGmi(lines)
{
    var currentPre = null;
    var currentUl = null;
    var title = null;
    lines.forEach(line => {
        if(currentPre == null)
        {
            if(currentUl != null && !line.startsWith('*'))
            {
                document.body.appendChild(currentUl);
                currentUl = null;
            }
            if(line.startsWith("```"))
            {
                var lang = line.substring(3).trim();
                currentPre = document.createElement('pre');
                if(lang.length > 0) currentPre.setAttribute("title", lang);
            }
            else if(line.startsWith("=>"))
            {
                var a = line.substring(2).trimStart().split(/[ \t]+/);
                var href = a.shift();
                var text = a.join(" ");
                if(text.trim().length == 0)
                    text = href;
                var newhref = resolveLink(href, true);
                addNodeA(newhref, text);
            }
            else if(m = line.match(/^#{1,3}/))
            {
                var l = m[0].length;
                addNode('h'+l, line.substring(l).trim(), true);
                if(l == 1 && title == null)
                    title = line.substring(l).trim();
            }                
            else if(line.startsWith(">"))
                addNode('blockquote', line.substring(1).trim());
            else if(line.startsWith("*"))
            {
                if(currentUl == null)
                    currentUl = document.createElement('ul');
                currentUl.appendChild(createNode('li', line.substring(1).trim()));
            }
            else if(line.trim().length > 0)
                addNode('p', line);
            //else
            //    document.body.appendChild(document.createElement('br'));
        }
        else
        {
            if(line.startsWith("```"))
            {
                var lang = currentPre.getAttribute("title");
                if(lang)
                {
                    lang = lang.toLowerCase();
                    if(lang == "ansi")
                    {
                        currentPre.className = "language-"+lang;
                        var text = currentPre.innerHTML;
                        var html = new AnsiUp().ansi_to_html(text);
                        currentPre.innerHTML = html;
                    }
                    else if(hljs.getLanguage(lang))
                    {
                        currentPre.className = "language-"+lang;
                        hljs.highlightElement(currentPre);
                    }
                }
                document.body.appendChild(currentPre);
                currentPre = null;
            }
            else
                currentPre.appendChild(document.createTextNode(line+"\n"));
        }
    });
    if(currentPre) document.body.appendChild(currentPre);
    if(currentUl) document.body.appendChild(currentUl);
    if(title != null) document.title = title;
}

function submit()
{
    var input = encodeURIComponent(document.querySelector('input').value);
    var theurl = new URL(url);
    theurl.search = "?"+input;
    location.search = "?"+theurl.toString();
}

function createNode(elem, txt, id)
{
    var node = document.createElement(elem);
    var text = document.createTextNode(txt);
    if(id)
    {
        var anchor = txt.trim().toLowerCase().replace(/ /g,"-").replace(/[^a-z0-9\-]/g, "");
        if(document.getElementById(anchor))
        {
            var i;
            for(i = -1; document.getElementById(anchor+i); i--) {}
            anchor = anchor+i;
        }
        node.id = anchor;
    }
    node.appendChild(text);
    return node;
}

function addNode(elem, txt, id)
{
    document.body.appendChild(createNode(elem, txt, id));
}

function addNodeA(href, txt)
{
    var node = createNode('a', txt);
    node.setAttribute('href', href);
    var p = document.createElement('p');
    p.appendChild(node);
    document.body.appendChild(p);
}

function resolveLink(href, friendly)
{
    var theurl;
    try
    {
        theurl = new URL(href, url);
    }
    catch(e)
    {
        theurl = new URL(href, url+"/");
    }
    var newhref = theurl.href;
    if(theurl.protocol == "gemini:" && friendly)
        newhref = "/handler.html?"+newhref;
    return newhref;
}

function setFavicon(text)
{
    var icon = createNode('div', text);
    twemoji.parse(icon);
    var img = icon.querySelector('img');
    if(img)
        document.head.querySelector('link[rel=icon]').href = img.src;
}