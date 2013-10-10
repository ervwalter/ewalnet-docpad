---
layout: post
title: "Butterfly Labs Jalapeno: Ten Months and Two Weeks Later"
date: 2013-05-13 22:00
published: true
comments: true
tags: Bitcoin Gadgets
---

*Update 8/15/13: After reading this post, see [this followup post](/2013/08/15/bitcoin-mining-update-butterfly-labs-sc-singles/) for more information.*

I admit it.  I have been a closet [Bitcoin](http://bitcoin.org) geek for more than two years. Yes, the unfinished portion of my basement is full of GPUs running at full bore 24/7. My wife used to be annoyed by the noise, but that was when Bitcoins were [worth $5](http://preev.com/) each. Now, she asks if we should add more. :)

Anyway, my interactions with [Butterfly Labs](http://butterflylabs.com) started in January, 2012 when I ordered two BFL Single [FPGA](http://en.wikipedia.org/wiki/Field-programmable_gate_array) miners from them.  And then I waited.  And waited.  But after a frustratingly long time, my FPGA miners actually arrived, and even better, worked (and have kept working for more than a year).

Fast forward to June, 2012.  Butterfly Labs announces they are going to develop and sell the BFL SC Single, an [ASIC](http://en.wikipedia.org/wiki/Application-specific_integrated_circuit) miner for Bitcoin that will hash at almost 100x the speed of a typical GPU for about the same amount of electricity.  Has to be a scam, right?  Then they announce that they are going to let current FPGA owners trade up to an ASIC miner and get full credit for the price they paid for their FPGA miner.  Oh, and they will also be developing and selling a tiny little USB miner called the Jalapeno for about $150 that will double as a coffee cup warmer.  For whatever reason, I decided to place an order to upgrade my two FPGA miners, and what the heck, I ordered one Jalapeno miner also for the fun of it.

I placed my order on the afternoon of June 23, 2012.  My order numbers were 1662 and 1666.

And then the waiting. And waiting.  I should have learned my lesson with the FPGAs, right?

Imagine my surprise when, two weeks ago, I arrive home to find a notice on my door that the postman had tried to deliver a package from Butterfly Labs that required a signature. You'd think I would have received an email ahead of time letting me know that it had shipped.  Nah.

I had heard of a few others getting their Jalapenos.  I assumed, then, that this package contained my Jalapeno, and the next day I confirmed my suspicions were correct.

The packaging was decent (you can find unboxing videos on YouTube), and the device was easy to setup.  I had already compiled a version of [cgminer](https://bitcointalk.org/index.php?topic=28402.0) with support for the Jalapeno and so all I really had to do was plug my Jalapeno in to USB and power and start up the miner.  Sadly, my package did not include a PCIx power adapter (as some of the early development units apparently did) and so I am powering mine with the standard power brick.

It's been running non-stop without any problems now for two weeks.  Here are the relevant statistics:

* Hashrate: **5.59 GH/s**
* Reject Rate (using stratum/btcguild): **0.57%**
* Hardware Error Rate (yay, cgminer): **0.00038%**
* Power Usage (while mining): **43.6 watts**
* Power Usage (while idle): **29.6 watts**

Of special note is the fact that my Jalapeno seems to use more power that most other reports I've read from others who have received their early Jalapeno.  There seems to be some variation in power usage with early units, but most others were around 30 watts while mine is at 43.6 watts.  Interestingly, my idle power usage is 29.6 watts which is also higher than the reports I have seen from others.

I'm not at all sure what the difference is, nor am I going to complain (too much).  In the two weeks I have had it, the Jalapeno has earned more than enough BTC to cover the $150 I paid for it.  Yes, I paid for it in BTC, but I used BTC that I had purchased that weekend for the purpose, so I choose not to dwell on what would have happened if I bought the BTC and just held on to it until today.

Now, I guess it is time to return to waiting for my BFL SC Singles. Maybe this time, I'll get a shipment notification in advance so that I know to have someone at the house to sign for the packages...

**Update:** My BFL SC Singles have arrived! [Read the update](/2013/08/15/bitcoin-mining-update-butterfly-labs-sc-singles/).

Obligatory pictures...

<img class="fancybox" src="/stuff/bitcoin-jalapeno-front.jpg" width="300" />
<img class="fancybox" src="/stuff/bitcoin-jalapeno-back.jpg" width="300" />

<img class="fancybox" src="/stuff/bitcoin-jalapeno-cgminer.png" width="300" />
<img class="fancybox" src="/stuff/bitcoin-jalapeno-power.jpg" width="300" />



