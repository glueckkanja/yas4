using System.Collections.Generic;
using System;

namespace YaS4Core
{
    public struct StorageAction
    {
        public StorageAction(FileProperties properties, ListDiffActionType action) : this()
        {
            Properties = properties;
            switch (action)
            {
                case ListDiffActionType.Update:
                    Operation = StorageOperation.Keep;
                    break;
                case ListDiffActionType.Add:
                    Operation = StorageOperation.Add;
                    break;
                case ListDiffActionType.Remove:
                    Operation = StorageOperation.Delete;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("action");
            }
        }

        public StorageAction(FileProperties properties, StorageOperation action) : this()
        {
            Properties = properties;
            Operation = action;
        }

        public FileProperties Properties { get; set; }
        public StorageOperation Operation { get; private set; }

        public static StorageAction Convert(ListDiffAction<FileProperties, FileProperties> action)
        {
            FileProperties properties = action.ActionType == ListDiffActionType.Remove
                                            ? action.SourceItem
                                            : action.DestinationItem;

            return new StorageAction(properties, action.ActionType);
        }

        public override string ToString()
        {
            return string.Format("{0:-2} {1}", Operation.ToString()[0], Properties.Key);
        }
    }
}