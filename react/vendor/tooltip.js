/*!
 * tooltip 0.1.0 - 18th Dec 2013
 * https://github.com/darsain/tooltip
 *
 * Licensed under the MIT license.
 * http://opensource.org/licenses/MIT
 */
/**
 * Require the given path.
 *
 * @param {String} path
 * @return {Object} exports
 * @api public
 */

function require(path, parent, orig) {
  var resolved = require.resolve(path);

  // lookup failed
  if (null == resolved) {
    orig = orig || path;
    parent = parent || 'root';
    var err = new Error('Failed to require "' + orig + '" from "' + parent + '"');
    err.path = orig;
    err.parent = parent;
    err.require = true;
    throw err;
  }

  var module = require.modules[resolved];

  // perform real require()
  // by invoking the module's
  // registered function
  if (!module._resolving && !module.exports) {
    var mod = {};
    mod.exports = {};
    mod.client = mod.component = true;
    module._resolving = true;
    module.call(this, mod.exports, require.relative(resolved), mod);
    delete module._resolving;
    module.exports = mod.exports;
  }

  return module.exports;
}

/**
 * Registered modules.
 */

require.modules = {};

/**
 * Registered aliases.
 */

require.aliases = {};

/**
 * Resolve `path`.
 *
 * Lookup:
 *
 *   - PATH/index.js
 *   - PATH.js
 *   - PATH
 *
 * @param {String} path
 * @return {String} path or null
 * @api private
 */

require.resolve = function(path) {
  if (path.charAt(0) === '/') path = path.slice(1);

  var paths = [
    path,
    path + '.js',
    path + '.json',
    path + '/index.js',
    path + '/index.json'
  ];

  for (var i = 0; i < paths.length; i++) {
    var path = paths[i];
    if (require.modules.hasOwnProperty(path)) return path;
    if (require.aliases.hasOwnProperty(path)) return require.aliases[path];
  }
};

/**
 * Normalize `path` relative to the current path.
 *
 * @param {String} curr
 * @param {String} path
 * @return {String}
 * @api private
 */

require.normalize = function(curr, path) {
  var segs = [];

  if ('.' != path.charAt(0)) return path;

  curr = curr.split('/');
  path = path.split('/');

  for (var i = 0; i < path.length; ++i) {
    if ('..' == path[i]) {
      curr.pop();
    } else if ('.' != path[i] && '' != path[i]) {
      segs.push(path[i]);
    }
  }

  return curr.concat(segs).join('/');
};

/**
 * Register module at `path` with callback `definition`.
 *
 * @param {String} path
 * @param {Function} definition
 * @api private
 */

require.register = function(path, definition) {
  require.modules[path] = definition;
};

/**
 * Alias a module definition.
 *
 * @param {String} from
 * @param {String} to
 * @api private
 */

require.alias = function(from, to) {
  if (!require.modules.hasOwnProperty(from)) {
    throw new Error('Failed to alias "' + from + '", it does not exist');
  }
  require.aliases[to] = from;
};

/**
 * Return a require function relative to the `parent` path.
 *
 * @param {String} parent
 * @return {Function}
 * @api private
 */

