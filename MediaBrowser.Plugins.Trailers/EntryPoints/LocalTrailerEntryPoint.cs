﻿using MediaBrowser.Common.Net;
using MediaBrowser.Common.Security;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Plugins.Trailers.Search;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MediaBrowser.Plugins.Trailers.EntryPoints
{
    public class LocalTrailerEntryPoint : IServerEntryPoint
    {
        private readonly List<BaseItem> _newlyAddedItems = new List<BaseItem>();

        private const int NewItemDelay = 60000;

        private readonly ILibraryManager _libraryManager;
        private readonly ISecurityManager _securityManager;
        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IDirectoryWatchers _directoryWatchers;

        private Timer NewItemTimer { get; set; }

        public LocalTrailerEntryPoint(ILibraryManager libraryManager, ISecurityManager securityManager, ILogger logger, IHttpClient httpClient, IDirectoryWatchers directoryWatchers)
        {
            _libraryManager = libraryManager;
            _securityManager = securityManager;
            _logger = logger;
            _httpClient = httpClient;
            _directoryWatchers = directoryWatchers;
        }

        public void Run()
        {
            _libraryManager.ItemAdded += libraryManager_ItemAdded;
            _libraryManager.ItemUpdated += libraryManager_ItemAdded;
        }

        void libraryManager_ItemAdded(object sender, ItemChangeEventArgs e)
        {
            lock (_newlyAddedItems)
            {
                _newlyAddedItems.Add(e.Item);

                if (NewItemTimer == null)
                {
                    NewItemTimer = new Timer(NewItemTimerCallback, null, NewItemDelay, Timeout.Infinite);
                }
                else
                {
                    NewItemTimer.Change(NewItemDelay, Timeout.Infinite);
                }
            }
        }

        private async void NewItemTimerCallback(object state)
        {
            List<BaseItem> newItems;

            // Lock the list and release all resources
            lock (_newlyAddedItems)
            {
                newItems = _newlyAddedItems.DistinctBy(i => i.Id).ToList();
                _newlyAddedItems.Clear();

                NewItemTimer.Dispose();
                NewItemTimer = null;
            }

            var items = newItems.OfType<Movie>()
              .Where(i => i.LocationType == LocationType.FileSystem && i.LocalTrailerIds.Count == 0)
              .Take(1)
              .ToList();

            if (items.Count == 0 || !Plugin.Instance.Configuration.EnableLocalTrailerDownloads)
            {
                return;
            }

            try
            {
                if (!_securityManager.IsMBSupporter)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error getting MB supporter status", ex);
            }

            foreach (var item in items)
            {
                try
                {
                    await new LocalTrailerDownloader(_httpClient, _directoryWatchers, _logger).DownloadTrailerForItem(item, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error downloading trailer for {0}", ex, item.Name);
                }
            }
        }

        public void Dispose()
        {
            if (NewItemTimer != null)
            {
                NewItemTimer.Dispose();
                NewItemTimer = null;
            }
        }
    }
}
