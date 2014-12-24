var Fluxxor = require('fluxxor'),
	constants = require('./constants'),
	actions = require('./actions'),
	stores = require('./stores'),
	Collection = require('./collection'),
	Plays = require('./plays');

var GamesPage = React.createClass({
	mixins: [Fluxxor.FluxMixin(React)],
	render() {
		return (
			<div>
				<h1>Board Games</h1>
				<p>I admit that I am obsessed with modern/designer board games. I have a respectable collection of games, and I seem to add to it more frequently than I probably should.  I track the games that I own and the games that I play on <a href="http://boardgamegeek.com">BoardGameGeek</a>, and this page chronicles my obsession.</p>
				<Plays />
				<Collection />
			</div>
		)
	}
})

var flux = new Fluxxor.Flux(stores, actions);

flux.on("dispatch", function (type, payload) {
	if (console && console.log) {
		console.log("[Dispatch]", type, payload);
	}
});

React.render(<GamesPage flux={flux}/>, document.getElementById("games"));