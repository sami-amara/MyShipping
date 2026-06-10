var assert = require('assert');

var join = require('path').join;

var resolve = require('component-resolver');

var Build = require('./build.js');

describe('example', function () {
    var fixture = join(__dirname, 'fixtures/example'),
        build, css;

    it('should resolve', function (done) {
        resolve(fixture, {
            install: true
        }, function (err, tree) {
            if (err) return done(err);

            build = Build(tree);

            done();
        });
    });

    it('should build styles', function (done) {
        build.styles(function (err, string) {
            if (err) return done(err);

            css = string;
            done();
        });
    });

    it('should autoprefix', function (){
        assert(~css.indexOf('-webkit-flex'));
    });

    it('should post-process with Rework', function(){
        // variables
        assert(~css.indexOf('body{color:#313131}'));
        assert(~css.indexOf('div{color:#F37561}'));
        // pseudos
        assert(~css.indexOf('::-moz-selection'));
        // custom media
        assert(~css.indexOf('@media screen and (max-width:30em){div{width:50px}}'));

        // remove comments
        assert(!~css.indexOf('/* A comment to be removed */'));
    });
});
