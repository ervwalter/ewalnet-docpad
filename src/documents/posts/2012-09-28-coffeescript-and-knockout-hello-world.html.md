---
layout: post
title: "CoffeeScript and Knockout: Hello, World!"
date: 2012-09-28 22:00
published: true
comments: true
tags: CoffeeScript Knockout
---

I recently decided to try my hand at writing some [CoffeeScript](http://coffeescript.org/), and since a lot of my web development uses [Knockout](http://knockoutjs.com/), I was curious if the two would play nicely together.  It turns out that they fit together very well.

Let's start with the standard [Hello, World](http://knockoutjs.com/examples/helloWorld.html) example.  Hit the link for the full example if you're not familiar with it.  Here, I'm only going to focus on the script.

Here is the CoffeeScript version of the example's JavaScript:

``` coffeescript
class ViewModel
	constructor: (first, last) ->
		@firstName = ko.observable(first)
		@lastName = ko.observable(last)
		@fullName = ko.computed =>
			@firstName() + " " + @lastName()

$ ->
	ko.applyBindings(new ViewModel("CoffeeScript", "Fan"))
```

You can try this example live on [jsFiddle](http://jsfiddle.net/ervwalter/FNDep/).

A few things are worth pointing out in the example.

First is the use of `@`.  When I first started learning CoffeeScript, I missed the fact that `@` was an alias for `this`.  So in the example above, `@firstName` is equivalent to `this.firstName`.

The second thing to notice is that I used the fat arrow (`=>`) for the computed observable in order to ensure that `this` is set correctly when the function runs.  I could have alternatively passed `this` as a second parameter to `ko.computed()` but I think that the approach above is easier to read.

Third, the last two lines may look a little odd if you haven't seen the pattern before, but this is a simple way to write a handler for the jQuery document ready event.

When processed by CoffeeScript, the code above turns into familiar looking JavaScript:

``` javascript
var ViewModel;

ViewModel = (function () {

	function ViewModel(first, last) {
		var _this = this;
		this.firstName = ko.observable(first);
		this.lastName = ko.observable(last);
		this.fullName = ko.computed(function () {
			return _this.firstName() + " " + _this.lastName();
		});
	}

	return ViewModel;

})();

$(function () {
	return ko.applyBindings(new ViewModel("CoffeeScript", "Fan"));
});
```

Note that the fat arrow (`=>`) has introduced the use of `_this` in the computed observable.  This is essentially the same pattern that people use when they define `var self = this;` and then use `self` instead of `this` in closures in their view model, but it is done automatically by the CoffeeScript compiler.

In the end, the score is 9 lines of CoffeeScript instead of 20 lines of JavaScript and the CoffeeScript version is (in my opinion) easier to read with a higher signal-to-noise ratio.

This is just a very simple example, and it doesn't really exercise the full power of CoffeeScript. The next few posts will walk through a few more scenarios including CoffeeScript versions of some of the more complex Knockout samples and examples of how to use the Knockout mapping plugin with CoffeeScript.

Next post in this series: [Knockout Samples in CoffeeScript](/2012/10/04/knockout-samples-in-coffeescript/)
