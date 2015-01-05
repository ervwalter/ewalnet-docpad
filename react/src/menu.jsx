var Fluxxor = require('fluxxor'),
	actions = require('./actions'),
	gamesStores = require('./gamesStores'),
	menuStores = require('./menuStores');

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
		if (this.state.litefare.length === 0) {
			return (
				<div className="container"></div>
			);
		} else {
			return (
				<div className="container">
					<div className="fade-in">
						<div id="actions">
							<a href="/games/"><span className="icon icon-th-list adjust-down"></span></a>
							<a href="" onClick={this.onReseedClick}><span className="icon icon-refresh3"></span></a>
						</div>
						<div id="title"><a href="/menu/">Board Game Menu</a></div>
						<div id="subtitle">&mdash; Games for Two Players &mdash;</div>
						<Category key="litefare" title="Snacks / Lite Fare" games={this.state.litefare} />
						<Category key="entrees" title="Entreés" games={this.state.entree} />
					</div>
				</div>
			);
		}
	}
});

var Category = React.createClass({
	render() {
		return (
			<div className="category">
				<div className="category-title">{this.props.title}</div>
				{this.props.games.map(game => {
					return <MenuItem key={game.gameId} game={game} />;
				})}
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
			<div className="item">
				<div className="name"><a href={'http://boardgamegeek.com/boardgame/' + game.gameId}>{game.name}</a><span className="year">&nbsp;({game.yearPublished})</span>{badge}</div>
				<div className="mechanics">{game.mechanics.join(', ')}</div>
				<div className="time">{game.playingTime} Minutes</div>
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