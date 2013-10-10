---
layout: post
title: "Bitcoin Mining Update: Butterfly Labs SC Singles"
date: 2013-08-15 22:00
published: true
comments: true
tags: Bitcoin Gadgets
---

Since I get regular questions about [my last Bitcoin post](/2013/05/13/butterfly-labs-jalapeno-ten-months-and-two-weeks-later/), I thought it might be helpful to post a brief update.

<img class="fancybox" src="/stuff/bitcoin-miners.jpg" width="415" />
<img class="fancybox" src="/stuff/bitcoin-dashboard.png" width="300" />

## BFL SC Singles!

As you can see, I did finally receive my two BFL SC Singles from [Butterfly Labs](http://butterflylabs.com).  I've had them now for about 6 weeks (they arrived around June 25th), and everything is running smoothly.  Each SC Single runs at about 59-60 GH/s and each uses about 280 watts under load.  Also, my Jalapeno is now running at almost 8 GH/s thanks to a friend from work helping me update it to newer firmware.  And yes, I have a couple ASICMiner USB Eruptors, just for fun.

You probably noticed that I have removed the cases from all of the BFL miners.  You can't tell from the photos, but I also flipped the fans on top of each miner over so that they blow down instead of up (which is better cooling).  The SC Singles also used to have fans on each end blowing sideways.  I removed those and instead just use some larger table fans (running on low speed) to keep air moving in the area.

The main benefit in removing the cases for the miners is that they are *much* quieter without the metal grates that were previously up against the fans.  With the normal cases on, the SC Singles were making an very annoying and reasonably loud whining noise that was audible even through the walls and the ceiling of the unfinished basement room this stuff is in (which was not cool with my wife).  With the cases removed, this entire setup is reasonably quiet.  Much quieter than my old GPU mining operation, and cooler as well.

In the 6 weeks I have had the SC Singles, they have earned what I paid for them a year ago (in USD) several times over, so I am quite happy.

## Dashboard

I also updated my custom bitcoin mining dashboard so that I could keep a close eye on these since a mining downtime means non-trivial loss of BTC with these guys.  A couple notes about what you see there:

* The dashboard has dual purposes.  It summarizes my mining activity and shows stats for each of my active miners. It also acts as a frontend to my bitcoind server, allowing me to see transactions, send BTC to people, create new addresses, etc.
* The hashrates shown are all *calculated* based on the number of *accepted shares* found recently.  This is why, for example, the ASICMiners are showing 311 MH/s and 292 MH/s instead of the 334 MH/s they actually are doing.  In this case, they happen to have been a bit unlucky in the past three hours (i.e. normal variance) and so the calculated hashrate is a bit low.  I could show the hashrate as reported by cgminer, but since I get paid based on *accepted shares*, I prefer to see hashrates based on *accepted shares*.
* You can see that I am currently using the "balance" feature of cgminer to evenly divide my mining activity across three different pools.
* The screenshot has been sanitized to remove my personal details.  In the version *I* see, there are real pool names, real numbers, and real transaction details.

Because I am sure that I will get asked... The code for this dashboard is not currently available for others to use.  However, that may change at some point soon.  I just got back from a tech conference, and I think I am going to revamp a few parts of the dashboard as an excuse to play with some of the cool stuff I learned about.  In the process, I'll probably write a blog post or two about how it works, and some form of the code will probably end up on GitHub.

So stay tuned, but be warned, the *backend* is not particularly hacker friendly at the moment (it's ASP.NET and SQL Server).  That might also change if I get an itch to experiment with NodeJS and MongoDB, but that probably won't happen for the initial code releases.

Anyway, all is well in my Bitcoin world.  Now, if the difficulty increases would just slow down a little bit...