﻿// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// Copyright © 2023 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.IO;
using System.Runtime.Caching;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MapControl
{
    public partial class TileImageLoader
    {
        /// <summary>
        /// Default folder path where an ObjectCache instance may save cached data, i.e. C:\ProgramData\MapControl\TileCache
        /// </summary>
        public static string DefaultCacheFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "MapControl", "TileCache");

        /// <summary>
        /// An ObjectCache instance used to cache tile image data. The default value is MemoryCache.Default.
        /// </summary>
        public static ObjectCache Cache { get; set; } = MemoryCache.Default;


        private static async Task LoadCachedTile(Tile tile, Uri uri, string cacheKey)
        {
            var cacheItem = Cache.Get(cacheKey) as Tuple<byte[], DateTime>;
            var buffer = cacheItem?.Item1;

            if (cacheItem == null || cacheItem.Item2 < DateTime.UtcNow)
            {
                var response = await ImageLoader.GetHttpResponseAsync(uri).ConfigureAwait(false);

                if (response != null) // download succeeded
                {
                    buffer = response.Buffer; // may be null or empty when no tile available, but still be cached

                    cacheItem = Tuple.Create(buffer, GetExpiration(response.MaxAge));

                    Cache.Set(cacheKey, cacheItem, new CacheItemPolicy { AbsoluteExpiration = cacheItem.Item2 });
                }
            }
            //else System.Diagnostics.Debug.WriteLine($"Cached: {cacheKey}");

            if (buffer != null && buffer.Length > 0)
            {
                await LoadTile(tile, () => ImageLoader.LoadImageAsync(buffer));
            }
        }

        private static async Task LoadTile(Tile tile, Func<Task<ImageSource>> loadImageFunc)
        {
            var image = await loadImageFunc().ConfigureAwait(false);

            await tile.Image.Dispatcher.InvokeAsync(() => tile.SetImageSource(image));
        }
    }
}
