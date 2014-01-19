username = 'edwalter'

app = angular.module 'GamesApp', ['ngResource']

app.controller 'GamesCtrl', ($scope, $resource) ->
	playsApi = $resource "http://bgg-json.azurewebsites.net/plays/#{username}", {},
		jsonp: { method: 'JSONP', params: { callback: 'JSON_CALLBACK' }, isArray: true }

	collectionApi = $resource "http://bgg-json.azurewebsites.net/collection/#{username}?grouped=true", {},
		jsonp: { method: 'JSONP', params: { callback: 'JSON_CALLBACK' }, isArray: true }


	$scope.hasExpansion = (game) ->
		return false unless game?.expansions?
		result = false
		(result = true if e.owned) for e in game.expansions
		return result

	$scope.plays = playsApi.jsonp()
	$scope.games = collectionApi.jsonp()

	$scope.sortByName = ->
		$scope.sortBy = 'name'
		$scope.reverse = false
	$scope.sortByRating = ->
		$scope.sortBy = 'rating'
		$scope.reverse = true
	$scope.sortByPlays = ->
		$scope.sortBy = 'numPlays'
		$scope.reverse = true
	$scope.sortByName()

	$scope.showDetails = ->
		$scope.thumbnailsOnly = false
	$scope.showThumbnails = ->
		$scope.thumbnailsOnly = true
	$scope.showDetails()

app.filter 'floor', ->
	(input) ->
		Math.floor(parseFloat(input)).toString()