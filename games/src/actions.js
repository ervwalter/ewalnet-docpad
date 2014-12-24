var api = require('./api'),
	constants = require('./constants');

module.exports = {
	loadPlays() {
		this.dispatch(constants.LoadPlays);
		api.loadPlays(response => {
			this.dispatch(constants.LoadPlaysComplete, {plays: response.body});
		});
	},
	loadCollection() {
		this.dispatch(constants.LoadCollection);
		api.loadCollection(response => {
			this.dispatch(constants.LoadCollectionComplete, {games: response.body});
		});
	},
	changeSort(property) {
		this.dispatch(constants.ChangeSort, {property: property});
	}
};


