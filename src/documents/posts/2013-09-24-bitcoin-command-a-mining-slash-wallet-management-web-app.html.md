---
layout: post
title: "Bitcoin Command: A Mining & Wallet Management Web App"
date: 2013-09-24 12:00
published: true
comments: true
tags: Bitcoin Gadgets
---

This is one of two posts about my Bitcoin Command app.  

* The first post (this post) focuses on the actual functionality of the app and will be of interest mostly to other Bitcoin miners.
* The [second post](/2013/09/24/bitcoin-command-fun-with-angularjs/) focuses on the technology I used to create the app and may be of interest to other web developers regardless of their interest in Bitcoin.

A month or so ago, in [post about my BFL Singles](/2013/08/15/bitcoin-mining-update-butterfly-labs-sc-singles/), I mentioned that I had a custom bitcoin dashboard that I used to monitor my mining operation.  I also mentioned that I was in the process of rewriting the dashboard and would post the source when I finished that effort.  Well, I am not entirely done with my rewrite, but it is "done enough", so I'm putting it out there for anyone that might be interested in adapting the code to their own operation.

## What Is It?

It's a combination mining dashboard and a web-based frontend to a bitcoind wallet.

Features include...

* Stats on current mining devices and current pools *based on shares submitted to pools*
* Stats on expected and actual income per day
* Pool stats (unpaid balances, etc) for active pools
* A nearly complete wallet frontend
    * Recent transactions list
    * Send bitcoins to others
    * New address creation
    * Address management (renaming labels, archiving address)
* Mobile-optimized to work well on modern smartphone browsers (Safari, Chrome, etc).

Here are some screenshots of current functionality (click to enlarge)...

<img class="fancybox border" src="/stuff/btc-cc-1.png" width="150" />
<img class="fancybox border" src="/stuff/btc-cc-2.png" width="150" />
<img class="fancybox border" src="/stuff/btc-cc-3.png" width="150" />
<img class="fancybox border" src="/stuff/btc-cc-4.png" width="150" />
<img class="fancybox border" src="/stuff/btc-cc-5.png" width="150" />

And on a smartphone...

<img class="fancybox border" src="/stuff/btc-cc-6.png" width="100" />
<img class="fancybox border" src="/stuff/btc-cc-7.png" width="100" />
<img class="fancybox border" src="/stuff/btc-cc-8.png" width="100" />

When the app is actively running, you also see live updates as shares are found by your miners:

<img class="border" src="/stuff/btc-cc-mining.gif" />

## Technical Requirements

First and foremost, **you probably need to be a developer or have a developer friend to get this up and running.**

This is not a polished, ready to install software package aimed at end users.  This is the source code of my own, personal, dashboard, and while I made many things configurable (like where your database is hosted and how to connect to your bitcoind server), many other assumptions are hardwired into the code (like the fact that I prefer the coinbase USD exchange rate API since that is where I sell my BTC). If you would like to use it, you will almost certainly need to have an understanding of the technologies involved and you'll need to be comfortable reading and editing the source code to make things work for your specific setup.  I suggest reading the [other blog post](http://blog.dev/2013/09/24/bitcoin-command-an-adventure-with-angularjs/) as well so that you have a better understanding of how the technical pieces fit together.

The major technologies you'll need to have installed and be comfortable with are:

* [Node.js](http://nodejs.org/) as the web server
* [AngularJS](http://angularjs.org/) as the frontend application framework
* [MongoDB](http://www.mongodb.org/) as the backend database
* [CoffeeScript](http://coffeescript.org/) as the programming language (vs. raw JavaScript)
* [Compass](http://compass-style.org/) for CSS management

These are all open source technologies that, in general, these are all relatively easy to get up and running on a Linux or OS X box and also relatively painful to get running on a windows box.  I, personally, use an OS X box for my development server and a Debian Linux box for my "production" server.

The specific details of getting these up and running are left as an exercise for the reader, but at a high level, the steps are:

1. Get the source code from GitHub: <https://github.com/ervwalter/bitcoin-command>
2. Install Node, Mongo, and Compass
3. Run `npm install -g coffee-script` to install the coffeescript command line tool on your system
4. Run `npm install` from the root directory of this project to install all the require node modules
5. Configure the application by either editing the default.coffee file in the `config` folder, or by creating a [host-specific config file](http://lorenwest.github.io/node-config/latest/)
6. Make sure MongoDB is running
7. Run `coffee app.coffee` to start the web server.
8. Point your web browser at the server you just started.

## Connecting Your Miners

After you get the application working, you'll see an empty and generally useless dashboard.  In order for any of the mining functionality to work, you'll need to feed the dashboard with information from your miners.  

All of the mining statistics that the dashboard shows are calculated based on the *actual shares your miners find*.  That means everytime one of your miners finds a share, you need to automatically submit the specifics of that share in the dashboard.

If you are using cgminer, the easiest solution is to use my [cgminer share monitor](https://github.com/ervwalter/share-monitor/).  This is another NodeJS script that integrates with cgminer's `-sharelog` feature and sends details from each share to the dashboard.  You can either have cgminer log shares to an actual file and just have the share monitor script watch that file for changes, or you can use cgminer's `-monitor` feature to have cgminer pipe the sharelog to the share monitor script without the sharelog ever going to disk.  Details of how to set this up are on [GitHub](https://github.com/ervwalter/share-monitor/wiki).

This script (and the dashboard in general) may work with bfgminer, but I have not personally tested it, so I can't say for sure.

In the next few weeks, I will also be creating a modified version of slush's stratum proxy that submits shares to the dashboard as they are found.  This will be useful in cases where you're mining with a piece of hardware not supported by cgminer (e.g. the BitFury rig that I will be receiving sometime next month).  I'll make another post when that modified proxy is available.

## What's Next?

As I said, I'm not done with this project.  I will be adding at least the following features over the next few weeks:

* Idle or Unhealthy device detection:
    * Email notifications when a device is misbehaving
    * Color coding or some kind of visual indication on the dashboard for unhealthy devices
* Sign/Verify support for the wallet
* Socket.io support for wallet updates.  This will reduce the number of RPC calls made to bitcoind when sitting on the dashboard.  The reduction in RPC calls may even be enough to make it practical to point the app at a [blockchain.info](http://blockchain.info) wallet since they have a bitcoind-compatible RPC interface but restrict the volume of RPC calls you can make.

## Questions

If you have *specific* questions about getting this running, I'd be happy to answer them.  Note, *'How do I get this up and running?'* and *'How do I install NodeJS?'* are not what I mean by *specific*.  On the other hand *'How does the code determine what kind of mining device (Block Eruptor vs BFL SC Single) something is?'* is a perfectly reasonable question.  Post your question in the comments or just email me at <erv@ewal.net>.


