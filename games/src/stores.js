var Fluxxor = require('fluxxor'),
	constants = require('./constants'),
	actions = require('./actions'),
	compare = require('compare-property'),
	Lazy = require('lazy.js');

var CollectionStore = Fluxxor.createStore({
	initialize() {
		this.loading = false;
		this.games = [];
		this.sortBy = 'name';
		this.bindActions(
			constants.LoadCollection, this.onLoadCollection,
			constants.LoadCollectionComplete, this.onLoadCollectionComplete,
			constants.ChangeSort, this.onChangeSort
		);
	},

	onLoadCollection() {
		this.loading = true;
		this.emit('change');
	},

	onLoadCollectionComplete(payload) {
		this.loading = false;
		var games = this._processGames(payload.games);
		this._sortGames(games);
		this.games = games;
		this.emit('change');
	},

	onChangeSort(payload) {
		var sortBy = payload.property;
		if (sortBy !== this.sortBy) {
			this.sortBy = sortBy;
			this._sortGames(this.games);
			this.emit('change');
		}
	},

	_processGames(games) {
		games = Lazy(games).filter(game => {
			return game.owned && !game.isExpansion;
		}).toArray();
		for (let game of games) {
			updateGameProperties(game)
		}
		return games;
	},

	_sortGames(games) {
		var fn;
		switch (this.sortBy) {
			case 'numPlays':
				fn = compare.properties({numPlays: -1, sortableName: 1});
				break;
			case 'rating':
				fn = compare.properties({rating: -1, sortableName: 1});
				break;
			default:
				fn = compare.property('sortableName', 1);
		}
		games.sort(fn);
	}
});

var PlaysStore = Fluxxor.createStore({
	initialize() {
		this.loading = false;
		this.plays = [];
		this.bindActions(
			constants.LoadPlays, this.onLoadPlays,
			constants.LoadPlaysComplete, this.onLoadPlaysComplete
		);
	},

	onLoadPlays() {
		this.loading = true;
		this.emit('change');
	},

	onLoadPlaysComplete(payload) {
		this.loading = false;
		//process the raw JSON api results to group plays of the same game and filter out very old plays
		this.plays = this._processPlays(payload.plays);
		this.emit('change');
	},

	_processPlays(plays) {
		var result = Lazy(plays).groupBy('gameId').map(item => { return item }).sortBy(item => { return item[0].playDate }).reverse().toArray()
		var cutoff = result[Math.min(10, result.length)][0].playDate;
		result = result.map((item) => {
			var game = {
				gameId: item[0].gameId,
				image: item[0].image,
				name: item[0].name,
				thumbnail: item[0].thumbnail,
				plays: Lazy(item).filter((play) => {
					return play.playDate > cutoff
				}).map((play) => {
					return {playDate: play.playDate, comments: play.comments};
				}).toArray()
			};
			return game;
		});
		for (let play of result) {
			updateGameProperties(play);
			play.key = "play-" + play.gameId + "-" + play.plays.length;
		}
		return result;
	}


});

function updateGameProperties(game) {
	game.name = game.name.trim().replace(/\ \ +/, ' ');  // remove extra spaces
	if (game.name.toLowerCase().endsWith('- base set')) { // fix Pathfinder games
		game.name = game.name.substr(0, game.name.length - 10).trim();
	}
	if (game.name.toLowerCase().endsWith('– base set')) { // fix Pathfinder games
		game.name = game.name.substr(0, game.name.length - 10).trim();
	}
	game.sortableName = game.name.toLowerCase().trim().replace(/^the\ |a\ |an\ /, ''); // create a sort-friendly name without 'the', 'a', and 'an' at the start of titles
	return game;
}

module.exports = {
	CollectionStore: new CollectionStore(),
	PlaysStore: new PlaysStore()
};
