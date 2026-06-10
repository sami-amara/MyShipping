/**
 * Expose Build.
 */

module.exports = Build;

function Build(tree, options) {
    if (!(this instanceof Build))
        return new Build(tree, options);

    this.tree = tree;
    this.options = options || {};
}

Build.prototype.set = function (key, value) {
    this.options[key] = value;
    return this;
}

var styles = require('../lib/');

Object.keys(styles).forEach(function (name) {
    Build.prototype[name] = styles[name];
});

Build.stylePlugins = Build.prototype.stylePlugins;
