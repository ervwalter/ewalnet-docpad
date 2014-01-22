username = 'edwalter'

app = angular.module 'GamesApp', ['ngResource']

#app.config ($locationProvider) ->
#	$locationProvider.html5Mode true

app.controller 'GamesCtrl', ($scope, $resource, $location, $http) ->
	playsApi = $resource "http://bgg-json.azurewebsites.net/plays/#{username}", {},
		jsonp: {
			method: 'JSONP'
			params: { callback: 'JSON_CALLBACK' }
			isArray: true
			transformResponse: $http.defaults.transformResponse.concat (data) ->
				processPlays(data)
				return data
		}

	collectionApi = $resource "http://bgg-json.azurewebsites.net/collection/#{username}?grouped=true", {},
		jsonp: {
			method: 'JSONP'
			params: { callback: 'JSON_CALLBACK' }
			isArray: true
			transformResponse: $http.defaults.transformResponse.concat (data) ->
				processGames(data)
				return data
		}

	$scope.hasExpansion = (game) ->
		return false unless game?.expansions?
		result = false
		(result = true if e.owned) for e in game.expansions
		return result

	$scope.plays = playsApi.jsonp()
	$scope.games = collectionApi.jsonp()
	$scope.playsLimit = 7

	$scope.showMorePlays = ->
		$scope.playsLimit = 50
	$scope.showFewerPlays = ->
		$scope.playsLimit = 7
		$('#games-recent-plays').scrollTo()

	$scope.sortByName = ->
		$location.search 'sort', 'name'
	$scope.sortByRating = ->
		$location.search 'sort', 'rating'
	$scope.sortByPlays = ->
		$location.search 'sort', 'plays'

	$scope.showDetails = ->
		$location.search 'show', 'details'
	$scope.showThumbnails = ->
		$location.search 'show', 'thumbnails'

	$scope.$watch ->
		$location.search().sort
	, (sort) ->
		switch sort
			when 'rating' then $scope.sortBy = ['-rating', '+sortableName']
			when 'plays' then $scope.sortBy = ['-numPlays', '+sortableName']
			else $scope.sortBy = '+sortableName'

	$scope.$watch ->
		$location.search().show
	, (show) ->
		switch show
			when 'thumbnails' then $scope.thumbnailsOnly = true
			else $scope.thumbnailsOnly = false

processPlays = (plays) ->
	for play in plays
		play.name = play.name.trim().replace(/\ \ +/, ' ') # remove extra spaces
		play.name = play.name.substr(0, play.name.length - 10).trim() if play.name.toLowerCase().endsWith('- base set') # fix Pathfinder games
	return plays

processGames = (games) ->
	for game in games
		game.name = game.name.trim().replace(/\ \ +/, ' ') # remove extra spaces
		game.name = game.name.substr(0, game.name.length - 10).trim() if game.name.toLowerCase().endsWith('- base set') # fix Pathfinder games
		game.sortableName = game.name.toLowerCase().trim().replace(/^the\ |a\ |an\ /, '') # create a sort-friendly name without 'the', 'a', and 'an' at the start of titles
		parentName = game.name.toLowerCase()
		if game.expansions?
			for expansion in game.expansions
				expansion.name = expansion.name.trim().replace(/\ \ +/, ' ') # remove extra spaces in expansion name also
				if expansion.name.toLowerCase().substr(0, parentName.length) is parentName
					expansion.name = expansion.name.substr(parentName.length).trimStart(['-', ':', ' '])
	return games

app.filter 'floor', ->
	return (input) ->
		Math.floor(parseFloat(input)).toString()


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
			return "Last #{m.format('ddd')}"
		else if diff < 0
			return 'Yesterday'
		else if diff == 0
			return 'Today'
		else
			return dateFilter(date, format)





