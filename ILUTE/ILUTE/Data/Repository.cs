/*
    Copyright 2016-2018 Travel Modelling Group, Department of Civil Engineering, University of Toronto

    This file is part of ILUTE, a set of modules for XTMF.

    XTMF is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    XTMF is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with XTMF.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMG.Ilute.Data.Demographics;
using XTMF;

namespace TMG.Ilute.Data
{
    /// <summary>
    /// A higher level abstraction for Repository in order to
    /// allow us to have dependent repositories
    /// </summary>
    public abstract class Repository
    {
        /// <summary>
        /// Call this to let dependences know that
        /// </summary>
        internal abstract void MakeNew(long index);

        internal abstract void CascadeRemove(long index);

        /// <summary>
        /// Get a copy of the repository from the data source loading it if necessary
        /// </summary>
        /// <typeparam name="T">The type of data stored</typeparam>
        /// <param name="source">The datasource to be loading</param>
        /// <returns>The now loaded data source's data</returns>
        public static T GetRepository<T>(IDataSource<T> source)
        {
            if (!source.Loaded)
            {
                source.LoadData();
            }
            return source.GiveData();
        }
    }

    /// <summary>
    /// This class is designed to facilitate the creation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class Repository<T> : Repository, IDataSource<Repository<T>>, IEnumerable<T>, IDisposable
        where T : IndexedObject
    {

        public bool Loaded { get; private set; }

        public string Name { get; set; }

        public float Progress => 0f;

        public Tuple<byte, byte, byte> ProgressColour => new Tuple<byte, byte, byte>(50, 150, 50);

        public void LoadData()
        {
            _dependents = DependentResources?.Select(repository => repository.AcquireResource<Repository>()).ToArray() ?? new Repository[0];
            Loaded = true;
        }

        public Repository<T> GiveData()
        {
            return this;
        }

        public bool RuntimeValidation(ref string error)
        {
            return true;
        }

        public void UnloadData()
        {
            _dataLock.EnterWriteLock();
            Thread.MemoryBarrier();
            _data.Clear();
            Loaded = false;
            Thread.MemoryBarrier();
            _dataLock.ExitWriteLock();
        }

        [SubModelInformation(Description = "Repositories that need to increase when data is added to this repository.")]
        public IResource[] DependentResources;

        /// <summary>
        /// A list of repositories that need to update when this repository is added to
        /// </summary>
        private Repository[] _dependents;

        /// <summary>
        /// This is used before accessing the DataList
        /// </summary>
        private ReaderWriterLockSlim _dataLock = new ReaderWriterLockSlim();

        /// <summary>
        /// The data storage, get the ListLock before accessing
        /// </summary>
        private Dictionary<long, T> _data = new Dictionary<long, T>();

        /// <summary>
        /// Add a new entry
        /// </summary>
        /// <param name="data">The information to add to the repository. Must be non-null!</param>
        /// <returns>The new index assigned for this element</returns>
        public long AddNew(long index, T data)
        {
            if (data == null)
            {
                throw new XTMFRuntimeException(this, "Error trying to add a new element with a null for data.");
            }
            if(_dependents == null)
            {
                throw new XTMFRuntimeException(this, "The repository was not loaded before trying to add data!");
            }
            _dataLock.EnterWriteLock();
            Thread.MemoryBarrier();
            data.Id = index;
            _data.Add(index, data);
            // If the index is equal to or higher than the highest index so far, increase that index
            _highest = Math.Max(index + 1, _highest);
            Thread.MemoryBarrier();
            for (int i = 0; i < _dependents?.Length; i++)
            {
                _dependents[i].MakeNew(index);
            }
            _dataLock.ExitWriteLock();
            return index;
        }

        public long AddNew(T data)
        {
            _dataLock.EnterWriteLock();
            Thread.MemoryBarrier();
            long index = Interlocked.Increment(ref _highest) - 1;
            data.Id = index;
            _data.Add(index, data);
            Thread.MemoryBarrier();
            for (int i = 0; i < _dependents.Length; i++)
            {
                _dependents[i].MakeNew(index);
            }
            _dataLock.ExitWriteLock();
            return index;
        }

        // Accessing this must be done inside of a memory fence!
        private long _highest = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="index"></param>
        public void SetByID(T data, long index)
        {
            _dataLock.EnterWriteLock();
            Thread.MemoryBarrier();
            _data[index] = data;
            _dataLock.ExitWriteLock();
            Thread.MemoryBarrier();
        }

        /// <summary>
        /// Generate a new entry for the given position
        /// </summary>
        sealed override internal void MakeNew(long index)
        {
            _dataLock.EnterWriteLock();
            Thread.MemoryBarrier();
            var data = default(T);
            if (data != null)
            {
                data.Id = index;
            }
            _data.Add(index, data);
            // If the index is equal to or higher than the highest index so far, increase that index
            _highest = Math.Max(index + 1, _highest);
            Thread.MemoryBarrier();
            for (int i = 0; i < _dependents.Length; i++)
            {
                _dependents[i].MakeNew(index);
            }
            _dataLock.ExitWriteLock();
        }

        /// <summary>
        /// Remove the given index from the repository
        /// </summary>
        /// <param name="index">The index to remove</param>
        public void Remove(long index) => CascadeRemove(index);

        /// <summary>
        /// Delete the given index and all dependent repositories' index as well
        /// </summary>
        /// <param name="index">The index to delete</param>
        internal sealed override void CascadeRemove(long index)
        {
            // after the object is ready to be removed, do so
            _dataLock.EnterWriteLock();
            Thread.MemoryBarrier();
            if (!_data.TryGetValue(index, out T element))
            {
                throw new XTMFRuntimeException(this, $"In {Name} we were unable to find data at index {index} in order to retrieve it!");
            }
            if (element != null)
            {
                element.BeingRemoved();
                _data.Remove(index);
            }
            Thread.MemoryBarrier();
            for (int i = 0; i < _dependents.Length; i++)
            {
                _dependents[i].CascadeRemove(index);
            }
            _dataLock.ExitWriteLock();
        }

        /// <summary>
        /// Get a data element given its ID
        /// </summary>
        /// <param name="id">The ID to look up</param>
        /// <returns>The data at the given address</returns>
        public T GetByID(long id)
        {
            T ret = null;
            _dataLock.EnterReadLock();
            Thread.MemoryBarrier();
#if DEBUG
            try
            {
#endif
            ret = _data[id];
#if DEBUG
            }
            finally
            {
#endif
            Thread.MemoryBarrier();
            _dataLock.ExitReadLock();
#if DEBUG
            }
#endif
            return ret;
        }

        /// <summary>
        /// Try to get the data for the given index
        /// </summary>
        /// <param name="id">The index of the data</param>
        /// <param name="data">The data to access</param>
        /// <returns>True if the data was recalled, false otherwise.</returns>
        public bool TryGet(long id, out T data)
        {
            bool ret;
            _dataLock.EnterReadLock();
            Thread.MemoryBarrier();
#if DEBUG
            try
            {
#endif
            ret = _data.TryGetValue(id, out data);
#if DEBUG
            }
            finally
            {
#endif
            Thread.MemoryBarrier();
            _dataLock.ExitReadLock();
#if DEBUG
            }
#endif
            return ret;
        }

        /// <summary>
        /// Get or set by index
        /// </summary>
        /// <param name="index">The index to work with</param>
        /// <returns>The value at the given index</returns>
        public T this[long index]
        {
            get => GetByID(index);
            set => SetByID(value, index);
        }

        public int Count => _data.Count;

        public struct RepositoryEnumerator : IEnumerator<T>
        {
            private Dictionary<long, T>.Enumerator LocalEnumerator;
            private readonly Repository<T> Repo;
            private volatile bool IsDisposed;


            public RepositoryEnumerator(Repository<T> repo)
            {
                IsDisposed = false;
                Repo = repo;
                LocalEnumerator = Repo._data.GetEnumerator();
            }

            public T Current => LocalEnumerator.Current.Value;

            object IEnumerator.Current => LocalEnumerator.Current.Value;

            public bool MoveNext() => LocalEnumerator.MoveNext();

            public void Dispose()
            {
                lock (Repo._data)
                {
                    if (IsDisposed)
                    {
                        return;
                    }
                    LocalEnumerator.Dispose();
                    IsDisposed = true;
                }
            }

            public void Reset() => ((IEnumerator)LocalEnumerator).Reset();
        }

        public struct MultipleAccessContext : IDisposable
        {
            private readonly Repository<T> _repo;
            private readonly Dictionary<long, T> _data;
            private volatile bool _isDisposed;
            private readonly int _count;

            public MultipleAccessContext(Repository<T> repo)
            {
                _isDisposed = false;
                _repo = repo;
                _data = repo._data;
                _count = _data.Count;
                _repo._dataLock.EnterReadLock();
                Thread.MemoryBarrier();
            }

            public bool TryGet(long index, out T data)
            {
                return _data.TryGetValue(index, out data);
            }

            public T this[long i]
            {
                get => _data[i];
                set => _data[i] = value;
            }

            public static implicit operator List<long>(MultipleAccessContext context)
            {
                return context.GetKeys();
            }

            public List<long> GetKeys()
            {
                return _data.Keys.ToList();
            }

            public void Dispose()
            {
                lock (_data)
                {
                    if (_isDisposed)
                    {
                        return;
                    }
                    _isDisposed = true;
                }
                Thread.MemoryBarrier();
                _repo._dataLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Get a context which allows you to make multiple reads
        /// from the repository without allowing a write to occur
        /// and to reduce the number of lock checks.  Use this
        /// with a using statement to ensure dispose gets invoked.
        /// </summary>
        /// <returns>The context</returns>
        public MultipleAccessContext GetMultiAccessContext()
        {
            return new MultipleAccessContext(this);
        }

        public RepositoryEnumerator GetEnumerator()
        {
            return new RepositoryEnumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void Dispose(bool managed)
        {
            if (managed)
            {
                GC.SuppressFinalize(this);
            }
            _dataLock.Dispose();
            _dataLock = null;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        ~Repository()
        {
            Dispose(false);
        }
    }
}
