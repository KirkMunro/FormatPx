using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FormatPx
{
    public class FormatRecord
    {
        public bool PersistWhenOutput { get; set; }

        public Collection<FormatEntry> Entry { get; set; }

        public FormatContainer Format { get; set; }

        public FormatContainer Group { get; set; }

        public bool OutOfBandFormat { get; set; }

        public FormatRecord(FormatContainer format, FormatContainer group, Collection<FormatEntry> entry, bool persistWhenOutput)
        {
            // This constructor is used when a specific format is being used
            Format = format;
            Group = group;
            Entry = entry;
            PersistWhenOutput = persistWhenOutput;
            OutOfBandFormat = false;
        }

        public FormatRecord(Collection<FormatEntry> outOfBandFormatData)
        {
            // This constructor is used when out of band formatting is being used
            Entry = outOfBandFormatData;
            OutOfBandFormat = true;
        }
    }
}