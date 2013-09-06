(function ($) {
    // http://lucassmith.name/pub/typeof.html   
    $.type = function (o) {
        var toS = Object.prototype.toString;
        var types = {
            'undefined': 'undefined',
            'number': 'number',
            'boolean': 'boolean',
            'string': 'string',
            '[object Function]': 'function',
            '[object RegExp]': 'regexp',
            '[object Array]': 'array',
            '[object Date]': 'date',
            '[object Error]': 'error'
        };
        return types[typeof o] || types[toS.call(o)] || (o ? 'object' : 'null');
    };
    // http://mootools.net   
    var $specialChars = { '\b': '\\b', '\t': '\\t', '\n': '\\n', '\f': '\\f', '\r': '\\r', '"': '\\"', '\\': '\\\\' };
    var $replaceChars = function (chr) {
        return $specialChars[chr] || '\\u00' + Math.floor(chr.charCodeAt() / 16).toString(16) + (chr.charCodeAt() % 16).toString(16);
    };
    $.toJSON = function (o) {
        var s = [];
        switch ($.type(o)) {
            case 'undefined':
                return 'undefined';
            case 'null':
                return 'null';
            case 'number':
            case 'boolean':
            case 'date':
            case 'function':
                return o.toString();
            case 'string':
                return '"' + o.replace(/[\x00-\x1f\\"]/g, $replaceChars) + '"';
            case 'array':
                for (var i = 0, l = o.length; i < l; i++) {
                    s.push($.toJSON(o[i]));
                }
                return '[' + s.join(',') + ']';
            case 'error':
            case 'object':
                for (var p in o) {
                    s.push(p + ':' + $.toJSON(o[p]));
                }
                return '{' + s.join(',') + '}';
            default:
                return '';
        }
    };
    $.evalJSON = function (s) {
        if ($.type(s) != 'string' || !s.length) return null;
        return eval('(' + s + ')');

    };
})(jQuery);