require.relative = function(parent) {
  var p = require.normalize(parent, '..');

  /**
   * lastIndexOf helper.
   */

  function lastIndexOf(arr, obj) {
    var i = arr.length;
    while (i--) {
      if (arr[i] === obj) return i;
    }
    return -1;
  }

  /**
   * The relative require() itself.
   */

  function localRequire(path) {
    var resolved = localRequire.resolve(path);
    return require(resolved, parent, path);
  }

  /**
   * Resolve relative to the parent.
   */

  localRequire.resolve = function(path) {
    var c = path.charAt(0);
    if ('/' == c) return path.slice(1);
    if ('.' == c) return require.normalize(p, path);

    // resolve deps by returning
    // the dep in the nearest "deps"
    // directory
    var segs = parent.split('/');
    var i = lastIndexOf(segs, 'deps') + 1;
    if (!i) i = 0;
    path = segs.slice(0, i + 1).join('/') + '/deps/' + path;
    return path;
  };

  /**
   * Check if module is defined at `path`.
   */

  localRequire.exists = function(path) {
    return require.modules.hasOwnProperty(localRequire.resolve(path));
  };

  return localRequire;
};
require.register("darsain-event/index.js", Function("exports, require, module",
"'use strict';\n\
\n\
/**\n\
 * Bind `el` event `type` to `fn`.\n\
 *\n\
 * @param {Element}  el\n\
 * @param {String}   type\n\
 * @param {Function} fn\n\
 * @param {Boolean}  capture\n\
 *\n\
 * @return {Function}\n\
 */\n\
exports.bind = window.addEventListener ? function (el, type, fn, capture) {\n\
\tel.addEventListener(type, fn, capture || false);\n\
\treturn fn;\n\
} : function (el, type, fn) {\n\
\tvar fnid = type + fn;\n\
\tel[fnid] = el[fnid] || function () {\n\
\t\tvar event = window.event;\n\
\t\tevent.target = event.srcElement;\n\
\t\tevent.preventDefault = function () {\n\
\t\t\tevent.returnValue = false;\n\
\t\t};\n\
\t\tevent.stopPropagation = function () {\n\
\t\t\tevent.cancelBubble = true;\n\
\t\t};\n\
\t\tfn.call(el, event);\n\
\t};\n\
\tel.attachEvent('on' + type, el[fnid]);\n\
\treturn fn;\n\
};\n\
\n\
/**\n\
 * Unbind `el` event `type`'s callback `fn`.\n\
 *\n\
 * @param {Element}  el\n\
 * @param {String}   type\n\
 * @param {Function} fn\n\
 * @param {Boolean}  capture\n\
 *\n\
 * @return {Function}\n\
 */\n\
exports.unbind = window.removeEventListener ? function (el, type, fn, capture) {\n\
\tel.removeEventListener(type, fn, capture || false);\n\
\treturn fn;\n\
} : function (el, type, fn) {\n\
\tvar fnid = type + fn;\n\
\tel.detachEvent('on' + type, el[fnid]);\n\
\ttry {\n\
\t\tdelete el[fnid];\n\
\t} catch (err) {\n\
\t\t// can't delete window object properties\n\
\t\tel[fnid] = undefined;\n\
\t}\n\
\treturn fn;\n\
};//@ sourceURL=darsain-event/index.js"
));
require.register("darsain-position/index.js", Function("exports, require, module",
"'use strict';\n\
\n\
/**\n\
 * Transport.\n\
 */\n\
module.exports = position;\n\
\n\
/**\n\
 * Globals.\n\
 */\n\
var win = window;\n\
var doc = win.document;\n\
var docEl = doc.documentElement;\n\
\n\
/**\n\
 * Poor man's shallow object extend.\n\
 *\n\
 * @param {Object} a\n\
 * @param {Object} b\n\
 *\n\
 * @return {Object}\n\
 */\n\
function extend(a, b) {\n\
\tfor (var key in b) {\n\
\t\ta[key] = b[key];\n\
\t}\n\
\treturn a;\n\
}\n\
\n\
/**\n\
 * Checks whether object is window.\n\
 *\n\
 * @param {Object} obj\n\
 *\n\
 * @return {Boolean}\n\
 */\n\
function isWin(obj) {\n\
\treturn obj && obj.setInterval != null;\n\
}\n\
\n\
/**\n\
 * Returns element's object with `left`, `top`, `bottom`, `right`, `width`, and `height`\n\
 * properties indicating the position and dimensions of element on a page.\n\
 *\n\
 * @param {Element} element\n\
 *\n\
 * @return {Object}\n\
 */\n\
function position(element) {\n\
\tvar winTop = win.pageYOffset || docEl.scrollTop;\n\
\tvar winLeft = win.pageXOffset || docEl.scrollLeft;\n\
\tvar box = { left: 0, right: 0, top: 0, bottom: 0, width: 0, height: 0 };\n\
\n\
\tif (isWin(element)) {\n\
\t\tbox.width = win.innerWidth || docEl.clientWidth;\n\
\t\tbox.height = win.innerHeight || docEl.clientHeight;\n\
\t} else if (docEl.contains(element) && element.getBoundingClientRect != null) {\n\
\t\textend(box, element.getBoundingClientRect());\n\
\t\t// width & height don't exist in <IE9\n\
\t\tbox.width = box.right - box.left;\n\
\t\tbox.height = box.bottom - box.top;\n\
\t} else {\n\
\t\treturn box;\n\
\t}\n\
\n\
\tbox.top = box.top + winTop - docEl.clientTop;\n\
\tbox.left = box.left + winLeft - docEl.clientLeft;\n\
\tbox.right = box.left + box.width;\n\
\tbox.bottom = box.top + box.height;\n\
\n\
\treturn box;\n\
}//@ sourceURL=darsain-position/index.js"
));
require.register("component-classes/index.js", Function("exports, require, module",
"/**\n\
 * Module dependencies.\n\
 */\n\
\n\
var index = require('indexof');\n\
\n\
/**\n\
 * Whitespace regexp.\n\
 */\n\
\n\
var re = /\\s+/;\n\
\n\
/**\n\
 * toString reference.\n\
 */\n\
\n\
var toString = Object.prototype.toString;\n\
\n\
/**\n\
 * Wrap `el` in a `ClassList`.\n\
 *\n\
 * @param {Element} el\n\
 * @return {ClassList}\n\
 * @api public\n\
 */\n\
\n\
module.exports = function(el){\n\
  return new ClassList(el);\n\
};\n\
\n\
/**\n\
 * Initialize a new ClassList for `el`.\n\
 *\n\
 * @param {Element} el\n\
 * @api private\n\
 */\n\
\n\
function ClassList(el) {\n\
  if (!el) throw new Error('A DOM element reference is required');\n\
  this.el = el;\n\
  this.list = el.classList;\n\
}\n\
\n\
/**\n\
 * Add class `name` if not already present.\n\
 *\n\
 * @param {String} name\n\
 * @return {ClassList}\n\
 * @api public\n\
 */\n\
\n\
ClassList.prototype.add = function(name){\n\
  // classList\n\
  if (this.list) {\n\
    this.list.add(name);\n\
    return this;\n\
  }\n\
\n\
  // fallback\n\
  var arr = this.array();\n\
  var i = index(arr, name);\n\
  if (!~i) arr.push(name);\n\
  this.el.className = arr.join(' ');\n\
  return this;\n\
};\n\
\n\
/**\n\
 * Remove class `name` when present, or\n\
 * pass a regular expression to remove\n\
 * any which match.\n\
 *\n\
 * @param {String|RegExp} name\n\
 * @return {ClassList}\n\
 * @api public\n\
 */\n\
\n\
ClassList.prototype.remove = function(name){\n\
  if ('[object RegExp]' == toString.call(name)) {\n\
    return this.removeMatching(name);\n\
  }\n\
\n\
  // classList\n\
  if (this.list) {\n\
    this.list.remove(name);\n\
    return this;\n\
  }\n\
\n\
  // fallback\n\
  var arr = this.array();\n\
  var i = index(arr, name);\n\
  if (~i) arr.splice(i, 1);\n\
  this.el.className = arr.join(' ');\n\
  return this;\n\
};\n\
\n\
/**\n\
 * Remove all classes matching `re`.\n\
 *\n\
 * @param {RegExp} re\n\
 * @return {ClassList}\n\
 * @api private\n\
 */\n\
\n\
ClassList.prototype.removeMatching = function(re){\n\
  var arr = this.array();\n\
  for (var i = 0; i < arr.length; i++) {\n\
    if (re.test(arr[i])) {\n\
      this.remove(arr[i]);\n\
    }\n\
  }\n\
  return this;\n\
};\n\
\n\
/**\n\
 * Toggle class `name`.\n\
 *\n\
 * @param {String} name\n\
 * @return {ClassList}\n\
 * @api public\n\
 */\n\
\n\
ClassList.prototype.toggle = function(name){\n\
  // classList\n\
  if (this.list) {\n\
    this.list.toggle(name);\n\
    return this;\n\
  }\n\
\n\
  // fallback\n\
  if (this.has(name)) {\n\
    this.remove(name);\n\
  } else {\n\
    this.add(name);\n\
  }\n\
  return this;\n\
};\n\
\n\
/**\n\
 * Return an array of classes.\n\
 *\n\
 * @return {Array}\n\
 * @api public\n\
 */\n\
\n\
ClassList.prototype.array = function(){\n\
  var str = this.el.className.replace(/^\\s+|\\s+$/g, '');\n\
  var arr = str.split(re);\n\
  if ('' === arr[0]) arr.shift();\n\
  return arr;\n\
};\n\
\n\
/**\n\
 * Check if class `name` is present.\n\
 *\n\
 * @param {String} name\n\
 * @return {ClassList}\n\
 * @api public\n\
 */\n\
\n\
ClassList.prototype.has =\n\
ClassList.prototype.contains = function(name){\n\
  return this.list\n\
    ? this.list.contains(name)\n\
    : !! ~index(this.array(), name);\n\
};\n\
//@ sourceURL=component-classes/index.js"
));
require.register("component-indexof/index.js", Function("exports, require, module",
"module.exports = function(arr, obj){\n\
  if (arr.indexOf) return arr.indexOf(obj);\n\
  for (var i = 0; i < arr.length; ++i) {\n\
    if (arr[i] === obj) return i;\n\
  }\n\
  return -1;\n\
};//@ sourceURL=component-indexof/index.js"
));
require.register("tooltip/index.js", Function("exports, require, module",
"'use strict';\n\
\n\
/**\n\
 * Dependencies.\n\
 */\n\
var evt = require('event');\n\
var classes = require('classes');\n\
var indexOf = require('indexof');\n\
var position = require('position');\n\
\n\
/**\n\
 * Globals.\n\
 */\n\
var win = window;\n\
var doc = win.document;\n\
var body = doc.body;\n\
var verticalPlaces = ['top', 'bottom'];\n\
\n\
/**\n\
 * Transport.\n\
 */\n\
module.exports = Tooltip;\n\
\n\
/**\n\
 * Prototypal inheritance.\n\
 *\n\
 * @param {Object} o\n\
 *\n\
 * @return {Object}\n\
 */\n\
var objectCreate = Object.create || (function () {\n\
\tfunction F() {}\n\
\treturn function (o) {\n\
\t\tF.prototype = o;\n\
\t\treturn new F();\n\
\t};\n\
})();\n\
\n\
/**\n\
 * Poor man's shallow object extend.\n\
 *\n\
 * @param {Object} a\n\
 * @param {Object} b\n\
 *\n\
 * @return {Object}\n\
 */\n\
function extend(a, b) {\n\
\tfor (var key in b) {\n\
\t\ta[key] = b[key];\n\
\t}\n\
\treturn a;\n\
}\n\
\n\
/**\n\
 * Parse integer from strings like '-50px'.\n\
 *\n\
 * @param {Mixed} value\n\
 *\n\
 * @return {Integer}\n\
 */\n\
function parsePx(value) {\n\
\treturn 0 | Math.round(String(value).replace(/[^\\-0-9.]/g, ''));\n\
}\n\
\n\
/**\n\
 * Get computed style of element.\n\
 *\n\
 * @param {Element} element\n\
 *\n\
 * @type {String}\n\
 */\n\
var style = win.getComputedStyle ? function style(element, name) {\n\
\treturn win.getComputedStyle(element, null)[name];\n\
} : function style(element, name) {\n\
\treturn element.currentStyle[name];\n\
};\n\
\n\
/**\n\
 * Returns transition duration of element in ms.\n\
 *\n\
 * @param {Element} element\n\
 *\n\
 * @return {Int}\n\
 */\n\
function transitionDuration(element) {\n\
\tvar duration = String(style(element, transitionDuration.propName));\n\
\tvar match = duration.match(/([0-9.]+)([ms]{1,2})/);\n\
\tif (match) {\n\
\t\tduration = Number(match[1]);\n\
\t\tif (match[2] === 's') {\n\
\t\t\tduration *= 1000;\n\
\t\t}\n\
\t}\n\
\treturn 0|duration;\n\
}\n\
transitionDuration.propName = (function () {\n\
\tvar element = doc.createElement('div');\n\
\tvar names = ['transitionDuration', 'webkitTransitionDuration'];\n\
\tvar value = '1s';\n\
\tfor (var i = 0; i < names.length; i++) {\n\
\t\telement.style[names[i]] = value;\n\
\t\tif (element.style[names[i]] === value) {\n\
\t\t\treturn names[i];\n\
\t\t}\n\
\t}\n\
}());\n\
\n\
/**\n\
 * Tooltip construnctor.\n\
 *\n\
 * @param {String|Element} content\n\
 * @param {Object}         options\n\
 *\n\
 * @return {Tooltip}\n\
 */\n\
function Tooltip(content, options) {\n\
\tif (!(this instanceof Tooltip)) {\n\
\t\treturn new Tooltip(content, options);\n\
\t}\n\
\tthis.hidden = 1;\n\
\tthis.options = extend(objectCreate(Tooltip.defaults), options);\n\
\tthis._createElement();\n\
\tthis.content(content);\n\
}\n\
\n\
/**\n\
 * Creates a tooltip element.\n\
 *\n\
 * @return {Void}\n\
 */\n\
Tooltip.prototype._createElement = function () {\n\
\tthis.element = doc.createElement('div');\n\
\tthis.classes = classes(this.element);\n\
\tthis.classes.add(this.options.baseClass);\n\
\tvar propName;\n\
\tfor (var i = 0; i < Tooltip.classTypes.length; i++) {\n\
\t\tpropName = Tooltip.classTypes[i] + 'Class';\n\
\t\tif (this.options[propName]) {\n\
\t\t\tthis.classes.add(this.options[propName]);\n\
\t\t}\n\
\t}\n\
};\n\
\n\
/**\n\
 * Changes tooltip's type class type.\n\
 *\n\
 * @param {String} name\n\
 *\n\
 * @return {Tooltip}\n\
 */\n\
Tooltip.prototype.type = function (name) {\n\
\treturn this.changeClassType('type', name);\n\
};\n\
\n\
/**\n\
 * Changes tooltip's effect class type.\n\
 *\n\
 * @param {String} name\n\
 *\n\
 * @return {Tooltip}\n\
 */\n\
Tooltip.prototype.effect = function (name) {\n\
\treturn this.changeClassType('effect', name);\n\
};\n\
\n\
/**\n\
 * Changes class type.\n\
 *\n\
 * @param {String} propName\n\
 * @param {String} newClass\n\
 *\n\
 * @return {Tooltip}\n\
 */\n\
Tooltip.prototype.changeClassType = function (propName, newClass) {\n\
\tpropName += 'Class';\n\
\tif (this.options[propName]) {\n\
\t\tthis.classes.remove(this.options[propName]);\n\
\t}\n\
\tthis.options[propName] = newClass;\n\
\tif (newClass) {\n\
\t\tthis.classes.add(newClass);\n\
\t}\n\
\treturn this;\n\
};\n\
\n\
/**\n\
 * Updates tooltip's dimensions.\n\
 *\n\
 * @return {Tooltip}\n\
 */\n\
Tooltip.prototype.updateSize = function () {\n\
\tif (this.hidden) {\n\
\t\tthis.element.style.visibility = 'hidden';\n\
\t\tbody.appendChild(this.element);\n\
\t}\n\
\tthis.width = this.element.offsetWidth;\n\
\tthis.height = this.element.offsetHeight;\n\
\tif (this.spacing == null) {\n\
\t\tthis.spacing = this.options.spacing != null ? this.options.spacing : parsePx(style(this.element, 'top'));\n\
\t}\n\
\tif (this.hidden) {\n\
\t\tbody.removeChild(this.element);\n\
\t\tthis.element.style.visibility = '';\n\
\t} else {\n\
\t\tthis.position();\n\
\t}\n\
\treturn this;\n\
};\n\
\n\
/**\n\
 * Change tooltip content.\n\
 *\n\
 * When tooltip is visible, its size is automatically\n\
 * synced and tooltip correctly repositioned.\n\
 *\n\
 * @param {String|Element} content\n\
 *\n\
 * @return {Tooltip}\n\
 */\n\
Tooltip.prototype.content = function (content) {\n\
\tif (typeof content === 'object') {\n\
\t\tthis.element.innerHTML = '';\n\
\t\tthis.element.appendChild(content);\n\
\t} else {\n\
\t\tthis.element.innerHTML = content;\n\
\t}\n\
\tthis.updateSize();\n\
\treturn this;\n\
};\n\
\n\
/**\n\
 * Pick new place tooltip should be displayed at.\n\
 *\n\
 * When the tooltip is visible, it is automatically positioned there.\n\
 *\n\
 * @param {String} place\n\
 *\n\
 * @return {Tooltip}\n\
 */\n\
Tooltip.prototype.place = function (place) {\n\
\tthis.options.place = place;\n\
\tif (!this.hidden) {\n\
\t\tthis.position();\n\
\t}\n\
\treturn this;\n\
};\n\
\n\
/**\n\
 * Attach tooltip to an element.\n\
 *\n\
 * @param {Element} element\n\
 *\n\
 * @return {Tooltip}\n\
 */\n\
Tooltip.prototype.attach = function (element) {\n\
\tthis.attachedTo = element;\n\
\tif (!this.hidden) {\n\
\t\tthis.position();\n\
\t}\n\
\treturn this;\n\
};\n\
\n\
/**\n\
 * Detach tooltip from element.\n\
 *\n\
 * @return {Tooltip}\n\
 */\n\
Tooltip.prototype.detach = function () {\n\
\tthis.hide();\n\
\tthis.attachedTo = null;\n\
\treturn this;\n\
};\n\
\n\
/**\n\
 * Pick the most reasonable place for target position.\n\
 *\n\
 * @param {Object} target\n\
 *\n\
 * @return {Tooltip}\n\
 */\n\
Tooltip.prototype._pickPlace = function (target) {\n\
\tif (!this.options.auto) {\n\
\t\treturn this.options.place;\n\
\t}\n\
\tvar winPos = position(win);\n\
\tvar place = this.options.place.split('-');\n\
\tvar spacing = this.spacing;\n\
\n\
\tif (~indexOf(verticalPlaces, place[0])) {\n\
\t\tif (target.top - this.height - spacing <= winPos.top) {\n\
\t\t\tplace[0] = 'bottom';\n\
\t\t} else if (target.bottom + this.height + spacing >= winPos.bottom) {\n\
\t\t\tplace[0] = 'top';\n\
\t\t}\n\
\t\tswitch (place[1]) {\n\
\t\t\tcase 'left':\n\
\t\t\t\tif (target.right - this.width <= winPos.left) {\n\
\t\t\t\t\tplace[1] = 'right';\n\
\t\t\t\t}\n\
\t\t\t\tbreak;\n\
\t\t\tcase 'right':\n\
\t\t\t\tif (target.left + this.width >= winPos.right) {\n\
\t\t\t\t\tplace[1] = 'left';\n\
\t\t\t\t}\n\
\t\t\t\tbreak;\n\
\t\t\tdefault:\n\
\t\t\t\tif (target.left + target.width / 2 + this.width / 2 >= winPos.right) {\n\
\t\t\t\t\tplace[1] = 'left';\n\
\t\t\t\t} else if (target.right - target.width / 2 - this.width / 2 <= winPos.left) {\n\
\t\t\t\t\tplace[1] = 'right';\n\
\t\t\t\t}\n\
\t\t}\n\
\t} else {\n\
\t\tif (target.left - this.width - spacing <= winPos.left) {\n\
\t\t\tplace[0] = 'right';\n\
\t\t} else if (target.right + this.width + spacing >= winPos.right) {\n\
\t\t\tplace[0] = 'left';\n\
\t\t}\n\
\t\tswitch (place[1]) {\n\
\t\t\tcase 'top':\n\
\t\t\t\tif (target.bottom - this.height <= winPos.top) {\n\
\t\t\t\t\tplace[1] = 'bottom';\n\
\t\t\t\t}\n\
\t\t\t\tbreak;\n\
\t\t\tcase 'bottom':\n\
\t\t\t\tif (target.top + this.height >= winPos.bottom) {\n\
\t\t\t\t\tplace[1] = 'top';\n\
\t\t\t\t}\n\
\t\t\t\tbreak;\n\
\t\t\tdefault:\n\
\t\t\t\tif (target.top + target.height / 2 + this.height / 2 >= winPos.bottom) {\n\
\t\t\t\t\tplace[1] = 'top';\n\
\t\t\t\t} else if (target.bottom - target.height / 2 - this.height / 2 <= winPos.top) {\n\
\t\t\t\t\tplace[1] = 'bottom';\n\
\t\t\t\t}\n\
\t\t}\n\
\t}\n\
\n\
\treturn place.join('-');\n\
};\n\
\n\
/**\n\
 * Position the element to an element or a specific coordinates.\n\
 *\n\
 * @param {Integer|Element} x\n\
 * @param {Integer}         y\n\
 *\n\
 * @return {Tooltip}\n\
 */\n\
Tooltip.prototype.position = function (x, y) {\n\
\tif (this.attachedTo) {\n\
\t\tx = this.attachedTo;\n\
\t}\n\
\tif (x == null && this._p) {\n\
\t\tx = this._p[0];\n\
\t\ty = this._p[1];\n\
\t} else {\n\
\t\tthis._p = arguments;\n\
\t}\n\
\tvar target = typeof x === 'number' ? {\n\
\t\tleft: 0|x,\n\
\t\tright: 0|x,\n\
\t\ttop: 0|y,\n\
\t\tbottom: 0|y,\n\
\t\twidth: 0,\n\
\t\theight: 0\n\
\t} : position(x);\n\
\tvar spacing = this.spacing;\n\
\tvar newPlace = this._pickPlace(target);\n\
\n\
\t// Add/Change place class when necessary\n\
\tif (newPlace !== this.curPlace) {\n\
\t\tif (this.curPlace) {\n\
\t\t\tthis.classes.remove(this.curPlace);\n\
\t\t}\n\
\t\tthis.classes.add(newPlace);\n\
\t\tthis.curPlace = newPlace;\n\
\t}\n\
\n\
\t// Position the tip\n\
\tvar top, left;\n\
\tswitch (this.curPlace) {\n\
\t\tcase 'top':\n\
\t\t\ttop = target.top - this.height - spacing;\n\
\t\t\tleft = target.left + target.width / 2 - this.width / 2;\n\
\t\t\tbreak;\n\
\t\tcase 'top-left':\n\
\t\t\ttop = target.top - this.height - spacing;\n\
\t\t\tleft = target.right - this.width;\n\
\t\t\tbreak;\n\
\t\tcase 'top-right':\n\
\t\t\ttop = target.top - this.height - spacing;\n\
\t\t\tleft = target.left;\n\
\t\t\tbreak;\n\
\n\
\t\tcase 'bottom':\n\
\t\t\ttop = target.bottom + spacing;\n\
\t\t\tleft = target.left + target.width / 2 - this.width / 2;\n\
\t\t\tbreak;\n\
\t\tcase 'bottom-left':\n\
\t\t\ttop = target.bottom + spacing;\n\
\t\t\tleft = target.right - this.width;\n\
\t\t\tbreak;\n\
\t\tcase 'bottom-right':\n\
\t\t\ttop = target.bottom + spacing;\n\
\t\t\tleft = target.left;\n\
\t\t\tbreak;\n\
\n\
\t\tcase 'left':\n\
\t\t\ttop = target.top + target.height / 2 - this.height / 2;\n\
\t\t\tleft = target.left - this.width - spacing;\n\
\t\t\tbreak;\n\
\t\tcase 'left-top':\n\
\t\t\ttop = target.bottom - this.height;\n\
\t\t\tleft = target.left - this.width - spacing;\n\
\t\t\tbreak;\n\
\t\tcase 'left-bottom':\n\
\t\t\ttop = target.top;\n\
\t\t\tleft = target.left - this.width - spacing;\n\
\t\t\tbreak;\n\
\n\
\t\tcase 'right':\n\
\t\t\ttop = target.top + target.height / 2 - this.height / 2;\n\
\t\t\tleft = target.right + spacing;\n\
\t\t\tbreak;\n\
\t\tcase 'right-top':\n\
\t\t\ttop = target.bottom - this.height;\n\
\t\t\tleft = target.right + spacing;\n\
\t\t\tbreak;\n\
\t\tcase 'right-bottom':\n\
\t\t\ttop = target.top;\n\
\t\t\tleft = target.right + spacing;\n\
\t\t\tbreak;\n\
\t}\n\
\n\
\t// Set tip position & class\n\
\tthis.element.style.top = Math.round(top) + 'px';\n\
\tthis.element.style.left = Math.round(left) + 'px';\n\
\n\
\treturn this;\n\
};\n\
\n\
/**\n\
 * Show the tooltip.\n\
 *\n\
 * @param {Integer|Element} x\n\
 * @param {Integer}         y\n\
 *\n\
 * @return {Tooltip}\n\
 */\n\
Tooltip.prototype.show = function (x, y) {\n\
\tx = this.attachedTo ? this.attachedTo : x;\n\
\n\
\t// Clear potential ongoing animation\n\
\tclearTimeout(this.aIndex);\n\
\n\
\t// Position the element when requested\n\
\tif (x != null) {\n\
\t\tthis.position(x, y);\n\
\t}\n\
\n\
\t// Stop here if tip is already visible\n\
\tif (this.hidden) {\n\
\t\tthis.hidden = 0;\n\
\t\tbody.appendChild(this.element);\n\
\t}\n\
\n\
\t// Make tooltip aware of window resize\n\
\tif (this.attachedTo) {\n\
\t\tthis._aware();\n\
\t}\n\
\n\
\t// Trigger layout and kick in the transition\n\
\tif (this.options.inClass) {\n\
\t\tif (this.options.effectClass) {\n\
\t\t\tvoid this.element.clientHeight;\n\
\t\t}\n\
\t\tthis.classes.add(this.options.inClass);\n\
\t}\n\
\n\
\treturn this;\n\
};\n\
\n\
/**\n\
 * Hide the tooltip.\n\
 *\n\
 * @return {Tooltip}\n\
 */\n\
Tooltip.prototype.hide = function () {\n\
\tif (this.hidden) {\n\
\t\treturn;\n\
\t}\n\
\n\
\tvar self = this;\n\
\tvar duration = 0;\n\
\n\
\t// Remove .in class and calculate transition duration if any\n\
\tif (this.options.inClass) {\n\
\t\tthis.classes.remove(this.options.inClass);\n\
\t\tif (this.options.effectClass) {\n\
\t\t\tduration = transitionDuration(this.element);\n\
\t\t}\n\
\t}\n\
\n\
\t// Remove tip from window resize awareness\n\
\tif (this.attachedTo) {\n\
\t\tthis._unaware();\n\
\t}\n\
\n\
\t// Remove the tip from the DOM when transition is done\n\
\tclearTimeout(this.aIndex);\n\
\tthis.aIndex = setTimeout(function () {\n\
\t\tself.aIndex = 0;\n\
\t\tbody.removeChild(self.element);\n\
\t\tself.hidden = 1;\n\
\t}, duration);\n\
\n\
\treturn this;\n\
};\n\
\n\
Tooltip.prototype.toggle = function (x, y) {\n\
\treturn this[this.hidden ? 'show' : 'hide'](x, y);\n\
};\n\
\n\
Tooltip.prototype.destroy = function () {\n\
\tclearTimeout(this.aIndex);\n\
\tthis._unaware();\n\
\tif (!this.hidden) {\n\
\t\tbody.removeChild(this.element);\n\
\t}\n\
\tthis.element = this.options = null;\n\
};\n\
\n\
/**\n\
 * Make the tip window resize aware.\n\
 *\n\
 * @return {Void}\n\
 */\n\
Tooltip.prototype._aware = function () {\n\
\tvar index = indexOf(Tooltip.winAware, this);\n\
\tif (!~index) {\n\
\t\tTooltip.winAware.push(this);\n\
\t}\n\
};\n\
\n\
/**\n\
 * Remove the window resize awareness.\n\
 *\n\
 * @return {Void}\n\
 */\n\
Tooltip.prototype._unaware = function () {\n\
\tvar index = indexOf(Tooltip.winAware, this);\n\
\tif (~index) {\n\
\t\tTooltip.winAware.splice(index, 1);\n\
\t}\n\
};\n\
\n\
/**\n\
 * Handles repositioning of tooltips on window resize.\n\
 *\n\
 * @return {Void}\n\
 */\n\
Tooltip.reposition = (function () {\n\
\tvar rAF = window.requestAnimationFrame || window.webkitRequestAnimationFrame || function (fn) {\n\
\t\treturn setTimeout(fn, 17);\n\
\t};\n\
\tvar rIndex;\n\
\n\
\tfunction requestReposition() {\n\
\t\tif (rIndex || !Tooltip.winAware.length) {\n\
\t\t\treturn;\n\
\t\t}\n\
\t\trIndex = rAF(reposition, 17);\n\
\t}\n\
\n\
\tfunction reposition() {\n\
\t\trIndex = 0;\n\
\t\tvar tip;\n\
\t\tfor (var i = 0, l = Tooltip.winAware.length; i < l; i++) {\n\
\t\t\ttip = Tooltip.winAware[i];\n\
\t\t\ttip.position();\n\
\t\t}\n\
\t}\n\
\n\
\treturn requestReposition;\n\
}());\n\
Tooltip.winAware = [];\n\
\n\
// Bind winAware repositioning to window resize event\n\
evt.bind(window, 'resize', Tooltip.reposition);\n\
evt.bind(window, 'scroll', Tooltip.reposition);\n\
\n\
/**\n\
 * Array with dynamic class types.\n\
 *\n\
 * @type {Array}\n\
 */\n\
Tooltip.classTypes = ['type', 'effect'];\n\
\n\
/**\n\
 * Default options for Tooltip constructor.\n\
 *\n\
 * @type {Object}\n\
 */\n\
Tooltip.defaults = {\n\
\tbaseClass:   'tooltip', // Base tooltip class name.\n\
\ttypeClass:   null,      // Type tooltip class name.\n\
\teffectClass: null,      // Effect tooltip class name.\n\
\tinClass:     'in',      // Class used to transition stuff in.\n\
\tplace:       'top',     // Default place.\n\
\tspacing:     null,      // Gap between target and tooltip.\n\
\tauto:        0          // Whether to automatically adjust place to fit into window.\n\
};//@ sourceURL=tooltip/index.js"
));








require.alias("darsain-event/index.js", "tooltip/deps/event/index.js");
require.alias("darsain-event/index.js", "event/index.js");

require.alias("darsain-position/index.js", "tooltip/deps/position/index.js");
require.alias("darsain-position/index.js", "position/index.js");

require.alias("component-classes/index.js", "tooltip/deps/classes/index.js");
require.alias("component-classes/index.js", "classes/index.js");
require.alias("component-indexof/index.js", "component-classes/deps/indexof/index.js");

require.alias("component-indexof/index.js", "tooltip/deps/indexof/index.js");
require.alias("component-indexof/index.js", "indexof/index.js");
if (typeof exports == "object") {
  module.exports = require("tooltip");
} else if (typeof define == "function" && define.amd) {
  define(function(){ return require("tooltip"); });
} else {
  this["Tooltip"] = require("tooltip");
};