﻿using AutoMapper;
using Maple.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Maple
{
    /// <summary>
    /// Extensions for <see cref="MediaItem"/>
    /// </summary>
    public static class MapperExtensions
    {

        public static IEnumerable<MediaItem> GetMany(this IMediaItemMapper mapper, IEnumerable<Data.MediaItem> items)
        {
            return items.ForEach(mapper.Get);
        }

        public static IList<MediaItem> GetManyAsList(this IMediaItemMapper mapper, IEnumerable<Data.MediaItem> items)
        {
            return items.ForEach(mapper.Get).ToList();
        }

        public static IEnumerable<Playlist> GetMany(this IPlaylistMapper mapper, IEnumerable<Data.Playlist> items)
        {
            return items.ForEach(mapper.Get);
        }

        public static IEnumerable<Data.Playlist> GetManyData(this IPlaylistMapper mapper, IEnumerable<Playlist> items)
        {
            return items.ForEach(mapper.GetData);
        }

        public static IMappingExpression<TSource, TDestination> Ignore<TSource, TDestination>(this IMappingExpression<TSource,
            TDestination> map,
            Expression<Func<TDestination, object>> selector)
        {
            map.ForMember(selector, config => config.Ignore());
            return map;
        }
    }
}
