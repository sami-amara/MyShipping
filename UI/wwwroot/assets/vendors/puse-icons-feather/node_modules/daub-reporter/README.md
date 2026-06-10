# reporter

Console reporting for [decanat-engine](https://github.com/decanat/engine).

## Installation

Using [component](https://github.com/component/component)

    $ component install decanat/engine-reporter

Using [npm](http://npmjs.org/) for Node and [browserify](http://browserify.org/)

    $ npm install decanat-engine-reporter

## Usage

```js
var report = require('decanat-engine-reporter');

report('Invalid source.', { path: '/views/index.html' })
```

## Test

Run tests with [mocha](http://visionmedia.github.io/mocha/)

    $ make test

## License

The MIT License.
