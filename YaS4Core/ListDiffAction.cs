using System.Collections.Generic;
using System;

namespace YaS4Core
{
    /// <summary>
    /// A <see cref="ListDiff{S,D}"/> action that can be one of: Update, Add, or Remove.
    /// </summary>
    /// <typeparam name="S">The type of the source list elements</typeparam>
    /// <typeparam name="D">The type of the destination list elements</typeparam>
    public class ListDiffAction<S, D>
    {
        public ListDiffActionType ActionType;
        public S SourceItem;
        public D DestinationItem;

        public ListDiffAction(ListDiffActionType type, S source, D dest)
        {
            ActionType = type;
            SourceItem = source;
            DestinationItem = dest;
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", ActionType, SourceItem, DestinationItem);
        }
    }
}