var assert = require('assert');

var report = require('..');

describe('reporter', function(){
    it('should log', function(){
        assert.equal(report('Something.'), void 0);
        assert.equal(report('Something.', {
            source : 'Hey\n Something',
            index  : 8
        }), void 0);
    });
});
