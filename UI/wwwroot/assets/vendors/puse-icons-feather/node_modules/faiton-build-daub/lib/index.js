var daub = require('daub');

module.exports = function (options) {
    options = options || {};

    return function (file, done) {
        if ('string' in file)
            return setImmediate(done);

        daub.readFile(file.filename, file.branch, function(err, markup){
            if (err) return done(err);

            if (options.scripts)
                file.string = JSON.stringify(markup),
                file.define = true;
            else
                file.string = markup;

            done();
        });
    };
};
