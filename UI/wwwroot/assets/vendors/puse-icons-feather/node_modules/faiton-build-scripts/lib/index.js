var Build = require('faiton-builder');

var daub = require('faiton-build-daub');

var es6modules = require('builder-es6-module-to-cjs');

var canonical = Build.scripts.canonical,
    requirejs = Build.scripts.require;

var plugins = Build.plugins;

exports.scripts = function (done) {
    var tree    = this.tree,
        options = this.options;

    var build = Build.scripts(tree, options);

    var require = options.require != null
            ? options.require
            : true,
        autorequire = options.autorequire !== false;

    this.scriptPlugins(build, options);

    build.end(function (err, js) {
        if (err) return done(err);
        if (!js) return done(null, '');

        if (require)
            js = requirejs + js;
        if (autorequire)
            js += 'require("' + canonical(tree).canonical + '");\n';

        done(null, js);
    });
};

exports.scriptPlugins = function (build, options) {
    build
        .use('scripts',
            es6modules(options),
            plugins.js(options))
        .use('json',
            plugins.json(options))
        .use('templates',
            daub({ scripts: true }));
};
