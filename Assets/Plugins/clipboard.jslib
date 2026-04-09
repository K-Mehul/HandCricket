mergeInto(LibraryManager.library, {
    CopyTextToClipboard: function (text) {
        var str = UTF8ToString(text);
        var el = document.createElement('textarea');
        el.value = str;
        document.body.appendChild(el);
        el.select();
        document.execCommand('copy');
        document.body.removeChild(el);
    }
});
