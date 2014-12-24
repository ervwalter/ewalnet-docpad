var request = require('superagent');

function loadPlays(success) {
	request.get('http://www.ewal.net/api/plays').end(success);
}

function loadCollection(success) {
	request.get('http://www.ewal.net/api/collection').end(success);
}

module.exports = {
	loadCollection: loadCollection,
	loadPlays: loadPlays
}