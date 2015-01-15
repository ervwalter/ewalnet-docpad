var React = require('react');
var Fluxxor = require('fluxxor');
var actions = require('./actions');
var gamesStores = require('./gamesStores');
var menuStores = require('./menuStores');

require('browsernizr/test/touchevents');
require('browsernizr');

require('./lib');

var MenuPage = React.createClass({
	mixins: [Fluxxor.FluxMixin(React), Fluxxor.StoreWatchMixin('MenuStore')],
	componentDidMount() {
		this.getFlux().actions.loadMenuData();
	},
	getStateFromFlux() {
		var store = this.getFlux().store('MenuStore');
		return {
			litefare: store.litefare,
			entree: store.entree
		};
	},
	onReseedClick(e) {
		this.getFlux().actions.reseedMenu();
		e.preventDefault();
	},
	render() {
		return (
			<div className="container">
				<div id="actions">
					<a href="/games/"><span className="icon icon-th-list adjust-down"></span></a>
					<a href="" onClick={this.onReseedClick}><span className="icon icon-refresh3"></span></a>
				</div>
				<div id="title"><a href="/menu/">Board Game Menu</a></div>
				<div id="subtitle">&mdash; Games for Two Players &mdash;</div>
				<Category key="litefare" title="Snacks / Lite Fare" games={this.state.litefare} />
				<Category key="entrees" title="Entreés" games={this.state.entree} />
			</div>
		);
	}
});

var Category = React.createClass({
	render() {
			var items;
		if (this.props.games.length === 0) {
			items = [
				<Placeholder key="1" ellipsis={true}/>,
				<Placeholder key="2" />,
				<Placeholder key="3" />
			]
		} else {
			items = this.props.games.map(game => <MenuItem key={game.gameId} game={game} />);
		}
		return (
			<div className="category" >
				<div className="category-title">{this.props.title}</div>
				{items}
			</div>
		);
	}
});

var MenuItem = React.createClass({
	render() {
		var badge;
		var game = this.props.game;
		if (game.numPlays === 0) {
			badge = <span className="new">New!</span>;
		} else if (game.rating > 8) {
			badge = <span className="icon icon-star favorite"></span>;
		}
		return (
			<div className="item fadein">
				<div className="name"><a href={'http://boardgamegeek.com/boardgame/' + game.gameId}>{game.name}</a><span className="year">&nbsp;({game.yearPublished})</span>{badge}</div>
				<div className="mechanics">{game.mechanics.join(', ')}</div>
				<div className="time">{game.playingTime} Minutes</div>
			</div>
		);
	}
})

var Placeholder = React.createClass({
	render() {
		return (
			<div className="item placeholder">
				<div className="name">{this.props.ellipsis ? <span>&hellip;</span> : <span>&nbsp;</span>}</div>
				<div className="mechanics">&nbsp;</div>
				<div className="time">&nbsp;</div>
			</div>
		);
	}
})

var flux = new Fluxxor.Flux({}, actions);
flux.addStores(gamesStores);
flux.addStores(menuStores);

flux.on("dispatch", function (type, payload) {
	if (console && console.log) {
		console.log("[Dispatch]", type, payload);
	}
});


React.render(
	<MenuPage flux={flux}/>
	, document.getElementById('menu'));