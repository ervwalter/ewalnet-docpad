var React = require('react');
var Tooltip = require('../vendor/tooltip.min.js');

var gTooltip = new Tooltip("", {
	baseClass: 'tt',
	typeClass: 'custom',
	effectClass: 'slide',
	auto: true
});
var gTooltipOwner = null;

var TooltipMixin = {

	componentDidMount() {
		var el = this.getDOMNode();

		if(el) {
			el.addEventListener('mouseenter', this.mouseenter, false);
			el.addEventListener('mouseleave', this.mouseleave, false);
		} else {
			console.log('no element', this._rootNodeID);
		}
	},
	componentDidUpdate() {
		// We only care if the tooltip is shown and we are the owner.
		if (!gTooltip.hidden && gTooltipOwner === this && this.tooltipContent) {
			this.update(this.tooltipContent());
		}
	},
	componentWillUnmount() {
		var el = this.getDOMNode();

		if (el) {
			el.removeEventListener('mouseenter', this.mouseenter);
			el.removeEventListener('mouseleave', this.mouseleave);
		}
	},

	mouseenter() {
		console.log('enter', this);
		// Assert ownership on mouseenter
		gTooltipOwner = this;

		if (this.tooltipContent) {
			this.update(this.tooltipContent());
		} else {
			console.warn("Component has TooltipMixin but does not provide tooltipContent()");
		}
	},
	mouseleave() {
		// Hide the tooltip only if we are still the owner.
		if (gTooltipOwner === this) {
			gTooltip.detach().hide();
			gTooltipOwner = null;
		}
	},

	update(content) {
		var el = this.getDOMNode();
		if (el) {
			React.render(content, gTooltip.element, function () {
				// Need to tell the tooltip that its contents have changed so
				// it can reposition itself correctly.
				gTooltip.attach(el).show().updateSize().place('top');
			});
		}
	}
}

module.exports = TooltipMixin;