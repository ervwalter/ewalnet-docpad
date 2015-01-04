var keymirror = require('keymirror');

var constants = {
	LoadCollection: null,
	LoadCollectionComplete: null,
	LoadPlays: null,
	LoadPlaysComplete: null,
	ChangeSort: null,
	LoadMenuExclusions: null,
	LoadMenuExclusionsComplete: null,
	ReseedMenu: null
};

module.exports = keymirror(constants);
