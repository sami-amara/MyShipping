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

var files = require('../lib/');

Object.keys(files).forEach(function (name) {
    Build.prototype[name] = files[name];
});

Build.filePlugins = Build.prototype.filePlugins;
