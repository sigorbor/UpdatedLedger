using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace LedgerDatabase
{
    public class Table<TLine>
    {
        ConcurrentDictionary<UInt64, TLine> lines;
        UInt64 primaryKey;

        public Table()
        {
            lines = new ConcurrentDictionary<UInt64, TLine>();
            primaryKey = 1;
        }

        public UInt64 Insert(TLine line)
        {
            UInt64 id = primaryKey++;
            lines[id] = line;
            return id;
        }

        public TLine Select(UInt64 id)
        {
            return lines[id];
        }

        public bool Contains(UInt64 id)
        {
            return lines.ContainsKey(id);
        }

        public void Delete(UInt64 id)
        {
            TLine l;
            lines.TryRemove(id, out l);
        }

        public ICollection<TLine> SelectAll()
        {
            return lines.Values;
        }
    }
}

