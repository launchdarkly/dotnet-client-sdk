using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LaunchDarkly.JsonStream;

namespace LaunchDarkly.Sdk.Client.Internal.DataStores
{
    /// <summary>
    /// Used internally to track which users have flag data in the persistent store.
    /// </summary>
    internal class UserIndex
    {
        internal ImmutableList<IndexEntry> Data { get; }

        internal struct IndexEntry
        {
            public string UserId { get; set; }
            public UnixMillisecondTime Timestamp { get; set; }
        }

        internal UserIndex(ImmutableList<IndexEntry> data = null)
        {
            Data = data ?? ImmutableList<IndexEntry>.Empty;
        }

        public UserIndex UpdateTimestamp(string userId, UnixMillisecondTime timestamp)
        {
            var builder = ImmutableList.CreateBuilder<IndexEntry>();
            builder.AddRange(Data.Where(e => e.UserId != userId));
            builder.Add(new IndexEntry { UserId = userId, Timestamp = timestamp });
            return new UserIndex(builder.ToImmutable());
        }

        public UserIndex Prune(int maxUsersToRetain, out IEnumerable<string> removedUserIds)
        {
            if (Data.Count <= maxUsersToRetain)
            {
                removedUserIds = ImmutableList<string>.Empty;
                return this;
            }
            // The data will normally already be in ascending timestamp order, in which case this Sort
            // won't do anything, but this is just in case unsorted data somehow got persisted.
            var sorted = Data.Sort((e1, e2) => e1.Timestamp.CompareTo(e2.Timestamp));
            var numDrop = Data.Count - maxUsersToRetain;
            removedUserIds = ImmutableList.CreateRange(sorted.Take(numDrop).Select(e => e.UserId));
            return new UserIndex(ImmutableList.CreateRange(sorted.Skip(numDrop)));
        }

        /// <summary>
        /// Returns a JSON representation of the user index.
        /// </summary>
        /// <returns>the JSON representation</returns>
        public string Serialize()
        {
            var w = JWriter.New();
            using (var aw0 = w.Array())
            {
                foreach (var e in Data)
                {
                    using (var aw1 = aw0.Array())
                    {
                        aw1.String(e.UserId);
                        aw1.Long(e.Timestamp.Value);
                    }
                }
            }
            return w.GetString();
        }

        /// <summary>
        /// Parses the user index from a JSON representation. If the JSON string is null or
        /// empty, it returns an empty user index.
        /// </summary>
        /// <param name="json">the JSON representation</param>
        /// <returns>the parsed data</returns>
        /// <exception cref="FormatException">if the JSON is malformed</exception>
        public static UserIndex Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new UserIndex();
            }
            var builder = ImmutableList.CreateBuilder<IndexEntry>();
            try
            {
                var r = JReader.FromString(json);
                for (var ar0 = r.Array(); ar0.Next(ref r);)
                {
                    var ar1 = r.Array();
                    if (ar1.Next(ref r))
                    {
                        var userId = r.String();
                        if (ar1.Next(ref r))
                        {
                            var timeMillis = r.Long();
                            builder.Add(new IndexEntry { UserId = userId, Timestamp = UnixMillisecondTime.OfMillis(timeMillis) });
                            ar1.Next(ref r);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new FormatException("invalid stored user index", e);
            }
            return new UserIndex(builder.ToImmutable());
        }
    }
}
