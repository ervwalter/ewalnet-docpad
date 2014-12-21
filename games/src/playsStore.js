var Reflux = require('reflux'),
	request = require('superagent'),
	_ = require('lodash'),
	Actions = require('./actions'),
	apiHelper = require('./apiHelper');

module.exports = Reflux.createStore({
	init() {
		this._data = {
			plays: [],
			loading: true
		}
		this.listenTo(Actions.loadPlays, this.onLoad);
		Actions.loadPlays();
	},

	onLoad() {
		var self = this;
		request.get('http://www.ewal.net//api/plays')
			.end(response => {
				self._data.plays = self.processPlays(response.body);
				self._data.loading = false;
				self.trigger(self._data);
			});
	},

	processPlays(plays) {
		var result = _.chain(plays).groupBy('gameId').sortBy((game) => {return game[0].playDate}).reverse().value();
		var cutoff = result[Math.min(10, result.length)][0].playDate;
		result = _.map(result, (item) => {
			var game = {
				gameId: item[0].gameId,
				image: item[0].image,
				name: item[0].name,
				thumbnail: item[0].thumbnail,
				plays: _.chain(item).filter((play) => {return play.playDate > cutoff}).map((play) => {
					return {playDate: play.playDate, comments: play.comments};
				}).value()
			};
			return game;
		});
		for (let play of result) {
			apiHelper.updateGameProperties(play);
			play.key = "play-" + play.gameId + "-" + play.plays.length;
		}
		return result;
	},

	getInitialState() {
		return this._data;
	}
});