var assert = require('assert');

var join = require('path').join;

var resolve = require('component-resolver');

var Build = require('./build.js');

describe('example', function () {
    var fixture = join(__dirname, 'fixtures/example'),
        build, css;

    it('should resolve', function (done) {
        resolve(fixture, {
            install: true,
        }, function (err, tree) {
            if (err) return done(err);

            build = Build(tree);
            done();
        });
    });

    it('should build styles', function (done) {
        build.files(function (err, string) {
            if (err) return done(err);

            done();
        });
    });
});
