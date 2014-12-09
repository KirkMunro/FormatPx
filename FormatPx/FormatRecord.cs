using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FormatPx
{
    public class FormatRecord
    {
        public bool PersistWhenOutput { get; set; }

        public bool UseDefault { get; set; }

        public Collection<FormatEntry> Entry { get; set; }

        public FormatContainer Format { get; set; }

        public FormatContainer Group { get; set; }

        public FormatRecord(FormatContainer format, FormatContainer group, Collection<FormatEntry> entry, bool persistWhenOutput)
        {
            // This constructor is used when a specific format is being used
            Format = format;
            Group = group;
            Entry = entry;
            PersistWhenOutput = persistWhenOutput;
            UseDefault = false;
        }

        public FormatRecord(bool persistWhenOutput)
        {
            // This constructor is used when the default format is being used
            Format = null;
            Group = null;
            Entry = null;
            PersistWhenOutput = persistWhenOutput;
            UseDefault = true;
        }
    }
}
