var Fluxxor = require('fluxxor'),
	constants = require('./constants'),
	actions = require('./actions'),
	compare = require('compare-property'),
	Lazy = require('lazy.js'),
	moment = require('moment'),
	seedrandom = require('seedrandom'),
	helper = require('./helper');

var ExclusionsStore = Fluxxor.createStore({
	initialize() {
		this.exclusions = [];
		this.loading = true;
		this.bindActions(
			constants.LoadMenuExclusions, this.onLoadMenuExclusions,
			constants.LoadMenuExclusionsComplete, this.onLoadMenuExclusionsComplete
		);
	},

	onLoadMenuExclusions() {
		this.loading = true;
		this.emit('change');
	},

	onLoadMenuExclusionsComplete(payload) {
		this.loading = false;
		this.exclusions = payload.exclusions;
		this.emit('change');
	}
});

var MenuStore = Fluxxor.createStore({
	initialize() {
		this.litefare = [];
		this.entree = [];
		this._exclusions = [];
		this._collection = [];
		this._games = [];
		this._rnd = seedrandom(moment().format('YYYY-MM-DD'));
		this.bindActions(
			constants.LoadMenuExclusionsComplete, this.onLoadMenuExclusionsComplete,
			constants.LoadCollectionComplete, this.onLoadCollectionComplete,
			constants.ReseedMenu, this.onReseedMenu
		);
	},

	onLoadCollectionComplete(payload) {
		this.waitFor(['CollectionStore'], collectionStore => {
			this._collection = collectionStore.games;
			this._processGames();
		});
	},

	onLoadMenuExclusionsComplete(payload) {
		this.waitFor(['ExclusionsStore'], exclusionsStore => {
			this._exclusions = exclusionsStore.exclusions;
			this._processGames();
		});
	},

	onReseedMenu() {
		this._rnd = seedrandom();
		this._processGames();
	},

	_processGames() {
		if (this._exclusions.length > 0 && this._collection.length > 0) {
			var exclusionsById = Lazy(this._exclusions).indexBy(value => {
				return value;
			}).toObject();
			var games = Lazy(this._collection).filter(game => {
				if (exclusionsById[game.gameId] || game.minPlayers > 2 || game.maxPlayers < 2) {
					return false;
				}
				return true;
			});

			games.each(game => {
				var score = 1;

				// Add some randomness to start
				score = 1 + (this._rnd() / 10) - 0.05

				// Increase chance for higher rated games
				if (game.rating) {
					score *= 1 + (0.2 * (game.rating - 7));
				}

				// Increase chance for games not played recently
				if (game.playDates && game.playDates.length > 0) {
					var today = moment().startOf('day');
					var lastPlay = moment(helper.jsonStringToDate(game.playDates[0]));
					if (lastPlay.isBefore(today)) {
						var daysSinceLastPlay = today.diff(lastPlay, 'days');
						if (daysSinceLastPlay > 60) {
							daysSinceLastPlay = 60;
						}
						score *= 1 + (0.01 * (daysSinceLastPlay - 10));
					}
				}

				if (games.numPlays === 0) {
					// Significantly increase chance for games never played
					score *= 2;
				} else 	if (games.numPlays < 5) {
					// Increase chance for games played less than 5 times
					score *= 1.1;
				}

				// Not really possible, but just in case...
				if (score < 0) {
					score = 0;
				}

				game.score = score;
			});

			//if (console.table) {
			//	console.table(games.sortBy('score', true).toArray(), ['name', 'score', 'rating', 'playingTime', 'minPlayers', 'maxPlayers']);
			//}

			this._games = games.toArray();

			var categories = games.groupBy(game => {
				if (game.playingTime < 90) {
					return 'litefare';
				}
				else {
					return 'entree';
				}
			}).toObject();

			for (var category in categories) {
				if (categories.hasOwnProperty(category)) {
					this[category] = this._chooseGames(categories[category], 4);
				}
			}
			this.emit('change');
		}
	},

	_chooseGames(games, count) {
		var sum = Lazy(games).sum(game => {
			return game.score
		});

		games.forEach(game => {
			game.normalizedScore = game.score / sum;
		});

		var choices = [];
		while (choices.length < count) {
			var target = this._rnd();
			var i = 0;
			var threshold = games[i].normalizedScore;
			while (threshold < target && i < (games.length - 1)) {
				i++;
				threshold += games[i].normalizedScore;
			}
			var choice = games[i];
			if (Lazy(choices).indexOf(choice) === -1) {
				choices.push(games[i]);
			}
		}

		return choices;
	}
});

module.exports = {
	ExclusionsStore: new ExclusionsStore(),
	MenuStore: new MenuStore()
};