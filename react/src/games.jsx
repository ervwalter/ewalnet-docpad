var Fluxxor = require('fluxxor'),
	actions = require('./actions'),
	stores = require('./gamesStores'),
	Collection = require('./collection'),
	Plays = require('./plays'),
	ProfileBox = require('./profile');

var GamesPage = React.createClass({
	mixins: [Fluxxor.FluxMixin(React), Fluxxor.StoreWatchMixin('CollectionStore')],
	getStateFromFlux() {
		var store = this.getFlux().store('CollectionStore');
		return {
			gamesCount: store.gamesCount
		};
	},
	render() {
		return (
			<div>
				<h1>Board Games</h1>
				<p>I admit that <a href="http://photos.ewal.net/games">I am obsessed</a> with modern/designer board games. I have a respectable collection of games, and I seem to add to it more frequently than I probably should.  I track the games that I own and the games that I play on <a href="http://boardgamegeek.com">BoardGameGeek</a>, and this page chronicles my obsession.</p>
				<Plays />
				<Collection />
				{ this.state.gamesCount > 0 ? <ProfileBox/> : null }
				<br/>
				<br/>
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