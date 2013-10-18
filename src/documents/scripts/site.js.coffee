htmlEncode = (value) ->
	$('<div/>').text(value).html()

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
		try
			codeIndex++
			$code = $(this)
			$pre = $code.parent()

			# compile into javascript
			coffeeSource = $code.text()
			jsSource = CoffeeScript.compile(coffeeSource, {bare: true})

			# add the markup to create the tabbed display
			$tabContent = $pre.wrap("<div class='tab-content'><div class='tab-pane active' id='code-#{codeIndex}-coffee'></div></div>").parent().parent()
			$("<ul class='nav nav-tabs auto-coffee'><li class='active'><a href='#code-#{codeIndex}-coffee' data-toggle='tab'>CoffeeScript</a></li><li><a href='#code-#{codeIndex}-js' data-toggle='tab'>JavaScript</a></li></ul>").insertBefore($tabContent)

			# add the javascript code block
			$tabContent.append("<div class='tab-pane' id='code-#{codeIndex}-js'><pre><code class='lang-javascript'>#{htmlEncode(jsSource)}</code></pre></div>")
		catch e
			# absorb exceptions, usually coffeescript compilation errors

	$('.lang-coffeescript-nojs').removeClass('lang-coffeescript-nojs').addClass('lang-coffeescript')
	$('.lang-none').removeClass('lang-none').addClass('lang-no-highlight')

	$('pre code').each (index, element) ->
		$code = $(this)
		classes = $code.attr('class')?.split(' ')
		if classes? then for origClass in classes
			fixedClass = origClass.replace /^lang-/, 'language-'
			$code.removeClass(origClass).addClass(fixedClass) if fixedClass isnt origClass
		try
			hljs.highlightBlock(element)
		catch e
			# absorb any problems, usually with older browsers