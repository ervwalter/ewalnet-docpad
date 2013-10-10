---
layout: post
title: "Knockout Samples in CoffeeScript"
date: 2012-10-04 22:00
published: true
comments: true
tags: Knockout CoffeeScript
---

In my [last post](/2012/09/28/coffeescript-and-knockout-hello-world/), I talked about my initial experiments with using Knockout with CoffeeScript.  In this post, I want to share the results of my conversions of several of the standard Knockout samples.  As someone said in my first post, this stuff isn't exactly rocket science.  On the other hand, I know I often learn new tech more quickly by looking at other people's sample code, so I'm putting these here in case someone else might find them useful.

## Grid Editor

The original Knockout sample page can be found [here](http://knockoutjs.com/examples/gridEditor.html).

My conversion of this one is pretty straightforward.  As a general rule with these conversions, my goal was to remain faithful to the original sample javascript both in how it was solving problems and how the view models were organizaed.  At the same time I tried to use native CoffeeScript features where they made sense (e.g. string interpolation in this example, and comprehensions over arrays in the other examples).

A fully functional version of this conversion can be found in [this jsFiddle](http://jsfiddle.net/ervwalter/4CLRh/), but here is the CoffeeScript:

``` coffeescript
class GiftModel
    constructor: (gifts) ->
        @gifts = ko.observableArray gifts

        @addGift = =>
            @gifts.push
                name: ""
                price: ""

        @removeGift = (gift) =>
            @gifts.remove gift

        @save = (form) =>
            alert "Could now transmit to server: #{ko.utils.stringifyJson @gifts}"

$ ->
    viewModel = new GiftModel(
        [
            { name: "Tall Hat", price: "39.95" }
            { name: "Long Cloak", price: "120.00"}
        ])
    ko.applyBindings viewModel
    $("form").validate submitHandler: viewModel.save
```

## Contacts Editor

The original Knockout sample page can be found [here](http://knockoutjs.com/examples/contactsEditor.html).

This conversion uses a couple more features of CoffeeScript including the use of comprehensions in place of `ko.utils.arrayMap()` and `$.each()`.

A fully functional version of my conversion can be found in [this jsFiddle](http://jsfiddle.net/ervwalter/Rj3pk/).  Here is the CoffeeScript:

``` coffeescript
initialData = [
    { firstName: "Danny", lastName: "LaRusso", phones: [
        { type: "Mobile", number: "(555) 121-2121" },
        { type: "Home", number: "(555) 123-4567"}]
    },
    { firstName: "Sensei", lastName: "Miyagi", phones: [
        { type: "Mobile", number: "(555) 444-2222" },
        { type: "Home", number: "(555) 999-1212"}]
    }
]

class ContactsModel
    constructor: (contacts) ->
        @contacts = ko.observableArray({
                firstName: contact.firstName
                lastName: contact.lastName
                phones: ko.observableArray(contact.phones)
            } for contact in contacts)

        @addContact = =>
            @contacts.push
                firstName: ""
                lastName: ""
                phones: ko.observableArray()

        @removeContact = (contact) =>
            @contacts.remove(contact)

        @addPhone = (contact) =>
            contact.phones.push
                type: ""
                number: ""

        @removePhone = (phone) =>
            contact.phones.remove phone for contact in @contacts()

        @save = =>
            @lastSavedJson JSON.stringify(ko.toJS(@contacts), null, 2)

        @lastSavedJson = ko.observable ""

$ ->
    ko.applyBindings(new ContactsModel(initialData))
```

## Shopping Cart Editor

The original Knockout sample page can be found [here](http://knockoutjs.com/examples/cartEditor.html).

This one is similar to the last two, but includes a second class to hold represent individual items in the shopping cart.

A fully functional version of my conversion can be found in [this jsFiddle](http://jsfiddle.net/ervwalter/qyDr2/), and here is the CoffeeScript:

``` coffeescript
window.formatCurrency = (value) ->
    "$" + value.toFixed(2)

class CartLine
    constructor: ->
        @category = ko.observable()
        @product = ko.observable()
        @quantity = ko.observable 1
        @subtotal = ko.computed =>
            if @product() then @product().price * parseInt("0" + @quantity()) else 0

        @category.subscribe =>
            @product undefined

class Cart
    constructor: ->
        @lines = ko.observableArray [new CartLine()]

        @grandTotal = ko.computed =>
            total = 0
            total += line.subtotal() for line in @lines()
            total

        @addLine = =>
            @lines.push new CartLine()

        @removeLine = (line) =>
            @lines.remove line

        @save = =>
            dataToSave = ({
                productName: line.product().name
                quantity: line.quantity()
            } for line in @lines() when line.product())
            alert "Could now send this to server: #{JSON.stringify dataToSave}"

$ ->
    ko.applyBindings new Cart()
```

## Twitter Client

And last but not least, the original version of the most complex Knockout sample page can be found [here](http://knockoutjs.com/examples/twitter.html).

This one exercises a few more knockout features, but was relatively easy to convert using the same basic CoffeeScript patterns I used in the other samples.

A fully functional version of this conversion can be found in [this jsFiddle](http://jsfiddle.net/ervwalter/95UBd/), and here is the CoffeeScript:

``` coffeescript
savedLists = [
    { name: "Celebrities", userNames: ['JohnCleese', 'MCHammer', 'StephenFry', 'algore', 'StevenSanderson']}
    { name: "Microsoft people", userNames: ['BillGates', 'shanselman', 'ScottGu']}
    { name: "Tech pundits", userNames: ['Scobleizer', 'LeoLaporte', 'techcrunch', 'BoingBoing', 'timoreilly', 'codinghorror']}
]

class TwitterListModel
    constructor: (lists, selectedList) ->

        @savedLists = ko.observableArray lists
        @editingList = name: ko.observable(selectedList), userNames: ko.observableArray()
        @userNameToAdd = ko.observable ""
        @currentTweets = ko.observableArray []

        @findSavedList = (name) ->
            lists = @savedLists()
            ko.utils.arrayFirst lists, (list) ->
                list.name == name

        @hasUnsavedChanges = ko.computed =>
            if not @editingList.name()
                @editingList.userNames().length > 0
            else
                savedData = @findSavedList(@editingList.name()).userNames
                editingData = @editingList.userNames()
                savedData.join("|") != editingData.join("|")

        @userNameToAddIsValid = ko.computed =>
            (@userNameToAdd() == "") || (@userNameToAdd().match(/^\s*[a-zA-Z0-9_]{1,15}\s*$/) != null)

        @canAddUserName = ko.computed =>
            @userNameToAddIsValid && @userNameToAdd != ""

        @addUser = =>
            if @userNameToAdd() && @userNameToAddIsValid()
                @editingList.userNames.push @userNameToAdd()
                @userNameToAdd ""

        @removeUser = (userName) =>
            @editingList.userNames.remove userName

        @saveChanges = =>
            saveAs = prompt "Save as", @editingList.name()
            if saveAs
                dataToSave = @editingList.userNames()[..]
                existingSavedList = @findSavedList(saveAs)
                if (existingSavedList)
                    existingSavedList.userNames = dataToSave
                else
                    @savedLists.push { name: saveAs, userNames: dataToSave }
                @editingList.name(saveAs)

        @deleteList = =>
            nameToDelete @editingList.name()
            savedListsExceptOneToDelete = (list for list in @savedLists() when list.name != nameToDelete)
            @editingList.name(if savedListsExceptOneToDelete.length == 0 then null else savedListsExceptOneToDelete[0].name)
            @savedLists savedListsExceptOneToDelete

        ko.computed =>
            savedList = @findSavedList @editingList.name()
            if savedList
                userNamesCopy = savedList.userNames[..]
                @editingList.userNames userNamesCopy
            else
                @editingList.userNames []

        ko.computed =>
            twitterApi.getTweetsForUsers @editingList.userNames(), @currentTweets

$ ->
    ko.applyBindings new TwitterListModel(savedLists, "Tech pundits")
    $(".loadingIndicator").ajaxStart(-> $(@).fadeIn()).ajaxComplete(-> $(@).fadeOut())
```

## Conclusion

In all of these conversions, I feel like the resulting CoffeeScript is easier to read and easier to maintain than the original JavaScript versions.  I will definitely be writing in CoffeeScript for my next Knockout-using project.