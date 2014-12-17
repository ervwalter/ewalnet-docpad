username = 'edwalter'

app = angular.module 'GamesApp', ['ngResource', 'ngTouch', 'ngSanitize', 'ui.bootstrap']

#app.config ($locationProvider) ->
#	$locationProvider.html5Mode true

app.directive 'playsTooltip', ($http, $compile) ->
	return {
	restrict: 'A'
	scope: {
		game: '=playsTooltip'
	}
	link: (scope, element, attrs) ->
		template = '<div><p ng-repeat="play in game.plays"><i>Played {{ play.playDate | relativeDate}}</i> - {{ play.comments }}</p></div>'
		compiledContent = $compile(template)(scope)
		console.log compiledContent
		$(element).qtip({
			content:
				title: scope.game.name
				text: compiledContent
			position:
				my: 'bottom center'
				at: 'top center'
				target: $(element)
				viewport: $(window)
			hide:
				fixed: true
			style:
				classes: 'qtip-bootstrap qtip-play'
		})
	}

app.directive 'tooltipHtml', ($http, $compile, $templateCache) ->
	return {
	restrict: 'A'
	scope: {
		content: '=tooltipHtml'
		title: '@tooltipTitle'
	}
	link: (scope, element, attrs) ->
		template = '<div ng-bind-html="content"></div>'
		compiledContent = $compile(template)(scope)
		$(element).qtip({
			content:
				title: scope.title
				text: compiledContent
			position:
				my: 'left center'
				at: 'right center'
				target: $(element)
				viewport: $(window)
			hide:
				fixed: true
			style:
				classes: 'qtip-bootstrap qtip-play'
		})
	}

app.controller 'GamesCtrl', ($scope, $resource, $location, $http, $filter) ->
	playsApi = $resource "http://www.ewal.net/api/plays", {},
		get: {
			isArray: true
			transformResponse: $http.defaults.transformResponse.concat (data) ->
				return processPlays(data)
		}

	collectionApi = $resource "http://www.ewal.net/api/collection", {},
		get: {
			isArray: true
			transformResponse: $http.defaults.transformResponse.concat (data) ->
				return processGames(data)
		}

	$scope.plays = playsApi.get()
	$scope.games = collectionApi.get()

	$scope.playsLoaded = ->
		$scope.plays?.length > 0

	$scope.gamesLoaded = ->
		$scope.games?.length > 0

	$scope.range = (n, max) ->
		n = Math.min(n, max)
		(num for num in [1..n])

	$scope.gameCount = (games) ->
		count = 0
		count++ for game in games when game.owned and (not game.isExpansion)
		count

	$scope.playDetails = (game) ->
		details = ("<i>Played #{$filter('relativeDate')(play.playDate)}</i> - #{htmlEncode(play.comments)}" for play in game.plays)
		return details.join('<br/><br/>')

	$scope.sortByName = ->
		$location.search 'sort', 'name'
	$scope.sortByRating = ->
		$location.search 'sort', 'rating'
	$scope.sortByPlays = ->
		$location.search 'sort', 'plays'

	$scope.$watch ->
		$location.search().sort
	, (sort) ->
		switch sort
			when 'rating' then $scope.sortBy = ['-rating', '+sortableName']
			when 'plays' then $scope.sortBy = ['-numPlays', '+sortableName']
			else $scope.sortBy = '+sortableName'

updateGameProperties = (game) ->
	game.name = game.name.trim().replace(/\ \ +/, ' ') # remove extra spaces
	game.name = game.name.substr(0, game.name.length - 10).trim() if game.name.toLowerCase().endsWith('- base set') # fix Pathfinder games
	game.name = game.name.substr(0, game.name.length - 10).trim() if game.name.toLowerCase().endsWith('– base set') # fix Pathfinder games
	game.sortableName = game.name.toLowerCase().trim().replace(/^the\ |a\ |an\ /, '') # create a sort-friendly name without 'the', 'a', and 'an' at the start of titles
	return

processPlays = (plays) ->
	result = _.chain(plays).groupBy('gameId').sortBy((game) -> game[0].playDate).reverse().value()
	cutoff = result[Math.min(10, result.length)][0].playDate
	result = _.map result, (item) ->
		game = {
			gameId: item[0].gameId
			image: item[0].image
			name: item[0].name
			thumbnail: item[0].thumbnail
			plays: _.chain(item).filter((play) -> play.playDate > cutoff).map((play) -> { playDate: play.playDate, comments: play.comments}).value()
		}
		return game
	updateGameProperties game for game in result
	result

processGames = (games) ->
	for game in games
		updateGameProperties game
	return games

app.filter 'floor', ->
	return (input) ->
		Math.floor(parseFloat(input)).toString()

htmlEncode = (value) ->
	$('<div/>').text(value).html()

R_ISO8601_STR = /^(\d{4})-?(\d\d)-?(\d\d)(?:T(\d\d)(?::?(\d\d)(?::?(\d\d)(?:\.(\d+))?)?)?(Z|([+-])(\d\d):?(\d\d))?)?$/
NUMBER_STRING = /^\-?\d+$/

isString = (value) -> typeof value is 'string'
isNumber = (value) -> typeof value is 'number'
isDate = (value) -> value instanceof Date
int = (str) -> parseInt(str, 10)

app.filter 'count', () ->
	return (arr) ->
		return arr.length

app.filter 'relativeDate', ($filter) ->
	jsonStringToDate = (string) ->
		if match = string.match(R_ISO8601_STR)
			date = new Date(0)
			tzHour = 0
			tzMin = 0
			dateSetter = (if match[8] then date.setUTCFullYear else date.setFullYear)
			timeSetter = (if match[8] then date.setUTCHours else date.setHours)
			if match[9]
				tzHour = int(match[9] + match[10])
				tzMin = int(match[9] + match[11])
			dateSetter.call date, int(match[1]), int(match[2]) - 1, int(match[3])
			h = int(match[4] or 0) - tzHour
			m = int(match[5] or 0) - tzMin
			s = int(match[6] or 0)
			ms = Math.round(parseFloat("0." + (match[7] or 0)) * 1000)
			timeSetter.call date, h, m, s, ms
			return date
		return string

	dateFilter = $filter('date')

	return (date, format) ->
		if isString(date)
			if NUMBER_STRING.test(date)
				date = int(date)
			else
				date = jsonStringToDate(date)
		date = new Date(date) if isNumber(date)
		return date unless isDate(date)
		m = moment(date)
		sod = moment().startOf('day')
		diff = m.diff(sod, 'days', true)
		if diff < -6
			return dateFilter(date, format)
		else if diff < -1
			return "#{m.format('dddd')}"
		else if diff < 0
			return 'Yesterday'
		else if diff == 0
			return 'Today'
		else
			return dateFilter(date, format)

app.directive 'trackLoaded', ($animate) ->
	{
		link: (scope, element, attrs) ->
			element.bind "load", (event) ->
				element.addClass('loaded')
	}
