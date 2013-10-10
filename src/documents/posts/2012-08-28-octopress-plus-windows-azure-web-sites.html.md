---
layout: post
title: "Octopress + Windows Azure Web Sites"
date: 2012-08-28 22:00
comments: true
tags: Azure Octopress
---

## Why?

Why not!  I'm a fan of Azure and wanted to see if it would work well with their git publishing.  It turns out that publishing to an Azure Web Site via git is pretty much like publishing to Github Pages.

## How?

The built in `rake setup_github_pages` won't work, so you'll have to do what would have done manually, but that's not too hard.

First, while in the top level folder for your octopress blog, run this command to rename the existing origin repository (the place you got octopress from when you cloned it):

``` none
git remote rename origin octopress
```

Then create and initialize the deployment directory for your Azure Web Site, and do an initial push:

``` none
mkdir _azure
cd _azure
git init .
echo "Hello, World!" > index.html
git add .
git commit -m "initial commit"
git remote add origin https://yourusername@yoursite.scm.azurewebsites.net/yoursite.git
git push origin master
```

Of course, replace `yourusername` and `yoursite` appropriately. This should prompt you for your the git username/password you setup on the Azure site and publish your empty site.  If you go to your site, you should see "Hello, World!"

Last edit, the Rakefile and make sure you set these three variables (they are each near the top)

``` ruby
deploy_default = "push"
deploy_branch = "master"
deploy_dir = "_azure"
```

Lastly, in _config.yml, make sure that the url variable is set to your site's url.

That's it.  Now, you can publish your site by running `rake deploy` or `rake gen_deploy`.