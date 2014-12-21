var _ = require('lodash');

module.exports = {
	updateGameProperties(game) {
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
}