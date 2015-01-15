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
			var exclusionsById = Lazy(this._exclusions).indexBy(value => value).toObject();
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
					var tomorrow = moment().startOf('day').add(1, 'day');
					var lastPlay = moment(helper.jsonStringToDate(game.playDates[0]));
					if (lastPlay.isBefore(tomorrow)) {
						var daysSinceLastPlay = tomorrow.diff(lastPlay, 'days') - 1;
						game.daysSinceLastPlay = daysSinceLastPlay;
						if (daysSinceLastPlay > 60) {
							daysSinceLastPlay = 60;
						}
						score *= 1 + (0.01 * (daysSinceLastPlay - 10));
					}
				}

				if (!game.numPlays) {
					// Significantly increase chance for games never played
					score *= 4;
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

			this._games = games.toArray();

			var categories = games.groupBy(game => {
				var effectivePlayingTime = game.playingTime;
				if (game.averageWeight > 0) {
					var averageWeight = game.averageWeight;
					if (averageWeight > 3.1) {
						effectivePlayingTime += 45;
					} else if (averageWeight > 2.8) {
						effectivePlayingTime += 30;
					} else if (averageWeight > 2.5) {
						effectivePlayingTime += 15;
					} else if (averageWeight < 1.7) {
						effectivePlayingTime *= 1/2;
					} else if (averageWeight < 2) {
						effectivePlayingTime *= 2/3;
					}
				}

				game.effectivePlayingTime = Number(effectivePlayingTime.toFixed(0));

				if (effectivePlayingTime<= 60) {
					return 'litefare';
				}
				else {
					return 'entree';
				}
			}).toObject();

			if (console.table) {
				console.log("Lite Fare Games:");
				console.table(Lazy(categories.litefare).sortBy('score', true).toArray(), ['name', 'score', 'rating', 'daysSinceLastPlay', 'playingTime', 'effectivePlayingTime', 'averageWeight']);
				console.log("Entree Games:");
				console.table(Lazy(categories.entree).sortBy('score', true).toArray(), ['name', 'score', 'rating', 'daysSinceLastPlay', 'playingTime', 'effectivePlayingTime', 'averageWeight']);
			}

			this.litefare = this._chooseGames(categories.litefare, 3);
			this.entree = this._chooseGames(categories.entree, 3);
			this.emit('change');
		}
	},

	_chooseGames(games, count) {
		var sum = Lazy(games).sum(game => game.score);

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