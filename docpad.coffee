# DocPad Configuration File
# http://docpad.org/docs/config

cheerio = require('cheerio')

# Define the DocPad Configuration
docpadConfig = {
	templateData:
		# Specify some site properties
		site:
			# The production url of our website
			url: "http://www.ewal.net"

			# The default title of our website
			title: "Ewal.net"

			# The website author's name
			author: "Erv Walter"

			# The website author's email
			email: "erv@ewal.net"


		# -----------------------------
		# Helper Functions

		# Get the prepared site/document title
		# Often we would like to specify particular formatting to our page's title
		# we can apply that formatting here
		getPreparedTitle: ->
			# if we have a document title, then we should use that and suffix the site's title onto it
			if @document.title
				"#{@document.title} - #{@site.title}"
				# if our document does not have it's own title, then we should just use the site's title
			else
				@site.title

		getPageUrlWithHostname: ->
			"#{@site.url}#{@document.url}"

		fixLinks: (content) ->
			baseUrl = @site.url
			regex = /^(http|https|ftp|mailto):/

			$ = cheerio.load(content)
			$('img').each ->
				$img = $(@)
				src = $img.attr('src')
				$img.attr('src', baseUrl + src) unless regex.test(src)
			$('a').each ->
				$a = $(@)
				href = $a.attr('href')
				$a.attr('href', baseUrl + href) unless regex.test(href)
			$.html()

		moment: require('moment')

		# Discus.com settings
		disqusShortName: 'ewalnet'

		# Google+ settings
		googlePlusId: '103974853049200513652'

	outPath: 'out/development'

	collections:
		posts: ->
			@getCollection('documents').findAllLive({relativeDirPath: 'posts'}, [date: -1])

	environments:
		static:
			outPath: 'out/generated'
		development:
			collections:
				posts: ->
					@getCollection('documents').findAllLive({relativeDirPath: {'$in' : ['posts', 'drafts']}}, [relativeDirPath: 1,  date: -1])

	plugins:
		rss:
			collection: 'posts'
		tagging:
			collectionName: 'posts'
			indexPageLowercase: true
		dateurls:
			cleanurl: true
			trailingSlashes: true
			keepOriginalUrls: false
			collectionName: 'posts'
			dateIncludesTime: true
		paged:
			cleanurl: true
			startingPageNumber: 2
		cleanurls:
			trailingSlashes: true
}

# Export the DocPad Configuration
module.exports = docpadConfig