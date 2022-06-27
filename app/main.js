var $ = a => document.getElementById(a);
document.addEventListener('DOMContentLoaded', function () {
    chrome.storage.sync.get({
        defaultPage: 'gemini://gemini.circumlunar.space/',
        rewriteLinks: true,
        redirectLimit: 5,
        css: ''
    }, function(items) {
        $('defaultPage').value = items.defaultPage;
        $('rewriteLinks').checked = items.rewriteLinks;
        $('redirectLimit').value = items.redirectLimit;
        $('css').value = items.css;
    });
    $('save').addEventListener('click', function () {
        chrome.storage.sync.set({
            defaultPage: $('defaultPage').value,
            rewriteLinks: $('rewriteLinks').checked,
            redirectLimit: $('redirectLimit').value,
            css: $('css').value
        });
    });
    /*$('gemini_handler').addEventListener('click', function () {
        //navigator.registerProtocolHandler("web+gemini", "chrome-extension://"+chrome.runtime.id+"/handler.html#%s", "Gemini handler")
    });*/
    $('navig_submit').addEventListener('click', function () {
        window.open("/handler.html?"+$('navig').value, '_blank');
    });
});