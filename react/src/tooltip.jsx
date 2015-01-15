var React = require('react');

var Tooltip = React.createClass({
	render() {
		return (
			<div className={this.props.className}>
				{this.props.title ? <div className="tt-title">{this.props.title}</div> : null }
				<div className="tt-content">{this.props.children}</div>
			</div>
		);
	}
})

module.exports = Tooltip;