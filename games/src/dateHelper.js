var moment = require('moment');

var R_ISO8601_STR = /^(\d{4})-?(\d\d)-?(\d\d)(?:T(\d\d)(?::?(\d\d)(?::?(\d\d)(?:\.(\d+))?)?)?(Z|([+-])(\d\d):?(\d\d))?)?$/;

function jsonStringToDate(string) {
	var date, dateSetter, h, m, match, ms, s, timeSetter, tzHour, tzMin;
	if (match = string.match(R_ISO8601_STR)) {
		date = new Date(0);
		tzHour = 0;
		tzMin = 0;
		dateSetter = (match[8] ? date.setUTCFullYear : date.setFullYear);
		timeSetter = (match[8] ? date.setUTCHours : date.setHours);
		if (match[9]) {
			tzHour = int(match[9] + match[10]);
			tzMin = int(match[9] + match[11]);
		}
		dateSetter.call(date, int(match[1]), int(match[2]) - 1, int(match[3]));
		h = int(match[4] || 0) - tzHour;
		m = int(match[5] || 0) - tzMin;
		s = int(match[6] || 0);
		ms = Math.round(parseFloat("0." + (match[7] || 0)) * 1000);
		timeSetter.call(date, h, m, s, ms);
		return date;
	}
	return string;
}

function isString(value){return typeof value === 'string';}

function isNumber(value){return typeof value === 'number';}

function isDate(value){
	return toString.call(value) === '[object Date]';
}

function int(str) {
	return parseInt(str, 10);
}

var NUMBER_STRING = /^\-?\d+$/;

function relativeDate(date, format) {
	var diff, m, sod;
	if (isString(date)) {
		if (NUMBER_STRING.test(date)) {
			date = int(date);
		} else {
			date = jsonStringToDate(date);
		}
	}
	if (isNumber(date)) {
		date = new Date(date);
	}
	if (!isDate(date)) {
		return date;
	}
	m = moment(date);
	sod = moment().startOf('day');
	diff = m.diff(sod, 'days', true);
	if (diff < -6) {
		return m.format('MMM D, YYYY');
	} else if (diff < -1) {
		return "" + (m.format('dddd'));
	} else if (diff < 0) {
		return 'Yesterday';
	} else if (diff === 0) {
		return 'Today';
	} else {
		return m.format('MMM D, YYYY');
	}
}

module.exports = {
	relativeDate: relativeDate
};
