﻿using System.Collections.Generic;
using System;

namespace YaS4Core
{
    // src: https://gist.github.com/praeclarum/3651254 THX!
    // "It's better than anything you have ever seen. Ever." <- SO TRUE

    /// <summary>
    /// Finds a diff between two lists (that contain possibly different types).
    /// <see cref="Actions"/> are generated such that the order of items in the
    /// destination list is preserved.
    /// The algorithm is from: http://en.wikipedia.org/wiki/Longest_common_subsequence_problem
    /// </summary>
    /// <typeparam name="S">The type of the source list elements</typeparam>
    /// <typeparam name="D">The type of the destination list elements</typeparam>
    public class ListDiff<S, D>
    {
        /// <summary>
        /// The actions needed to transform a source list to a destination list.
        /// </summary>
        public List<ListDiffAction<S, D>> Actions { get; private set; }

        /// <summary>
        /// Whether the <see cref="Actions"/> only contain Update actions
        /// (no Adds or Removes).
        /// </summary>
        public bool ContainsOnlyUpdates { get; private set; }

        public ListDiff(IEnumerable<S> sources,
                        IEnumerable<D> destinations,
                        Func<S, D, bool> match)
        {
            if (sources == null) throw new ArgumentNullException("sources");
            if (destinations == null) throw new ArgumentNullException("destinations");
            if (match == null) throw new ArgumentNullException("match");

            var x = new List<S>(sources);
            var y = new List<D>(destinations);

            Actions = new List<ListDiffAction<S, D>>();

            var m = x.Count;
            var n = y.Count;

            //
            // Construct the C matrix
            //
            var c = new int[m + 1, n + 1];
            for (var i = 1; i <= m; i++)
            {
                for (var j = 1; j <= n; j++)
                {
                    if (match(x[i - 1], y[j - 1]))
                    {
                        c[i, j] = c[i - 1, j - 1] + 1;
                    }
                    else
                    {
                        c[i, j] = Math.Max(c[i, j - 1], c[i - 1, j]);
                    }
                }
            }

            //
            // Generate the actions
            //
            ContainsOnlyUpdates = true;
            GenDiff(c, x, y, m, n, match);
        }

        void GenDiff(int[,] c, List<S> x, List<D> y, int i, int j, Func<S, D, bool> match)
        {
            if (i > 0 && j > 0 && match(x[i - 1], y[j - 1]))
            {
                GenDiff(c, x, y, i - 1, j - 1, match);
                Actions.Add(new ListDiffAction<S, D>(ListDiffActionType.Update, x[i - 1], y[j - 1]));
            }
            else
            {
                if (j > 0 && (i == 0 || c[i, j - 1] >= c[i - 1, j]))
                {
                    GenDiff(c, x, y, i, j - 1, match);
                    ContainsOnlyUpdates = false;
                    Actions.Add(new ListDiffAction<S, D>(ListDiffActionType.Add, default(S), y[j - 1]));
                }
                else if (i > 0 && (j == 0 || c[i, j - 1] < c[i - 1, j]))
                {
                    GenDiff(c, x, y, i - 1, j, match);
                    ContainsOnlyUpdates = false;
                    Actions.Add(new ListDiffAction<S, D>(ListDiffActionType.Remove, x[i - 1], default(D)));
                }
            }
        }
    }
}