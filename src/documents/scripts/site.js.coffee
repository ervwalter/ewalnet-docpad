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