/**
 * Dependencies.
 */

var rework  = require('rework'),
    vars    = require('rework-vars'),
    pseudos = require('rework-pseudos'),
    media   = require('rework-custom-media');

/**
 * Post-process final CSS with Rework.
 *
 * @param  {String} css
 * @param  {Object} options [optional]
 * @return {String}
 */

module.exports = function (css, options) {
    options = options || {};

    return rework(css)
        .use(vars())
        .use(pseudos())
        .use(media)
        .toString();
};
