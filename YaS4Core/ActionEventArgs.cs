using System;
using System.Collections.Generic;

namespace YaS4Core
{
    public class ActionEventArgs : EventArgs
    {
        internal ActionEventArgs(StorageAction action, TimeSpan duration)
        {
            Action = action;
            Duration = duration;
        }

        public StorageAction Action { get; private set; }
        public TimeSpan Duration { get; private set; }
    }
}