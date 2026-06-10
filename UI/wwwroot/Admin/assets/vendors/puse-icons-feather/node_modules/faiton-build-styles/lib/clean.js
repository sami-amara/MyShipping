var Clean  = require('clean-css');

var extend = require('yiwn-extend');

var defaults = {
        keepSpecialComments: 0
    };

module.exports = function(css, options) {
    options = extend({}, defaults, options);

    // skip compression for development builds
    if (options.dev || options.development)
        return css;

    var clean = new Clean(options);

    return clean.minify(css);
};

