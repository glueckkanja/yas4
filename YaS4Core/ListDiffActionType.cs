using System.Collections.Generic;
using System;

namespace YaS4Core
{
    /// <summary>
    /// The type of <see cref="ListDiffAction{S,D}"/>.
    /// </summary>
    public enum ListDiffActionType
    {
        /// <summary>
        /// Update the SourceItem to make it like the DestinationItem
        /// </summary>
        Update,
        /// <summary>
        /// Add the DestinationItem
        /// </summary>
        Add,
        /// <summary>
        /// Remove the SourceItem
        /// </summary>
        Remove,
    }
}