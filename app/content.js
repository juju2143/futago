chrome.storage.sync.get({
    rewriteLinks: true,
}, function(items) {
    if(items.rewriteLinks)
        Array.from(document.querySelectorAll('a')).forEach(a => {
            if(a.protocol == 'gemini:')
            {
                var loc = a.toString();

                a.protocol = 'chrome-extension:';
                a.host = chrome.runtime.id;
                a.pathname = '/handler.html';
                a.search = '?'+ loc;
            }
        });
});