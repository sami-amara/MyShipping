var fs      = require('fs'),
    vm      = require('vm'),
    path    = require('path'),
    assert  = require('assert');

var resolve = require('component-resolver');
var Builder = require('faiton-builder');

var daub    = require('..');

describe('example', function () {
    var build;

    var fixture  = path.join(__dirname, 'fixtures/example'),
        expected = path.join(fixture, '../full');

    it('should resolve', function (done) {
        resolve(fixture, {
            install: true,
        }, function (err, tree) {
            if (err) return done(err);

            Builder.scripts(tree)
                .use('templates', daub({ scripts: true }))
                .end(function(err, string){
                    assert.equal(string.trim(), fs.readFileSync(expected, 'utf8').trim());
                    done();
                });
        });
    });
});
