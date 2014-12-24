(function() {
  var htmlEncode,
    __indexOf = [].indexOf || function(item) { for (var i = 0, l = this.length; i < l; i++) { if (i in this && this[i] === item) return i; } return -1; };

  String.prototype.trimStart = function(c) {
    var i, _ref;
    if (this.length === 0) {
      return this;
    }
    c = (c != null ? (_.isArray(c) ? c : [c]) : [' ']);
    i = 0;
    while ((_ref = this.charAt(i), __indexOf.call(c, _ref) >= 0) && i < this.length) {
      i++;
    }
    return this.substring(i);
  };

  String.prototype.trimEnd = function(c) {
    var i, _ref;
    c = (c != null ? (_.isArray(c) ? c : [c]) : [' ']);
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

  htmlEncode = function(value) {
    return $('<div/>').text(value).html();
  };

  $(function() {
    var codeIndex;
    $('.post img').each(function() {
      var $el;
      $el = $(this);
      return $el.addClass('img-responsive');
    });
    $(".post,.project").each(function(i) {
      var _i;
      _i = i;
      return $(this).find('img.fancybox').each(function() {
        var $img, classes, title;
        $img = $(this);
        title = $img.attr('title');
        classes = $img.attr('class');
        $img.removeAttr('class');
        $img.wrap('<a href="' + this.src + '" class="' + classes + '" data-fancybox-group="post-' + _i + '" />');
        if (title != null) {
          return $img.parent().attr('title', title);
        }
      });
    });
    $(".fancybox").fancybox();
    codeIndex = 0;
    $('pre code.lang-coffeescript').each(function() {
      var $code, $pre, $tabContent, coffeeSource, e, jsSource;
      try {
        codeIndex++;
        $code = $(this);
        $pre = $code.parent();
        coffeeSource = $code.text();
        jsSource = CoffeeScript.compile(coffeeSource, {
          bare: true
        });
        $tabContent = $pre.wrap("<div class='tab-content'><div class='tab-pane active' id='code-" + codeIndex + "-coffee'></div></div>").parent().parent();
        $("<ul class='nav nav-tabs auto-coffee'><li class='active'><a href='#code-" + codeIndex + "-coffee' data-toggle='tab'>CoffeeScript</a></li><li><a href='#code-" + codeIndex + "-js' data-toggle='tab'>JavaScript</a></li></ul>").insertBefore($tabContent);
        return $tabContent.append("<div class='tab-pane' id='code-" + codeIndex + "-js'><pre><code class='lang-javascript'>" + (htmlEncode(jsSource)) + "</code></pre></div>");
      } catch (_error) {
        e = _error;
      }
    });
    $('.lang-coffeescript-nojs').removeClass('lang-coffeescript-nojs').addClass('lang-coffeescript');
    $('.lang-none').removeClass('lang-none').addClass('lang-no-highlight');
    return $('pre code').each(function(index, element) {
      var $code, classes, e, fixedClass, origClass, _i, _len, _ref;
      $code = $(this);
      classes = (_ref = $code.attr('class')) != null ? _ref.split(' ') : void 0;
      if (classes != null) {
        for (_i = 0, _len = classes.length; _i < _len; _i++) {
          origClass = classes[_i];
          fixedClass = origClass.replace(/^lang-/, 'language-');
          if (fixedClass !== origClass) {
            $code.removeClass(origClass).addClass(fixedClass);
          }
        }
      }
      try {
        return hljs.highlightBlock(element);
      } catch (_error) {
        e = _error;
      }
    });
  });

  (function($) {
    return $.fn.scrollTo = function(padding) {
      var top;
      if (padding == null) {
        padding = 20;
      }
      top = $(this).offset().top;
      top -= padding;
      if (top < 0) {
        top = 0;
      }
      $("html, body").animate({
        scrollTop: top + "px"
      }, "fast");
      return this;
    };
  })(jQuery);

}).call(this);
