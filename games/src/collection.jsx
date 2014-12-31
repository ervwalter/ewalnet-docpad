var Fluxxor = require('fluxxor'),
	helper = require('./helper'),
	Lazy = require('lazy.js');

var Collection = React.createClass({
	mixins: [Fluxxor.FluxMixin(React), Fluxxor.StoreWatchMixin('CollectionStore')],

	getStateFromFlux() {
		var store = this.getFlux().store('CollectionStore');
		return {
			loading: store.loading,
			games: store.games,
			gamesCount: store.gamesCount,
			expansionsCount: store.expansionsCount
		};
	},

	componentDidMount() {
		this.getFlux().actions.loadCollection();
	},

	render() {
		return (
			<div className="row">
				<div className="col-xs-12">
					<h2>My Game Collection - <a href="http://boardgamegeek.com/collection/user/edwalter?own=1">full list</a>
						<GameCount loading={this.state.loading} games={this.state.gamesCount} expansions={this.state.expansionsCount}/>
					</h2>
					<CollectionTable loading={this.state.loading} games={this.state.games} />
				</div>
			</div>
		);
	}
});

var CollectionTable = React.createClass({
	render() {
		if (this.props.games.length === 0) {
			return (
				<WireframeCollectionTable />
			)
		} else {
			var rows = this.props.games.map(game => {
				return (<CollectionRow game={game} key={game.gameId} />)
			});

			return (
				<table className="collection-table table table-striped">
					<thead>
						<tr className="actions">
							<th>
								<CollectionColumnHeader property="name">Name</CollectionColumnHeader>
							</th>
							<th>
								<CollectionColumnHeader property="numPlays">
									<span className="wide-only">Times </span>
									Played</CollectionColumnHeader>
							</th>
							<th>
								<CollectionColumnHeader property="rating">
									<span className="wide-only">My </span>
									Rating</CollectionColumnHeader>
							</th>
						</tr>
					</thead>
					<tbody>
						{rows}
					</tbody>
				</table>
			)
		}
	}
});

var CollectionColumnHeader = React.createClass({
	mixins: [Fluxxor.FluxMixin(React)],
	onSort(e) {
		e.preventDefault();
		this.getFlux().actions.changeSort(this.props.property);
	},
	render() {
		return (
			<a href="" onClick={this.onSort}>{this.props.children}</a>
		)
	}
});

var CollectionRow = React.createClass({
	render() {
		var game = this.props.game;

		return (
			<tr>
				<td>
					<a href={'http://boardgamegeek.com/boardgame/' + game.gameId}>{game.name}</a>
					<ExpansionLink game={game} />
				</td>
				<td className="games-times-played">
					<TimesPlayed count={game.numPlays}/>
				</td>
				<td className="games-rating">
					<Rating value={game.rating}/>
				</td>
			</tr>
		)
	}
});


var ExpansionLink = React.createClass({
	componentDidMount() {
		if ($) {
			var game = this.props.game;
			if (game.expansionsOwnedCount > 0) {
				var expansionNames = Lazy(game.expansions).where({owned: true}).sortBy('sortableName').pluck('name').toArray();
				var tooltipContent = expansionNames.join(",<br/>");
				var el = this.refs.link.getDOMNode();
				$(el).qtip({
					content: {
						title: 'Expansions',
						text: tooltipContent
					},
					position: {
						my: 'bottom center',
						at: 'top center',
						target: $(el),
						viewport: $(window)
					},
					hide: { fixed: true },
					style: { classes: 'qtip-bootstrap qtip-play'}
				});
			}
		}
	},
	componentWillUnmount() {
		var el = this.refs.link.getDOMNode();
		if ($) {
			$(el).qtip('destroy');
		}
	},
	render() {
		var game = this.props.game;
		if (game.expansionsOwnedCount > 0) {
			return (
				<span ref="link" className="games-expansions games-expansions-link wide-only">{ '' + game.expansionsOwnedCount + (game.expansionsOwnedCount > 1 ? ' expansions' : ' expansion') }</span>
			);
		} else {
			return null;
		}

	}
});

var TimesPlayed = React.createClass({
	render() {
		var count = this.props.count;
		if (count > 0) {
			return (
				<span>
					<span className="wide-only">Played </span>
					<b>{count}</b>
				{ count > 1 ? ' times' : ' time' }</span>
			)
		} else {
			return (
				<span className="mdash">&mdash;</span>
			)
		}
	}
});

var Rating = React.createClass({
	render() {
		var rating = this.props.value;
		if (rating => 0) {
			var stars = [];
			for (let i = 0; i < rating && i < 10; i++) {
				stars.push(<i className="glyphicon glyphicon-star" key={i + 1}></i>)
			}
			for (let i = rating; i < 10; i++) {
				stars.push(<i className="glyphicon glyphicon-star-empty" key={i + 1}></i>)
			}
			return (
				<span>
					<span className="wide-only">{stars}</span>
					<span className="narrow-only">{rating}</span>
				</span>
			)
		} else {
			return (
				<span className="mdash">&mdash;</span>
			)
		}
	}
});

var GameCount = React.createClass({
	render() {
		if (!this.props.loading) {
			return (
				<span className="pull-right hidden-xs game-counts fade-in-slow">
					<b>{ this.props.games }</b> games, <b>{ this.props.expansions }</b> expansions</span>
			)
		}
		return null;
	}
});

var WireframeCollectionTable = React.createClass({
	render() {
		return (
			<table className="collection-table wireframe table">
				<thead>
					<tr>
						<th>Name</th>
						<th><span className="wide-only">Times </span>Played</th>
						<th><span className="wide-only">My </span>Rating</th>
					</tr>
				</thead>
				<tbody>
					<WireframeRow />
					<WireframeRow />
					<WireframeRow />
					<WireframeRow />
					<WireframeRow />
				</tbody>
			</table>
		)
	}
});

var WireframeRow = React.createClass({
	render() {
		return (
			<tr>
				<td>&hellip;</td>
				<td className="games-times-played">&hellip;</td>
				<td className="games-rating">
					<span className="wide-only">
						{ helper.repeat(10, <i className="glyphicon glyphicon-star-empty"></i>) }
					</span>
					<span className="narrow-only">&hellip;</span>
				</td>
			</tr>
		);
	}
});

module.exports = Collection;