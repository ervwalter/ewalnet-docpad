var request = require('superagent'),
	moment = require('moment');

function loadPlays(success) {
	request.get('http://www.ewal.net/api/plays').use(nocache).end(success);
}

function loadCollection(success) {
	request.get('http://www.ewal.net/api/collection').use(nocache).end(success);
}

function loadMenuExclusions(success) {
	request.get('http://www.ewal.net/api/menuexclusions').use(nocache).end(success);
}

function nocache (request) {
	request.query({'_': moment().unix()});
	return request;
};

module.exports = {
	loadCollection: loadCollection,
	loadPlays: loadPlays,
	loadMenuExclusions: loadMenuExclusions
}