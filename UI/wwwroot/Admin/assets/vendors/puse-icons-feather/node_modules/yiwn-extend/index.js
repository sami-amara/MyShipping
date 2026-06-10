/**
 * Expose `extend`.
 */

module.exports = extend;


/**
 * Merge attributes from provided objects.
 * 
 * @param  {Object|Null} target
 * @return {Object}
 */

function extend (target) {
    target = target || {};
    // Takes an unlimited number of extenders.
    var args = Array.prototype.slice.call(arguments, 1),
        source, i = 0;

    // For each extender, copy their properties on our object.
    while (source = args[i++]) {
        if (!source) continue;
        for (var property in source)
            target[property] = source[property];
    }

    return target;
};