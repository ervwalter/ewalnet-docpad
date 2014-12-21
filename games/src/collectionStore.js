var Reflux = require('reflux'),
	request = require('superagent'),
	_ = require('lodash'),
	Actions = require('./actions'),
	apiHelper = require('./apiHelper');

module.exports = Reflux.createStore({
	init() {
		this._sortBy = 'name';
		this._data = {
			games: [],
			loading: true
		};
		this.listenTo(Actions.loadCollection, this.onLoad);
		this.listenTo(Actions.changeSort, this.onChangeSort);
		Actions.loadCollection();
		setTimeout(() => {Actions.loadCollection()}, 70000);
	},

	onLoad() {
		var self = this;
		request.get('http://www.ewal.net//api/collection')
			.end(response => {
				self._data.games = self.sortGames(self.processGames(response.body));
				self._data.loading = false;
				self.trigger(self._data);
			});
	},

	processGames(games) {
		games = _.filter(games, game => { return game.owned && !game.isExpansion; });
		for (let game of games) {
			apiHelper.updateGameProperties(game)
		}
		return games;
	},

	onChangeSort(column) {
		if (column != this._sortBy) {
			this._sortBy = column;
			this._data.games = this.sortGames(this._data.games);
			this.trigger(this._data);
		}
	},

	sortGames(games) {
		var column = this._sortBy;
		var sortNames = (a, b) => {
			if (a.sortableName < b.sortableName) {
				return -1;
			} else if (a.sortableName > b.sortableName) {
				return 1;
			}
			return 0;
		};
		var sortNumbers = (a, b) => {
			if (!a) { a = 0 } // handle undefined
			if (!b) { b = 0 } // handle undefined
			return b - a;
		};

		if (column == 'name') {
			games.sort(sortNames);
		}
		else if (column == 'numPlays') {
			games.sort((a, b) => {
				var firstSortResult = sortNumbers(a.numPlays, b.numPlays);
				if (firstSortResult == 0) {
					return sortNames(a, b);
				}
				else {
					return firstSortResult;
				}
			});
		}
		else if (column == 'rating') {
			games.sort((a, b) => {
				var firstSortResult = sortNumbers(a.rating, b.rating);
				if (firstSortResult == 0) {
					return sortNames(a, b);
				}
				else {
					return firstSortResult;
				}
			});
		}
		return games;
	},

	getInitialState() {
		return this._data;
	}
});