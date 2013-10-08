---
layout: post
title: "Bootstrap + Knockout: Toggle Button Bindings"
date: 2012-10-17
published: true
comments: true
tags: Bootstrap Knockout CoffeeScript
---

I am in the middle of an overhaul of the [TrendWeight](/trendweight/) user dashboards, and as part of that project, I found myself looking for ways to bring [Bootstrap button groups](http://twitter.github.com/bootstrap/javascript.html#buttons) and [Knockout](http://knockoutjs.com/) together in peace and harmony.  In other words, when someone clicks on one of the buttons in the radio button group, I need the associated observable on the ViewModel to be updated with the value associated with that particular radio button.

My first attempt worked fine, but left something to be desired.  Here is the markup I had:

``` html
<div class="btn-group">
    <button class="btn" data-bind="css: {active: range() == '2w'}, click: function() { $root.range('2w') }">2 weeks</button>
    <button class="btn" data-bind="css: {active: range() == '4w'}, click: function() { $root.range('4w') }">4 weeks</button>
    <button class="btn" data-bind="css: {active: range() == '3m'}, click: function() { $root.range('3m') }">3 months</button>
    <button class="btn" data-bind="css: {active: range() == '6m'}, click: function() { $root.range('6m') }">6 months</button>
    <button class="btn" data-bind="css: {active: range() == '1y'}, click: function() { $root.range('1y') }">1 year</button>
</div>
```

That's pretty verbose and repetitive.  This seems like the perfect opportunity to use a custom binding, but custom bindings have always seemed intimidating for some reason.  [Ryan Niemeyer](http://www.knockmeout.net/) has been selling me on the idea that custom bindings are really not that scary and that I should embrace them instead of fearing them.  After receiving a small pep talk this week, I decided to take the plunge.

My goal is to enable markup that looks like this instead of the markup above:

``` html
<div class="btn-group" data-bind="radio: range">
    <button type="button" class="btn" data-value="2w">2 Weeks</button>
    <button type="button" class="btn" data-value="4w">4 Weeks</button>
    <button type="button" class="btn" data-value="3m">3 Months</button>
    <button type="button" class="btn" data-value="6m">6 Months</button>
    <button type="button" class="btn" data-value="1y">1 Year</button>
</div>
```

Here, there is a binding on the btn-group element that says, "Hey, these buttons should act like radio buttons and be bound to the 'range' observable, please."  Additionally, each button in the group has a `data-value` attribute that tells the binding what value should be associated with each button.

In the binding I ended up writing, the `data-value` attribute is actually optional.  If you are ok with the value of your observable being the same as the captions on your buttons, you can leave the attribute off and the binding will use the inner text of the button instead.

While I was at it, I also made the binding handle the alternate markup below where the binding is on each individual button instead of on the button group.  This may be useful if you want radio button behavior without putting the buttons in a button group.  And for variety, this markup specifies the value for each radio button with `radioValue` property in the binding itself (although you the `data-value` attribute still would work as well):

``` html
<div class="btn-group">
    <button type="button" class="btn" data-bind="radio: alignment, radioValue: 'left'">Left</button>
    <button type="button" class="btn" data-bind="radio: alignment, radioValue: 'middle'">Middle</button>
    <button type="button" class="btn" data-bind="radio: alignment, radioValue: 'right'">Right</button>
</div>
```

After all that, I felt empowered and thought I might as well make a binding for checkbox behavior as well.  In other words, each button would be bound to a boolean observable and would be toggled when that observable was true.  So markup like this:

``` html
<div class="btn-group">
    <button type="button" class="btn" data-bind="checkbox: important">Important</button>
    <button type="button" class="btn" data-bind="checkbox: urgent">Urgent</button>
</div>
```

Before I get into the code for the bindings, here is a working [jsFiddle demo](http://jsfiddle.net/ervwalter/ccjnj) that shows the user interface behavior I'm talking about.

<iframe width="100%" height="300" src="http://jsfiddle.net/ervwalter/ccjnj/embedded/result,js,html,css" allowfullscreen="allowfullscreen" frameborder="0"></iframe>

Ok, so the code...  Of course, since I am on a CoffeeScript kick at the moment, I wrote these bindings in CoffeeScript.  If CoffeeScript isn't your thing, you can hit the jsFiddle link above to see the JavaScript version of the bindings.

``` coffeescript
ko.bindingHandlers.radio = {
    init: (element, valueAccessor, allBindings, data, context) ->
        observable = valueAccessor()

        if not ko.isWriteableObservable(observable)
            throw "You must pass an observable or writeable computed"

        $element = $(element)
        if $element.hasClass("btn")
            $buttons = $element
        else
            $buttons = $(".btn", $element)

        elementBindings = allBindings()
        $buttons.each ->
            btn = @
            $btn = $(btn)

            radioValue =
                elementBindings.radioValue || #this is really only useful when the binding is on the button, itself
                $btn.attr("data-value")  ||
                $btn.attr("value")  ||
                $btn.text()

            $btn.on "click", ->
                observable ko.utils.unwrapObservable(radioValue)
                return

            ko.computed disposeWhenNodeIsRemoved: btn, read: ->
                $btn.toggleClass "active", observable() == ko.utils.unwrapObservable(radioValue)
                return

        return
}

ko.bindingHandlers.checkbox = {
    init: (element, valueAccessor, allBindings, data, context) ->
        observable = valueAccessor()

        if not ko.isWriteableObservable(observable)
            throw "You must pass an observable or writeable computed"

        $element = $(element)

        $element.on "click", ->
            observable not observable()
            return

        ko.computed disposeWhenNodeIsRemoved: element, read: ->
            $element.toggleClass "active", observable()
            return

        return
}

```

One caveat: while Knockout does not depend on jQuery, these binding do. If you want to use them, you either have to have jQuery on your page, or you have to rewrite these appropriately.  Since jQuery is a dependency of Bootstrap's JavaScript plugins, I didn't feel like this was an unreasonable dependency here.

The radio button binding is the more interesting of the two bindings.  The first thing it does in the 'init' function (besides some checking for error conditions) is determine if the binding is directly on a button or not.  If it is on a button, it's going to wire up the button directly.  If it is not, it's going to look for any descendant buttons and wire each of them up.

Next, I loop through each of the buttons (which might just be the single button if the binding is directly on a button).  For each button, I first figure out what the radioButton value should be for that button.  Then I wire up a click event handler that sets the observable to the right value.  Finally, I create a computed observable that will fire any time the main observable changes.  The computed observable adds or removes the 'active' css class depending on if the value of the observable matches the button's assigned value.

Normally, you'd put the code to respond to changes from the main observable in a custom binding's 'update' function.  However in this case, I did it with an explicit computed observable because I needed access to the radioValue variable, and there is no convenient way to pass state between the 'init' function and the 'update' function.  Having an explicit computed observable accomplishes the same thing as using an 'update' function because 'update' functions essentially get turned into computed observables behind the scenes anyway.

The checkbox binding is similar but a bit simpler.  It is always used directly on a button, so the part about looping through descendant buttons is not there.  The binding just directly wires up a similar click event handler and creates a similar computed observable to toggle the right css class.  In this case, I *could* have used an 'update' function, but chose not to just to keep it consistent with how I structured the radio button binding.

I think I am mostly over my fear of custom bindings, and I expect that I'll now be looking for opportunities to use them even where they aren't really needed :)
