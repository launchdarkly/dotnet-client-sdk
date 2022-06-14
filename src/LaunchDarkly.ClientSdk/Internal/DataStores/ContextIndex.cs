using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LaunchDarkly.JsonStream;

namespace LaunchDarkly.Sdk.Client.Internal.DataStores
{
    /// <summary>
    /// Used internally to track which contexts have flag data in the persistent store.
    /// </summary>
    internal class ContextIndex
    {
        internal ImmutableList<IndexEntry> Data { get; }

        internal struct IndexEntry
        {
            public string ContextId { get; set; }
            public UnixMillisecondTime Timestamp { get; set; }
        }

        internal ContextIndex(ImmutableList<IndexEntry> data = null)
        {
            Data = data ?? ImmutableList<IndexEntry>.Empty;
        }

        public ContextIndex UpdateTimestamp(string contextId, UnixMillisecondTime timestamp)
        {
            var builder = ImmutableList.CreateBuilder<IndexEntry>();
            builder.AddRange(Data.Where(e => e.ContextId != contextId));
            builder.Add(new IndexEntry { ContextId = contextId, Timestamp = timestamp });
            return new ContextIndex(builder.ToImmutable());
        }

        public ContextIndex Prune(int maxContextsToRetain, out IEnumerable<string> removedUserIds)
        {
            if (Data.Count <= maxContextsToRetain)
            {
                removedUserIds = ImmutableList<string>.Empty;
                return this;
            }
            // The data will normally already be in ascending timestamp order, in which case this Sort
            // won't do anything, but this is just in case unsorted data somehow got persisted.
            var sorted = Data.Sort((e1, e2) => e1.Timestamp.CompareTo(e2.Timestamp));
            var numDrop = Data.Count - maxContextsToRetain;
            removedUserIds = ImmutableList.CreateRange(sorted.Take(numDrop).Select(e => e.ContextId));
            return new ContextIndex(ImmutableList.CreateRange(sorted.Skip(numDrop)));
        }

        /// <summary>
        /// Returns a JSON representation of the context index.
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
                        aw1.String(e.ContextId);
                        aw1.Long(e.Timestamp.Value);
                    }
                }
            }
            return w.GetString();
        }

        /// <summary>
        /// Parses the context index from a JSON representation. If the JSON string is null or
        /// empty, it returns an empty index.
        /// </summary>
        /// <param name="json">the JSON representation</param>
        /// <returns>the parsed data</returns>
        /// <exception cref="FormatException">if the JSON is malformed</exception>
        public static ContextIndex Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new ContextIndex();
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
                        var contextId = r.String();
                        if (ar1.Next(ref r))
                        {
                            var timeMillis = r.Long();
                            builder.Add(new IndexEntry { ContextId = contextId, Timestamp = UnixMillisecondTime.OfMillis(timeMillis) });
                            ar1.Next(ref r);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new FormatException("invalid stored context index", e);
            }
            return new ContextIndex(builder.ToImmutable());
        }
    }
}
