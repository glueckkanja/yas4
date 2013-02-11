using System.Collections.Generic;
using System;

namespace YaS4Core
{
    public struct FileProperties
    {
        public string Key { get; set; }
        public string LocalKey { get; set; }
        public long Size { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        public override string ToString()
        {
            return string.Format("{0} (ts: {1}; size: {2} bytes)", Key, Timestamp, Size);
        }
    }
}