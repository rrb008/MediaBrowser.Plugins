MediaBrowser.Plugins
====================

This repository contains all of the plugins managed by the Media Browser core team.

Each of the projects has a build event that copies it's output to the programdata/plugins folder. 

By default this assumes you have the server repository side by side in a folder called 'MediaBrowser', as well as the Media Browser Theater repository in a 'MediaBrowser.Theater' folder. If this is not the case, or if you've installed the server and/or theater, than you'll need to update the build events manually in order to test code changes.


## More Information ##

[How to Build a Server Plugin](https://github.com/MediaBrowser/MediaBrowser/wiki/How-to-build-a-Server-Plugin)
