---
layout: post
title: "Bitcoin Command: Fun with AngularJS, NodeJS, and MongoDB"
date: 2013-09-24 12:30
published: true
comments: true
tags: Bitcoin AngularJS NodeJS MongoDB
---

This is one of two posts about my Bitcoin Command app.  

* The [first post](/2013/09/24/bitcoin-command-a-mining-slash-wallet-management-web-app/) focuses on the actual functionality of the app and will be of interest mostly to other Bitcoin miners.
* The second post (this post) focuses on the technology I used to create the app and may be of interest to other web developers regardless of their interest in Bitcoin.

## Overview

Bitcoin Command Center is a relatively simple (in the grand scheme of things) single page application built with [AngularJS](http://angularjs.org/), [NodeJS](http://nodejs.org/), and [MongoDB](http://www.mongodb.org/).

I suppose I am a little late to the party, but this fall I attended [That Conference](http://www.thatconference.com/) and got excited to play around with AngularJS based on a couple of the sessions I attended there.  Rewriting my Bitcoin dashboard seemed like an ideal project to use as an excuse for playing around with a bunch of new-to-me technology.  So, I decided to not just try out AngularJS for the first time, but I also decided to use NodeJS and MongoDB in my new dashboard for added excitement.  My usual development stack is ASP.NET MVC, Knockout, and SQL Server, so this was really almost entirely new ground for me.  Fun!!

They are not new to me, but I also used [CoffeeScript](http://coffeescript.org/), [Compass](http://compass-style.org/), and [Bootstrap](http://getbootstrap.com/) with this project.

I have to say that I love the way things turned out and these technologies work *very* well together.  I certainly would consider using them again for future projects.

At a very high level, this is a fully client-side [single page application](http://en.wikipedia.org/wiki/Single-page_application) (or SPA) written in AngularJS.  The application gets data from the web server via a set of REST APIs that are implemented in NodeJS.  The REST API wraps a backend MongoDB database that stores JSON documents.

## The Lay of the Land

First, the code can be found here:

<https://github.com/ervwalter/bitcoin-command>

Let me start with a review my high level folder structure:

``` none
config/
public/
    sass/
    scripts/
        compiled/
        controllers/
        directives/
        services/
        vendor/
        app.coffee
        filters.coffee
    stylesheets/
    templates/
    index.ejs
server/
app.coffee
```

And here is a bit of explanation for the key pieces:

* `app.coffee`: This is the top level Node entry point.  It sets up the Express server
* `server/`: This folder contains all the .coffee files that make up my server-side Node code including all of the code for my REST API.
* `config/`: This is where configuration files live.  I am using the [node-config](https://github.com/lorenwest/node-config) module which allows me to define a default set of configuration options (which are in the GitHub repository) and then override them with host-specific settings on my development and production servers. 
* `public/`: This folder contains all of the files that the browser uses for the SPA including all the JavaScript, CSS, Images, etc.
* `public/index.ejs`: This is the main HTML file for the client-side application.  The only reason it is an EJS file is so that it can dynamically switch between debug and minified versions of the included JavaScript files based on a debug configuration flag.
* `public/sass/`: This is where all of the Compass source files (.scss) live.  They get compiled into .css files that end up in the `public/stylesheets/`.
* `public/scripts/`: All of the client side scripts live here...
    * `vendor/`: These are all the third party JavaScript libraries.  They get bundled into a single `libraries.js` file for the browser to download.
    * `controllers/`, `directives/`, `services/`, `filters.coffee`, `app.coffee`: These are my AngularJS source files.  They get compiled into a single `app.js` for the browser to download by the CoffeeScript compiler.
    * `compiled/`: This is where the compiled JavaScript files (mentioned above) end up.
* `templates/`: This folder holds all of the .html template files that my AngularJS application uses.  They get [compiled](https://github.com/ericclemmons/grunt-angular-templates) into a single `templates.js` file that the browser downloads so that AngularJS doesn't have to load each template with a separate HTTP request.

## NodeJS

I found Node to be be very easy to work with.  Besides static files (CSS, JavaScript, images, etc), I have, essentially, only REST APIs exposed by my Node server using [Express](http://expressjs.com/).

Each REST API endpoint is defined in my main `app.coffee` file like this:

``` coffeescript
security = require('./server/security')
mining = require('./server/mining')
wallet = require('./server/wallet')

# code to initialize the express app appears here...

# setup the REST API routes
app.post '/submitshare', noCache, mining.submitshare
app.get '/mining/summary', noCache, security.requireAuthentication, mining.summarydata
app.get '/mining/chart', noCache, security.requireAuthentication, mining.chartdata

app.get '/wallet/summary', noCache, security.requireAuthentication, wallet.summary
app.get '/wallet/price', noCache, security.requireAuthentication, wallet.price
app.get '/wallet/recentRecipients', noCache, security.requireAuthentication, wallet.recentRecipients
app.post '/wallet/send', noCache, security.requireAuthentication, wallet.sendTx
```

In those routes, `security.requireAuthentication` is part of how I implemented authentication between AngularJS and NodeJS.  I plan to explain that in a separate blog post.  `noCache` is a simple Express middleware function that ensures appropriate HTTP headers are included to prevent the browser from caching the API results:

``` coffeescript
noCache = (req, res, next) ->
    res.header('Cache-Control', 'no-cache, private, no-store, must-revalidate');
    next()
```

## AngularJS

I *really* like AngularJS.  When I first looked at it a long time ago, I was turned off by its complexity and its learning curve.  But now that I have taken the time to get up to speed, I am impressed by how robust it is.  I still like KnockoutJS and will probably continue to use it from time to time, but I think AngularJS may become my preferred framework anytime I am making something sophisticated.

I wrote all my AngularJS source in CoffeeScript, and found that to be very clean.  I will post about my development environment in greater detail in a separate post, but in a nutshell, my build script compiled all of the various .coffee source files from my `public/scripts/` source tree into a single app.js file for the browser to use.

The only thing I am not entirely satisfied with yet is minification of my application source.  I just can't bring myself to hand-write [minification-safe AngularJS code](http://thegreenpizza.github.io/2013/05/25/building-minification-safe-angular.js-applications/).  I tried using [ngmin](https://github.com/btford/ngmin) to automatically convert my happy, clean AngularJS code into minification-safe code, but it didn't catch 100% of the use cases.  

As a result, I am currently opting for suboptimal minification of my application code.  I use an [uglify](https://github.com/mishoo/UglifyJS2) configuration that removes whitespace but doesn't rename variables.  In the end, my app code amounts to only about 6k when gzipped, so it's not tragic that I am not fully minifying it.

I ran into a couple interesting scenarios as part of my project that I plan to write detailed posts about (stay tuned):

* __Security & Authentication__ There are a number of approaches out there for implementing a complete authentication system in an AngularJS + NodeJS application, but I didn't find any of them to be sufficient to my needs, so I created a hybrid that combined several approaches.
* __Form Validation__ There are also a number of resources on the net for cleanly handling form validation in conjunction with Bootstrap.  Again, I didn't find an existing pattern that I was completely happy with.  I'll talk about my approach in a separate post.

## MongoDB

This was my first project using a JSON-focused database.  I have to say that I found it very liberating to not have to define a schema for my data.  That's not to say that it doesn't conceptually have a schema, I just enjoyed not having to worry about "fixing the schema definition" each time I decided to tweak the schema I was using in practice.  I decided to use the [native MongoDB driver](http://mongodb.github.io/node-mongodb-native/) for NodeJS and not something like [Mongoose](http://mongoosejs.com/) because I like the low level, schema-less approach that the native client provides.

The app has just a few collections:

* __db.shares__ stores one JSON document per share found by a device.  All of the statistics shown on the dashboard come from aggregate analyses of this collection
* __db.devices__ and __db.pools__ hold stats about individual devices and mining pools
* __db.savings__ is just a simple collection of manually maintained (I use [Genghis](http://genghisapp.com/) which is AWESOME) documents about details of offline bitcoins I am holding.  These are used only to calculate my total "bitcoin net worth" on the dashboard.
* __db.addresses__ is a utility collection holding details of any addresses that have been archived or renamed.  Since bitcoind doesn't support renaming or archiving of addresses, I handle that with an application level layer based on this collection.

I found MongoDB's aggregation pipeline to be extremely powerful.  In my old ASP.NET/SQL Server dashboard, I had a really painful (and really slow) stored procedure that used multiple sub-select statements to figure out stats by pool.  In MongoDB, it was a (realtively) easy to understand aggregation:

``` coffeescript
pipeline = [
    { 
        $match:  # only include recent shares
            timestamp: $gte: cuttoff
    }
    { 
        $sort: timestamp: 1 # need to sort in order for $last to work below
    }
    { 
        $project: # select the data I care about
            timestamp: 1
            pool: 1
            targetDifficulty: 1
            acceptedDifficulty: { $cond: [
                $eq: ['$result', 'accept']
                '$targetDifficulty'
                0
            ]}
            rejectedDifficulty: { $cond: [
                $ne: ['$result', 'accept']
                '$targetDifficulty'
                0
            ]}
    }
    {
        $group: # group data by pool
            _id: '$pool'
            shares: $sum: '$targetDifficulty'
            accepted: $sum: '$acceptedDifficulty'
            rejected: $sum: '$rejectedDifficulty'
            lastShare: $last: '$timestamp'
    }
    {
        $project:  # clean up the results and add the calculated hashrate property 
            _id: 0
            url: '$_id'
            hashrate: $multiply: [ '$shares', 0.397682157037037 ] # $shares * 2^32 / (3600 * 3) / 1000000
            shares: 1
            accepted: 1
            rejected: 1
            lastShare: 1
    }
]
db.shares.aggregate(pipeline, callback)
```

I plan to write a dedicated blog post about how I used the MongoDB aggregation pipeline to calculate the hashrate histogram that drives the chart at the top of the dashboard.

## Questions...

Anyway, I think that is enough rambling.  If you're interested in exploring the source code for this application, hopefully you found this helpful.  If you have specific questions, please feel free to post them in the comments or email me at <erv@ewal.net>.



