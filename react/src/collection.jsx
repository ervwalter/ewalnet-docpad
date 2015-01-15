var React = require('react');
var Fluxxor = require('fluxxor');
var helper = require('./helper');
var Lazy = require('lazy.js');
var TooltipMixin = require('./tooltipMixin');
var Tooltip = require('./tooltip');

var Collection = React.createClass({
	mixins: [Fluxxor.FluxMixin(React), Fluxxor.StoreWatchMixin('CollectionStore')],

	getStateFromFlux() {
		var store = this.getFlux().store('CollectionStore');
		return {
			games: store.games,
			stats: {
				gamesCount: store.gamesCount,
				expansionsCount: store.expansionsCount,
				hIndex: store.hIndex
			}
		};
	},

	componentDidMount() {
		this.getFlux().actions.loadCollection();
	},

	render() {
		return (
			<div className="row">
				<div className="col-xs-12">
					<h2>My Game Collection
					&nbsp;-&nbsp;
						<a href="/menu/">
							<span className="wide-only">today's </span>
							menu</a>
					&nbsp;-&nbsp;
						<a href="http://boardgamegeek.com/collection/user/edwalter?own=1">full list</a>
						<GameStats stats={this.state.stats} loading={this.state.games.length === 0} />
					</h2>
					<CollectionTable games={this.state.games} />
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
			var rows = this.props.games.map(game => <CollectionRow game={game} key={game.gameId} />);

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
	mixins: [TooltipMixin],
	tooltipContent() {
		var game = this.props.game;
		if (game.expansionsOwnedCount > 0) {
			var expansionNames = Lazy(game.expansions).where({owned: true}).sortBy('sortableName').pluck('name').toArray();
			var elements = [];
			var max = expansionNames.length;
			for (let i = 0; i < max; i++) {
				elements.push(<div>{'' + expansionNames[i] + (i < (max - 1) ? ',' : '')}</div>);
			}
			return (
				<Tooltip title="Expansions" className="expansions">{elements}</Tooltip>
			);
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

var GameStats = React.createClass({
	mixins: [TooltipMixin],
	tooltipContent() {
		var hIndex = this.props.stats.hIndex;
		return (
			<Tooltip className="stats">
				<p>My H-index is <b>{hIndex}</b>.</p>
				<p><i>(I have played {hIndex} different games from my collection at least {hIndex} times each)</i></p>
			</Tooltip>
		);
	},
	render() {
		if (this.props.loading) {
			return <span className="hidden"></span>;
		} else {
			return (
				<span className="pull-right hidden-xs game-counts fade-in-slow">
					<b>{ this.props.stats.gamesCount }</b>&nbsp;
					games,&nbsp;
					<b>{ this.props.stats.expansionsCount }</b>&nbsp;
					expansions</span>
			)
		}
	}
});

var WireframeCollectionTable = React.createClass({
	render() {
		return (
			<table className="collection-table wireframe table">
				<thead>
					<tr>
						<th>Name</th>
						<th>
							<span className="wide-only">Times </span>
							Played</th>
						<th>
							<span className="wide-only">My </span>
							Rating</th>
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