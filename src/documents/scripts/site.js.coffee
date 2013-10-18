$ ->
	$('.post img').each ->
		$el = $(this)
		$el.addClass('img-responsive')

	# fix simple fancy img tags. wrap them with links as required by fancybox.js
	$(".post,.project").each (i) ->
		_i = i
		$(@).find('img.fancybox').each ->
			$img = $(this)
			title = $img.attr('title')
			classes = $img.attr('class')
			$img.removeAttr('class')
			$img.wrap('<a href="' + @src + '" class="' + classes + '" data-fancybox-group="post-' + _i + '" />')
			$img.parent().attr('title', title) if title?

	$(".fancybox").fancybox();

	codeIndex = 0

	$('pre code.lang-coffeescript').each ->
		codeIndex++
		$code = $(this)
		$pre = $code.parent()
		coffee = $code.text()
		$content = $pre.wrap("<div class='tab-content'><div class='tab-pane active' id='code-#{codeIndex}-coffee'></div></div>").parent().parent()

		js = CoffeeScript.compile(coffee, {bare: true})
		$content.append("<div class='tab-pane' id='code-#{codeIndex}-js'><pre><code class='lang-javascript'>" + js + "</code></pre></div>")
		$("<ul class='nav nav-tabs auto-coffee'><li class='active'><a href='#code-#{codeIndex}-coffee' data-toggle='tab'>CoffeeScript</a></li><li><a href='#code-#{codeIndex}-js' data-toggle='tab'>JavaScript</a></li></ul>").insertBefore($content)

	$('.lang-coffeescript-nojs').removeClass('lang-coffeescript-nojs').addClass('lang-coffeescript')

	$('pre code').each (i, e) ->
		hljs.highlightBlock(e)