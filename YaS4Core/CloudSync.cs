using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace YaS4Core
{
    public class CloudSync
    {
        private readonly CancellationToken _ct;

        public CloudSync(IStorageProvider sourceSite, IStorageProvider destinationSite, CancellationToken ct)
        {
            _ct = ct;
            SourceSite = sourceSite;
            DestinationSite = destinationSite;
            MaxParallelOperations = 4;
        }

        public IStorageProvider SourceSite { get; private set; }
        public IStorageProvider DestinationSite { get; private set; }
        public int MaxParallelOperations { get; set; }

        public event EventHandler<ActionEventArgs> ActionStarted;
        public event EventHandler<ActionEventArgs> ActionFinished;

        public async Task<IList<StorageAction>> ComputeDestinationActions()
        {
            var actions = new List<StorageAction>();

            IList<FileProperties> source = await SourceSite.ListObjects(_ct).ConfigureAwait(false);

            if (_ct.IsCancellationRequested) return actions;

            IList<FileProperties> destination = await DestinationSite.ListObjects(_ct).ConfigureAwait(false);

            if (_ct.IsCancellationRequested) return actions;

            var srcKeys = new HashSet<string>(source.Select(x => x.Key));
            var dstListing = destination.ToDictionary(x => x.Key, x => x);

            foreach (FileProperties src in source)
            {
                FileProperties dst;

                if (!dstListing.TryGetValue(src.Key, out dst))
                {
                    actions.Add(new StorageAction(src, StorageOperation.Add));
                    continue;
                }

                if (src.Key == dst.Key && src.Size == dst.Size && src.Timestamp <= dst.Timestamp)
                {
                    actions.Add(new StorageAction(dst, StorageOperation.Keep));
                }
                else
                {
                    actions.Add(new StorageAction(src, StorageOperation.Overwrite));
                }
            }

            foreach (FileProperties dst in destination)
            {
                if (!srcKeys.Contains(dst.Key))
                {
                    actions.Add(new StorageAction(dst, StorageOperation.Delete));
                }
            }

            return actions
                .OrderByDescending(x => x.Operation == StorageOperation.Delete)
                .ThenBy(x => x.Properties.Size)
                .ToList();
        }

        public async Task ExecuteSync(IEnumerable<StorageAction> actions)
        {
            await ForEachPooled(actions, MaxParallelOperations, _ct, (action, ct) =>
                {
                    if (action.Operation == StorageOperation.Add)
                        return AddImpl(action, false);
                    if (action.Operation == StorageOperation.Overwrite)
                        return AddImpl(action, true);
                    if (action.Operation == StorageOperation.Delete)
                        return DeleteImpl(action);

                    return Task.FromResult(0);
                }).ConfigureAwait(false);
        }

        private async Task AddImpl(StorageAction action, bool overwrite)
        {
            string path = null;

            try
            {
                Stream src;

                // > 1mb?
                if (action.Properties.Size > Math.Pow(2, 20))
                {
                    path = Path.GetTempFileName();
                    src = File.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

                    // allocate
                    src.SetLength(action.Properties.Size);
                    src.Position = 0;
                }
                else
                {
                    src = new MemoryStream();
                }

                using (src)
                {
                    OnActionStarted(action);

                    Stopwatch sw = Stopwatch.StartNew();

                    await SourceSite.ReadObject(action.Properties, src, _ct).ConfigureAwait(false);
                    src.Position = 0;
                    await DestinationSite.AddObject(action.Properties, src, overwrite, _ct).ConfigureAwait(false);

                    sw.Stop();

                    OnActionFinished(action, sw);
                }
            }
            finally
            {
                if (path != null)
                {
                    try
                    {
                        File.Delete(path);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private async Task DeleteImpl(StorageAction action)
        {
            await DestinationSite.DeleteObject(action.Properties, _ct).ConfigureAwait(false);
        }

        private void OnActionStarted(StorageAction action)
        {
            EventHandler<ActionEventArgs> temp = ActionStarted;

            if (temp != null)
            {
                temp(this, new ActionEventArgs(action, TimeSpan.Zero));
            }
        }

        private void OnActionFinished(StorageAction action, Stopwatch sw)
        {
            EventHandler<ActionEventArgs> temp = ActionFinished;

            if (temp != null)
            {
                temp(this, new ActionEventArgs(action, sw.Elapsed));
            }
        }

        internal static async Task ForEachPooled<T>(
            IEnumerable<T> values, int poolSize, CancellationToken ct, Func<T, CancellationToken, Task> action)
        {
            var buffer = new Task[poolSize];

            for (int i = 0; i < poolSize; i++)
            {
                buffer[i] = Task.FromResult(0);
            }

            foreach (T value in values)
            {
                while (true)
                {
                    bool started = false;

                    for (int i = 0; i < buffer.Length; i++)
                    {
                        Task task = buffer[i];

                        if (task.IsCompleted || task.IsCanceled || task.IsFaulted)
                        {
                            using (task) await task.ConfigureAwait(false);

                            task = action(value, ct);
                            buffer[i] = task;
                            started = true;
                            break;
                        }
                    }

                    if (started)
                        break;

                    await Task.WhenAny(buffer).ConfigureAwait(false);
                }
            }

            await Task.WhenAll(buffer).ConfigureAwait(false);
        }

        internal sealed class AsyncLock
        {
            // http://blogs.msdn.com/b/pfxteam/archive/2012/02/12/10266988.aspx

            private readonly Task<IDisposable> m_releaser;
            private readonly SemaphoreSlim m_semaphore = new SemaphoreSlim(1, 1);

            public AsyncLock()
            {
                m_releaser = Task.FromResult((IDisposable) new Releaser(this));
            }

            public Task<IDisposable> LockAsync()
            {
                Task wait = m_semaphore.WaitAsync();
                return wait.IsCompleted
                           ? m_releaser
                           : wait.ContinueWith((_, state) => (IDisposable) state,
                                               m_releaser.Result, CancellationToken.None,
                                               TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }

            private sealed class Releaser : IDisposable
            {
                private readonly AsyncLock m_toRelease;

                internal Releaser(AsyncLock toRelease)
                {
                    m_toRelease = toRelease;
                }

                public void Dispose()
                {
                    m_toRelease.m_semaphore.Release();
                }
            }
        }
    }
}