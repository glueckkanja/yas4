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

        public bool Equals(FileProperties other)
        {
            return string.Equals(Key, other.Key) && Size == other.Size && Timestamp.Equals(other.Timestamp);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is FileProperties && Equals((FileProperties) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (Key != null ? Key.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ Size.GetHashCode();
                hashCode = (hashCode*397) ^ Timestamp.GetHashCode();
                return hashCode;
            }
        }
    }
}