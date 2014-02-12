---
layout: post
title: "Bitcoin Mining with 240V"
date: 2014-02-10 20:22
published: true
comments: true
tags: Bitcoin Gadgets
---

I have two [CoinTerra](http://cointerra.com/) TerraMiner IVs on order, one in the December batch and one in the March batch.  As the December batch orders are now shipping, I spent the past week planning for the device's arrival and wanted to share the details for others in the U.S. that may also need to make similar preparation.

Why are preparations necessary?  First, let me say that I am in the U.S. and all of the following is specific to U.S. electrical standards.  Anyway, it turns out that CoinTerra missed their power utilization goals and these devices use between 1900 and 2100 watts *each*.  That is far too much for a single 110V 15A circuit which is what is typically found in U.S. homes.  In theory, you can connect each of these devices to two separate 110V circuits (they presumably have two 1200 watt power supplies that each will be plugged in), but that is a major hassle if you don't have multiple unused circuits in close proximity.

The solution is to use 240V circuits. Electrical devices use less current (amps) when run at higher voltage and so a single 240V circuit can power a CoinTerra miner (instead of multiple 110V circuits).  Adding a 240V circuit sounds hard if you don't have any experience with electrical work and I had no idea how to even begin to getting that done.  After a little research and a couple conversations with some local electricians, it turned out to not be too bad, after all.

You essentially need two things to make this work:

1. A 240V circuit with an appropriate receptacle.
2. Some way to connect a standard computer [power supply](http://amzn.com/B008Q7HUR0?tag=ewalnet-20) to that circuit.

## Getting Power ##

Getting a 240V circuit should hopefully be straightforward.  In the U.S., home circuits are typically either 110V or 240V, and most home probably already have at least one 240V circuit for things like an air conditioner or an electric range.  Adding another one should be straightforward as long as your breaker box has capacity.  Just call an electrician and ask them to install an additional 240V circuit.  You'll have to let them know what kind of receptacle you want, and how many Amps you want the circuit to be (more on that later).

The only challenge might be finding a location for the receptacle.  If you don't want to keep your Bitcoin miners near your breaker box, you might be looking at a larger project as the electrician may need to run wires through walls (i.e. a much larger project).  In my case, my Bitcoin miners are all in the unfinished portion of my basement right next to the electrical panel, so getting the additional circuits was painless.

## Distributing the Power ##

Choosing the type of receptacle and the number of amps will depend on your specific power needs and what kind of cable you plan to plug into it.

You probably aren't going to want to plug your miners directly into your 240V receptacle.  First you'd need an outlet for each item you wanted to plug in and that is not particularly cost effective for the electrician to install.  Second, and more importantly, you aren't going to easily find a cable that can connect a typical computer power supply to typical high amperage 240V receptacles.  The answer is to get a Power Distribution Unit (or PDU).  These are essentially specialized power strips for computer hardware.  They are typically used in data centers, but can also be used in your home without any significant problems.

After some tips from some folks on bitcointalk.org, I settled on these two PDUs as good choices:

* [Tripp Lite 240V 20A PDU](http://amzn.com/B004P3X4ZQ?tag=ewalnet-20).
* [Tripp Lite 240V 30A PDU](http://amzn.com/B0012VN0I0?tag=ewalnet-20).

The first one is a horizontal PDU that uses a NEMA L6-20P plug and needs a 20 amp circuit with an L6-20P receptacle. The second one is a vertical PDU that uses a NEMA L6-30P plug (similar but not identical) and needs a 30 amp circuit with a L6-30P receptacle.

For my own project, I decided to get one of each and to have the electrician install two circuits: a 20A L6-20P receptacle and a 30A L6-30P receptacle.  I decided to get both because I wanted to convert all my existing mining hardware over to 240V as part of this project because it is slightly more efficient.  I will use the 30A circuit for my CoinTerra miners (at 240V, 2200 watts = 9.2 amps), and I am using the 20A circuit for all the rest of my hardware.

The 20A version can just sit on a shelf easily enough.  I mounted the 30A version to the wall next to my shelves, but had to get creative with the mounting hardware since this is designed to be mounted to a rack in a data center and not to a basement wall :)

To calculate what kind of capacity you'll need, remember that Amps = Watts / Volts.

## Cables ##

Ok, so now you have a PDU, but you'll also need cables to connect the mining power supplies to the PDU.  I bought several of [these](http://amzn.com/B002O0KMJS?tag=ewalnet-20) cables.  They have a C13/C14 connectors that will plug into your PDU on one end and directly into your computer power supply on the other.

## Results ##

Both PDUs are up and running, and I switched all my existing miners over to 240V.  Now I just need to wait for the first TerraMiner to arrive.  Here are a couple photos (click to view larger)...

<img class="fancybox border" src="/stuff/cointerra-outlets.jpg" width="250" />
<img class="fancybox border" src="/stuff/cointerra-l6-30p.jpg" width="250" />
<img class="fancybox border" src="/stuff/cointerra-full-setup.jpg" width="250" />
