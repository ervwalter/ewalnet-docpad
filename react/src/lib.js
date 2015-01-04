var __indexOf = [].indexOf || function(item) { for (var i = 0, l = this.length; i < l; i++) { if (i in this && this[i] === item) return i; } return -1; };

if (!Array.isArray) {
	Array.isArray = function(arg) {
		return Object.prototype.toString.call(arg) === "[object Array]";
	};
}

String.prototype.trimStart = function(c) {
	var i, _ref;
	if (this.length === 0) {
		return this;
	}
	c = (c != null ? (Array.isArray(c) ? c : [c]) : [' ']);
	i = 0;
	while ((_ref = this.charAt(i), __indexOf.call(c, _ref) >= 0) && i < this.length) {
		i++;
	}
	return this.substring(i);
};

String.prototype.trimEnd = function(c) {
	var i, _ref;
	c = (c != null ? (Array.isArray(c) ? c : [c]) : [' ']);
	i = this.length - 1;
	while (i >= 0 && (_ref = this.charAt(i), __indexOf.call(c, _ref) >= 0)) {
		i--;
	}
	return this.substring(0, i + 1);
};

String.prototype.trim = function(c) {
	return this.trimStart(c).trimEnd(c);
};

if (typeof String.prototype.startsWith !== 'function') {
	String.prototype.startsWith = function(str) {
		return this.slice(0, str.length) === str;
	};
}

if (typeof String.prototype.endsWith !== 'function') {
	String.prototype.endsWith = function(str) {
		return this.slice(-str.length) === str;
	};
}