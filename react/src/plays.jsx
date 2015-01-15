var React = require('react');
var Fluxxor = require('fluxxor');
var helper = require('./helper');
//var qtip = require('qtip2');
var TooltipMixin = require('./tooltipMixin');
var Tooltip = require('./tooltip');

var Plays = React.createClass({
	mixins: [Fluxxor.FluxMixin(React), Fluxxor.StoreWatchMixin('PlaysStore')],

	getStateFromFlux() {
		var store = this.getFlux().store('PlaysStore');
		return {
			loading: store.loading,
			plays: store.plays
		};
	},

	componentDidMount() {
		this.getFlux().actions.loadPlays();
	},

	render() {
		return (
			<div className="row">
				<div className="col-xs-12">
					<h2>Most Recently Played Games
					&nbsp;-&nbsp;
						<a href="http://boardgamegeek.com/plays/bydate/user/edwalter/subtype/boardgame">full list</a>
					</h2>
					<PlaysTable loading={this.state.loading} plays={this.state.plays} />
				</div>
			</div>
		);
	}
});

var PlaysTable = React.createClass({
	render() {
		if (this.props.plays.length === 0) {
			return <WireframePlaysList />;
		} else {
			var plays = this.props.plays;
			var items = [];
			for (let i = 0; i < plays.length && i < 10; i++) {
				items.push(<PlayItem play={plays[i]} key={plays[i].key}/>);
			}
			return (
				<div className="games-recent">{items}</div>
			);
		}
	}
});

function htmlEncode(value) {
	return $('<div/>').text(value).html();
}

var PlayItem = React.createClass({
	mixins: [TooltipMixin],
	componentDidMount() {
		this.refs.image.getDOMNode().addEventListener('load', this.onImageLoad);
	},
	componentWillUnmount() {
		this.refs.image.getDomNode().removeEventListener('load', this.onImageLoad)
	},
	onImageLoad(e) {
		var img = e.target;
		if (img.classList) {
			img.classList.add('loaded');
		} else {
			img.className = 'loaded';
		}
	},
	tooltipContent() {
		var playDetails = this.props.play.plays.map(entry => <div className="play"><i>Played {helper.relativeDate(entry.playDate)}</i> - {entry.comments}</div>);
		return (
			<Tooltip title={this.props.play.name} className="plays">{playDetails}</Tooltip>
		);
	},
	render() {
		var play = this.props.play;
		return (
			<a ref="link" className="games-recent-item" href={"http://boardgamegeek.com/boardgame/" + play.gameId }>
				<img ref="image" src={play.thumbnail} alt={play.name} />
			</a>
		);
	}
});

var WireframePlaysList = React.createClass({
	render() {
		return (
			<div className="games-recent wireframe">
				{ helper.repeat(10, <a className="games-recent-item">
					<img src="/images/blank.gif" />
				</a>, 10) }
			</div>
		)
	}
});


module.exports = Plays;