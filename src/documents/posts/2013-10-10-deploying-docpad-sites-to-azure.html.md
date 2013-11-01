---
layout: post
title: "Deploying DocPad Sites To Windows Azure"
date: 2013-10-10 19:16
published: true
comments: true
tags: DocPad Azure NodeJS
---

There are many ways you can deploy a [DocPad](http://docpad.org) site.  One of the simplest options is just to generate a static set of HTML/JavaScript/CSS/etc files and upload them to your favorite web host.  Or you can upload your source files to a Node.js host and run the DocPad Node app interactively.  I'm going to talk about a hybrid approach, specifically with Windows Azure Web Sites.

## Why Azure?

I like Azure.  I'm not going to try to argue that it's 'better' than other cloud hosting vendors.  A lot of it comes down to personal preference.  Part of it comes from my background as a Microsoft-ecosystem developer (C#, ASP.NET MVC, etc).  But Azure is not just a cloud provider for Microsoft-ecosystem projects anymore.  They've broadened their horizons in the past few years, and have made a push to attack developers from popular non-Microsoft communities to Azure including Node.js, PHP, Ruby, and Python developers.

Windows Azure [Web Sites](http://www.windowsazure.com/en-us/documentation/services/web-sites/) are an interesting service.  They are similar to services like Heroku in that they abstract away the management of the platform and OS and you just deploy 'apps'.  They have free, cheap, and not-cheap pricing tiers depending on what features you want and what level of scalability you need. 

A couple of my other [projects](/projects/) are hosted on Azure Web Sites, and [TrendWeight](https://trendweight.com), in particular, is running on the 'standard' tier which essentially means I have an entire VM to myself that I can use for as many web apps as I want.

## Static Site vs Live Node.js App?

As I mentioned, DocPad sites can be either run as live Node.js apps, or you can generate a static site on your development machine and upload the static files.  There are pros and cons to each.  

If you run a live Node.js app, you can write additional code to change the behavior of the app at runtime (e.g. dynamically intercepting requests and redirecting them based on whatever criteria you like, etc.)  You upload your DocPad source files to the web server and they get run by Node.js dynamically at runtime.

My blog doesn't need that level of dynamic behavior, so my preference was for a static site just for raw speed.  Azure (IIS 8.0, really) is really good at serving up static files, and core IIS functionality, configured by web.config, can handle all the extra stuff I need (e.g. redirecting requests that leave the 'www' off the hostname).

But what I _didn't_ want to have is a painful authoring workflow:

1. Write a blog post by creating a markdown file
2. Commit my markdown file to my local git repo
3. Open a terminal window
4. Manually run `docpad -e static clean` and then `docpad -e static generate` to regenerate the static files
5. Copy the generated static files to Azure via one of their supported deployment options.

I was intrigued by the Windows Azure [deployment instructions](http://docpad.org/docs/deploy) on the DocPad site, but they didn't do exactly what I wanted and more importantly, they don't work anymore (Microsoft must have changed something to break them).  The idea of this approach is that you push your DocPad _source_ to Azure and then leverage the fact that Azure servers already have Node.js installed on them (since they support running Node.js apps) and run the DocPad generation command directly on the Azure server at deployment time instead of on your local development workstation.

<img class="fancybox border float-right" src="/stuff/docpad-azure-deploy.png" width="250"/>

I chose to use the GitHub integration, but you can also use DropBox or simply `git push` to get your files up to Azure. With this process, the authoring workflow looks like this:

1. Write a blog post by creating a markdown file
2. Commit my markdown file to my local git repo
3. Push my changes to GitHub

GitHub notifies Azure about the new commits, Azure pulls them, and runs the deployment script to generate fresh static files, then copies the new set of static files to the folder where the site is being served from (see the screenshot for an example).

## How?

Put these three files in the root folder of your source tree (next to docpad.coffee) and push your changes to Azure:

* [.deployment](https://github.com/ervwalter/ewalnet-docpad/blob/master/.deployment)
* [web.config](https://github.com/ervwalter/ewalnet-docpad/blob/master/web.config)
* [azure-deploy.cmd](https://github.com/ervwalter/ewalnet-docpad/blob/master/azure-deploy.cmd)

These files are _almost_ completely generic and can be made to work with any DocPad site, not just _my_ docpad site.  They automatically figure out things like the version of node your project requires, etc.  There is essentially only one thing you might need to tweak in azure-deploy.cmd, and it's explained below.

That said, I'll explain how so that they are less magical...

### .deployment

The `.deployment` file is a special file that Azure looks at to decide how to deploy your site.  If you have a `.deployment` file, Azure will do what it says instead of trying to auto-detect what kind of project you have.  Our file is simple and just tells Azure to run the `azure-deploy.cmd` script to do the actual deployment:

```
[config]
command = azure-deploy.cmd
```

### web.config

This `web.config` file is just a dummy file.  It is _not_ the `web.config` file that will actually be used by your website once deployed.  If yout want a `web.config` file for your site, you should put it in `./src/files/web.config` and it will be copied to the root of your site by the DocPad static file generation process.  

This file is just there to trick Microsoft's node/npm version detection script that we're going to use.  If you don't have one, it gives you a (benign) warning message currently.  Technically this file isn't necessary today, but I include one anyway because in the future Microsoft may change their script to do something not-benign if they don't detect an existing `web.config`.

### azure-deploy.cmd

This is the meat of the magic.  It does these main things:

#### Setup

The Setup' section initializes a number of variables that hold things like the path to our source code and the path to the root folder of our web site.

#### Node Version Detection

Next, this code uses a Microsoft-provided script to parse our package.json file to determine what version of Node and NPM we need and it initializes a couple variables to hold the paths to those two executables.

``` dos
:: 1. Select node version
call :SelectNodeVersion
```

#### Install Modules

Next, this section of code runs `npm install` to install the required node modules.

``` dos
:: 2. Install npm packages
echo Installing npm packages...
pushd "%DEPLOYMENT_SOURCE%"
call !NPM_CMD! install --production
IF !ERRORLEVEL! NEQ 0 goto error
popd
```

I use an [npm-shrinkwrap.json](https://npmjs.org/doc/shrinkwrap.html) file to ensure that dependencies only change when I have tested them on my development machine.  Keep in mind that if you remove a dependency, it will not get removed from the `node_modules` folder on azure automatically with this approach.  Usually, that isn't a problem and just amounts to a small amount of wasted disk space.  I did experiment, for a while, with deleting the `node_modules` folder and reinstalling everything from scratch with every deployment, but that made deployments take a very long time, so I stopped doing that.

#### Generate Static Files

The next bit of code runs `docpad -e static generate` to generate your site.  Again, prior to running the command, it deletes the existing `out` folder so that you always have exactly the right files every time you deploy even if you remove files over time.

``` dos
:: 2. Build DocPad site
echo Building DocPad site...
pushd "%DEPLOYMENT_SOURCE%"
rd /s /q out
IF !ERRORLEVEL! NEQ 0 goto error
"!NODE_EXE!" .\node_modules\docpad\bin\docpad -e static generate
IF !ERRORLEVEL! NEQ 0 goto error
popd
```

#### Copy Files

Finally, we use the Microsoft file-copy tool to copy the generated files to the root folder of our web site:

``` dos
:: 3. KuduSync
echo Copying Files...
call %KUDU_SYNC_CMD% -v 500 -i "posts;drafts" -f "%DEPLOYMENT_SOURCE%\out" -t "%DEPLOYMENT_TARGET%" -n "%NEXT_MANIFEST_PATH%" -p "%PREVIOUS_MANIFEST_PATH%"
IF !ERRORLEVEL! NEQ 0 goto error
```

__Note:__ This is the one section you might need to tweak.  Notice the `-i "posts;drafts"` in the third line.  That tells the [KuduSync](https://github.com/projectkudu/KuduSync.NET) tool to not copy anything in the `posts/` or `drafts/` folders.  This is specific to my particular blog and you may want to remove it or modify it for your own purposes.  I don't copy anything from `posts/` because I am using the [dateurls](https://github.com/mgroves84/docpad-plugin-dateurls/) plugin which changes the URLs of documents in that folder to live elsewhere.  I ignore that folder because I don't want the left over files from the `posts/` folder to be copied to the live site. I also exclude files in the 'drafts/' folder because, well, they are drafts and I don't want them to appear on the live site.  I'll talk more about how I handle draft posts in my next blog post.

## Summary

That's it.  Add those three files and you get easy continuous integration support with Azure and DocPad.  Pretty sweet.

