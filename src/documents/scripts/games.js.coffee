username = 'edwalter'

app = angular.module 'GamesApp', ['ngResource', 'ngTouch', 'ngSanitize', 'ngAnimate', 'ui.bootstrap']

#app.config ($locationProvider) ->
#	$locationProvider.html5Mode true

app.controller 'GamesCtrl', ($scope, $resource, $location, $http, $filter) ->
	playsApi = $resource "http://bgg-json.azurewebsites.net/plays/#{username}", {},
		jsonp: {
			method: 'JSONP'
			params: { callback: 'JSON_CALLBACK' }
			isArray: true
			transformResponse: $http.defaults.transformResponse.concat (data) ->
				return processPlays(data)
		}

	collectionApi = $resource "http://bgg-json.azurewebsites.net/collection/#{username}?grouped=true&details=true", {},
		jsonp: {
			method: 'JSONP'
			params: { callback: 'JSON_CALLBACK' }
			isArray: true
			transformResponse: $http.defaults.transformResponse.concat (data) ->
				return processGames(data)
		}

	challengeApi = $resource "http://bgg-json.azurewebsites.net/challenge/171319", {},
		jsonp: {
			method: 'JSONP'
			params: { callback: 'JSON_CALLBACK' }
		}

	$scope.plays = playsApi.jsonp()
	$scope.games = collectionApi.jsonp()
	$scope.challenge = challengeApi.jsonp()

	$scope.challengeLoaded = ->
		$scope.challenge?.items?.length > 0

	$scope.playsLoaded = ->
		$scope.plays?.length > 0

	$scope.gamesLoaded = ->
		$scope.games?.length > 0

	$scope.range = (n) ->
		(num for num in [1..n])

	$scope.hasExpansion = (game) ->
		return false unless game?.expansions?
		result = false
		(result = true if e.owned) for e in game.expansions
		return result

	$scope.expansions = (game) ->
		list = _.chain(game.expansions).where(owned: true).sortBy('sortableName').pluck('name').value()
		list.join(',<br/>')

	$scope.percentComplete = (challenge) ->
		return 0 unless challenge?.items?.length > 0
		sum = _.reduce challenge.items, ((s, i) -> s + i.playCount), 0
		total = challenge.items.length * challenge.goalPerGame
		return 100 if sum > total
		return Math.floor(100 * sum / total)

	$scope.playDetails = (play) ->
		details = ""
		#details += "<b>#{htmlEncode(play.name)}</b><br/>"
		details += "<i>Played #{$filter('relativeDate')(play.playDate)}</i> - "
		details += "#{htmlEncode(play.comments)}"
		return details

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
	updateGameProperties play for play in plays
	_.uniq plays, (item) ->
		item.gameId

processGames = (games) ->
	for game in games
		updateGameProperties game
		parentName = game.name.toLowerCase()
		if game.expansions?
			game.expansionList = (expansion.name for expansion in game.expansions).join('<br/>')
			for expansion in game.expansions
				updateGameProperties expansion
				if expansion.name.toLowerCase().substr(0, parentName.length) is (parentName)
					shortName = expansion.name.substr(parentName.length).trimStart(' ')
					unless shortName.toLowerCase().match(/^[a-z]/)
						expansion.longName = expansion.name
						expansion.name = shortName.trimStart(['–', '-', ':', ' '])
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
