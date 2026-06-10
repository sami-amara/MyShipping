var resolve = require('component-resolver');
var join = require('path').join;
var assert = require('assert');
var vm = require('vm');

var Build = require('./build.js');

describe('example', function () {
  var build;

  var fixture = join(__dirname, 'fixtures/example');

  it('should resolve', function (done) {
    resolve(fixture, {
      install: true,
    }, function (err, tree) {
      if (err) return done(err);

      build = Build(tree);
      done();
    })
  })

  it('should build scripts', function (done) {
    build.scripts(function (err, js) {
      if (err) return done(err);

      var ctx = vm.createContext();
      vm.runInContext(js, ctx);
      done();
    })
  })

})
