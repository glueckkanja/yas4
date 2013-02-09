using System;
using System.Collections.Generic;

namespace YaS4Core
{
    public struct StorageAction
    {
        public StorageAction(FileProperties properties, StorageOperation action) : this()
        {
            Properties = properties;
            Operation = action;
        }

        public FileProperties Properties { get; set; }
        public StorageOperation Operation { get; private set; }

        public override string ToString()
        {
            return string.Format("{0:-2} {1}", Operation.ToString()[0], Properties);
        }
    }
}