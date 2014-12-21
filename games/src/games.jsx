
var React = require('react'),
	Collection = require('./collection'),
	Plays = require('./plays');

window.React = React;

var GamesPage = React.createClass({
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

React.render(<GamesPage/>, document.getElementById("games"));