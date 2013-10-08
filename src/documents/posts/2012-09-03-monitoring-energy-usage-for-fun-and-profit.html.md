---
layout: post
title: "Monitoring Energy Usage For Fun and Profit"
date: 2012-09-03
published: true
comments: true
tags: Gadgets CurrentCost PlotWatt Cosm
---

About a month ago, I finally got my CurrentCost EnviR home energy monitor hooked up. Originally sold only in Europe, CurrentCost now is available in the U.S.  However, the websites are a little confusing.  The main website, [currentcost.com](http://currentcost.com) is still mostly for European customers while U.S. customers can go to [currentcost.net](http://currentcost.net) to obtain hardware and support.  I have no idea why they have split things like this because it is very confusing.  North American-style hardware is also available via [Amazon](http://amzn.com/B002J9IDSG?tag=ewalnet-20) and is [prime](http://www.amazon.com/gp/prime/?tag=ewalnet-20) eligible.

## Installation & Setup

I ordered a basic starter package, the usb data cable, and several individual appliance monitors.  All together, the components include:

* Two CT Clamps
* Transmitter Unit
* Receiver Unit
* USB Data Cable
* Four Individual Appliance Monitors

<img class="fancybox border" src="/stuff/currentcost-ct-clamp.jpg" width="180" title="CT Clamps" />
<img class="fancybox border" src="/stuff/currentcost-transmitter1.jpg" width="180" title="Transmitter" />
<img class="fancybox border " src="/stuff/currentcost-receiver.jpg" width="180" title="Receiver" />
<img class="fancybox border" src="/stuff/currentcost-iam2.jpg" width="180" title="Individual Appliance Monitor" />

The CT Clamps are installed inside my electrical panel / breaker box. Doing this required opening the cover of the electrical panel.  Although the installation is very easy (the clamps just clip around the two main power feeds entering the box from the outside of the home), I decided to have an electrician install them.  We had an electrician here for another job and installing these clamps took him only about 30 seconds. He attached the clamps inside the electrical box an then fed the two cables out a small hole in the side of the box.

I mounted the transmitter (the black box in the photo above) to the wall next to the electrical panel and attached the two cables from the clamps.  The transmitter is powered by several "D" batteries that supposedly will last 7 years.  I decided to attach the transmitter with a screw in the wall instead of the included adhesive patch so that it would be easier to remove from the wall to replace the batteries as needed.  The transmitter reads the current power usage and sends it wirelessly to the receiver which can be up to 100 feet away.

The receiver sits on my desk in my office one floor up from the electrical panel.  You can see the display in the photo above.  It gives you some general real time information about your energy usage, but to get full value from this system, I connect the receiver to my Windows box with a USB data cable (sold separately) and send the data to a couple different online sites.  More on that later.

Finally, I also have 4 individual appliance monitors (IAMs).  These are like [Kill A Watt](http://amzn.com/B000RGF29Q?tag=ewalnet-20) power monitors except they transmit their data to the central CurrentCost receiver.  During initial setup, each IAM had to be paired with the receiver, but that was relatively painless.  I have a lot of computers in my house and decided to get enough IAMs to monitor each set of computers.  In theory, the system can have up to 9 of these IAMs at the same time.  I plugged a power strip into each IAM.  I have my office computers on one, my servers on another (NAS, router, etc), and my two bitcoin mining rigs on the last two IAMs.

## Understanding the Data

I send the data from my CurrentCost system to two separate websites: [PlotWatt](http://plotwatt.com) and [Cosm](http://cosm.com).  Each is useful for different reasons.  I'll write a separate blog post later explaining how I get the data to each website (I am not using the "web bridge" device that is available from CurrentCost), but for now, let me just summarize what I think each is good for.

<img src="/stuff/currentcost-cosm-console.png" title="Cosm Dashboard" />
<img src="/stuff/currentcost-plotwatt-cost.png" title="PlotWatt Dashboard" />

I like Cosm for the ability to quickly go to [my dashboard](https://cosm.com/feeds/71541) and see what has been going on recently.  Cosm also makes all my historical data available via JSON and XML APIs, so if I ever wanted to create my own dashboard or analysis tool, I could use them as the backend database.

On the other hand, PlotWatt's mission is to give me an understanding of the cost of the energy I am using.  They analyze the patterns of energy use and over time will be able to tell me how much my refrigerator or A/C is costing me (see their [demo dashboard](https://plotwatt.com/users/demo_login)).  PlotWatt knows what rates my utility company charge, and they convert my electrical usage to an estimate of my next bill.  I can also set budgets for the whole house or an individual appliance and then get notifications when I go over budget.

I'll admit that my inability to resist a cool gadget was probably the real reason I bought this system in the first place.  That said, it has shed some light on how much my computers cost me (and how much my [bitcoin](http://bitcoin.org/) hobby costs me, in particular).  And sometimes there are interesting patterns in the data.  For example, the graph below is the energy usage for my Synology NAS:

<img class="fancybox" src="/stuff/currentcost-plotwatt-timemachine-blips.png" width="800" title="Effect of TimeMachine on NAS" />

You'll notice that once an hour, there is a small increase in power usage for the NAS. This is TimeMachine on my Mac Mini doing its hourly backup.  Pretty cool.

<br class="clear" />
