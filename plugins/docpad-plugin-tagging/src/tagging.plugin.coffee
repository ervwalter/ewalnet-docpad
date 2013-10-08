module.exports = (BasePlugin) ->

	_ = require('lodash')
	balUtil = require('bal-util')

	class Tagging extends BasePlugin
		name: 'tagging'

		config:
			collectionName: 'documents'
			indexPageLayout: 'tags'
			indexPagePath: 'tags'
			indexPageLowercase: false
			getTagWeight: (count, maxCount) ->
				# apply logarithmic weight algorithm
				logmin = 0
				logmax = Math.log(maxCount)
				result = (Math.log(count) - logmin) / (logmax - logmin)
				return result

		tagCloud: null
		tagCollection: null
		maxCount: 0

		# This is to prevent/detect recursive firings of ContextualizeAfter event
		contextualizeAfterLock: false

		extendCollections: (next) ->
			@tagCollection = @docpad.getDatabase().createLiveChildCollection()
							.setQuery("isTagIndex", tag: $exists: true)

		extendTemplateData: ({templateData}) ->
			me = @
			templateData.getTagCloud = ->
				return me.tagCloud
			templateData.getTagUrl = (tag) ->
 				return me.getTagUrl(tag)
			@

		contextualizeAfter: ({collection, templateData}, next) ->
			if not @contextualizeAfterLock
				return @generateTags(collection, next)
			else
				next()
			@

		getTagUrl: (tag) ->
			doc = @tagCollection.findOne(tag: tag)
			return doc?.get('url')

		generateTags: (renderCollection, next) ->

			# Prepare
			me = @
			docpad = @docpad
			config = @config
			database = docpad.getDatabase()
			targetedDocuments = docpad.getCollection(@config.collectionName)

			# regenerate tag cloud

			docpad.log 'debug', 'tagging::generateTags: Generating tag cloud'

			@maxCount = 0
			@tagCloud = {}

			targetedDocuments.forEach (document) =>
				# Prepare
				tags = document.get('tags') or []

				for tag in tags
					@tagCloud[tag] ?=
						tag: tag,
						count: 0,
						url: ""
						weight: 0
					count = ++@tagCloud[tag].count
					@maxCount = count if count > @maxCount

			# generate tag index pages

			docpad.log 'debug', 'tagging::generateTags: Generating tag index pages'

			docs_created = 0
			newDocs = new docpad.FilesCollection()
			for own tag of @tagCloud
				# check whether a document for this tag already exists in the collection
				if not @tagCollection.findOne(tag: tag)
					slug = balUtil.generateSlugSync(tag)
					slug = slug.toLowerCase() if config.indexPageLowercase
					doc = @docpad.createDocument(
							slug: slug
							relativePath: config.indexPagePath + "/" + slug + ".html"
							isDocument: true
							encoding: 'utf8'
						,
							data: " "	# NOTE: can't be empty string due to
										# quirk in FileModel (as of docpad v6.25)
							meta:
								layout: config.indexPageLayout
								referencesOthers: true
								tag: tag
					)
					database.add doc
					newDocs.add doc

					# if we're reloading (reset = false), our new document
					# will not have made it into the collection of modified
					# documents to render - so we need to add it
					if not renderCollection.findOne(tag: tag)
						renderCollection.add doc

					docs_created++

			docpad.log 'debug', "tagging::generateTags: #{docs_created} new docs added"

			# docpad has already called load and contextualize on its documents
			# so we need to call it manually here for our new docs
			docpad.loadFiles {collection: newDocs}, (err) =>
				if err then return next(err)

				@contextualizeAfterLock = true
				docpad.contextualizeFiles {collection: newDocs}, (err) =>
					if err then return next(err)

					@contextualizeAfterLock = false

					for own tag, item of @tagCloud
						@tagCloud[tag].url = @getTagUrl(tag)
						@tagCloud[tag].weight = @config.getTagWeight(item.count, @maxCount)

					next()

			@

