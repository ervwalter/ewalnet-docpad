var Fluxxor = require('fluxxor'),
	helper = require('./helper'),
	qtip = require('qtip2');

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
					<h2>Most Recently Played Games - <a href="http://boardgamegeek.com/plays/bydate/user/edwalter/subtype/boardgame">full list</a></h2>
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
			for (let i=0; i < plays.length && i < 10; i++) {
				items.push(<PlayItem play={plays[i]} key={plays[i].key}/>);
			}
			return (
				<div className="games-recent">{items}</div>
			);
		}
	}
});

function htmlEncode (value) {
	return $('<div/>').text(value).html();
}

var PlayItem = React.createClass({
	componentDidMount() {
		var el = this.refs.link.getDOMNode();
		if ($) {
			var play = this.props.play;
			var playDetails = this.props.play.plays.map(entry => {
				return "<i>Played " + helper.relativeDate(entry.playDate)
					+ "</i> - " + htmlEncode(entry.comments);
			});
			var tooltipContent = playDetails.join("<br/><br/>");
			$(el).qtip({
				content: {
					title: play.name,
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
	},
	componentWillUnmount() {
		var el = this.refs.link.getDOMNode();
		if ($) {
			$(el).qtip('destroy');
		}
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
				{ helper.repeat(10, <a className="games-recent-item"><img src="/images/blank.gif" /></a>, 10) }
			</div>
		)
	}
});


module.exports = Plays;