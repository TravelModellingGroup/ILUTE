/*
    Copyright 2016 Travel Modelling Group, Department of Civil Engineering, University of Toronto

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
        internal abstract void MakeNew(int index);

        internal abstract void CascadeRemove(int index);

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
    public sealed class Repository<T> : Repository, IDataSource<Repository<T>>
        where T : IndexedObject
    {

        public bool Loaded { get; private set; }

        public string Name { get; set; }

        public float Progress
        {
            get
            {
                return 0f;
            }
        }

        public Tuple<byte, byte, byte> ProgressColour
        {
            get
            {
                return new Tuple<byte, byte, byte>(50, 150, 50);
            }
        }

        public void LoadData()
        {
            Dependents = DependentResources.Select(repository => repository.AcquireResource<Repository>()).ToArray();
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
            DataLock.EnterWriteLock();
            Thread.MemoryBarrier();
            Data.Clear();
            Loaded = false;
            Thread.MemoryBarrier();
            DataLock.ExitWriteLock();
        }

        [SubModelInformation(Description = "Repositories that need to increase when data is added to this repository.")]
        public IResource[] DependentResources;

        /// <summary>
        /// A list of repositories that need to update when this repository is added to
        /// </summary>
        private Repository[] Dependents;

        /// <summary>
        /// This is used before accessing the DataList
        /// </summary>
        private ReaderWriterLockSlim DataLock = new ReaderWriterLockSlim();

        /// <summary>
        /// The data storage, get the ListLock before accessing
        /// </summary>
        private Dictionary<int, T> Data = new Dictionary<int, T>();

        /// <summary>
        /// Add a new entry
        /// </summary>
        /// <param name="data">The information to add to the repository</param>
        /// <returns>The new index assigned for this element</returns>
        public int AddNew(int index, T data)
        {
            DataLock.EnterWriteLock();
            Thread.MemoryBarrier();
            data.Id = index;
            Data.Add(index, data);
            // If the index is equal to or higher than the highest index so far, increase that index
            Highest = Math.Max(index + 1, Highest);
            Thread.MemoryBarrier();
            for (int i = 0; i < Dependents.Length; i++)
            {
                Dependents[i].MakeNew(index);
            }
            DataLock.ExitWriteLock();
            return index;
        }

        public int AddNew(T data)
        {
            DataLock.EnterWriteLock();
            Thread.MemoryBarrier();
            int index = Interlocked.Increment(ref Highest) - 1;
            data.Id = index;
            Data.Add(index, data);
            Thread.MemoryBarrier();
            for (int i = 0; i < Dependents.Length; i++)
            {
                Dependents[i].MakeNew(index);
            }
            DataLock.ExitWriteLock();
            return index;
        }

        private volatile int Highest = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="index"></param>
        public void SetByID(T data, int index)
        {
            DataLock.EnterWriteLock();
            Thread.MemoryBarrier();
            Data[index] = data;
            DataLock.ExitWriteLock();
            Thread.MemoryBarrier();
        }

        /// <summary>
        /// Generate a new entry for the given position
        /// </summary>
        sealed override internal void MakeNew(int index)
        {
            DataLock.EnterWriteLock();
            Thread.MemoryBarrier();
            var data = default(T);
            if (data != null)
            {
                data.Id = index;
            }
            Data.Add(index, data);
            // If the index is equal to or higher than the highest index so far, increase that index
            Highest = Math.Max(index + 1, Highest);
            Thread.MemoryBarrier();
            for (int i = 0; i < Dependents.Length; i++)
            {
                Dependents[i].MakeNew(index);
            }
            DataLock.ExitWriteLock();
        }

        /// <summary>
        /// Remove the given index from the repository
        /// </summary>
        /// <param name="index">The index to remove</param>
        public void Remove(int index)
        {
            CascadeRemove(index);
        }

        /// <summary>
        /// Delete the given index and all dependent repositories' index as well
        /// </summary>
        /// <param name="index">The index to delete</param>
        internal sealed override void CascadeRemove(int index)
        {
            // after the object is ready to be removed, do so
            DataLock.EnterWriteLock();
            Thread.MemoryBarrier();
            T element;
            if (!Data.TryGetValue(index, out element))
            {
                throw new XTMFRuntimeException($"In {Name} we were unable to find data at index {index} in order to retrieve it!");
            }
            if (element != null)
            {
                element.BeingRemoved();
                Data.Remove(index);
            }
            Thread.MemoryBarrier();
            for (int i = 0; i < Dependents.Length; i++)
            {
                Dependents[i].CascadeRemove(index);
            }
            DataLock.ExitWriteLock();
        }

        /// <summary>
        /// Get a data element given its ID
        /// </summary>
        /// <param name="id">The ID to look up</param>
        /// <returns>The data at the given address</returns>
        public T GetByID(int id)
        {
            T ret = null;
            DataLock.EnterReadLock();
            Thread.MemoryBarrier();
#if DEBUG
            try
            {
#endif
            ret = Data[id];
#if DEBUG
            }
            finally
            {
#endif
            Thread.MemoryBarrier();
            DataLock.ExitReadLock();
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
        public bool TryGet(int id, out T data)
        {
            bool ret;
            DataLock.EnterReadLock();
            Thread.MemoryBarrier();
#if DEBUG
            try
            {
#endif
            ret = Data.TryGetValue(id, out data);
#if DEBUG
            }
            finally
            {
#endif
            Thread.MemoryBarrier();
            DataLock.ExitReadLock();
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
        public T this[int index]
        {
            get
            {
                return GetByID(index);
            }
            set
            {
                SetByID(value, index);
            }
        }

        public int Count
        {
            get
            {
                return Data.Count;
            }
        }

        public struct RepositoryEnumerator : IEnumerator<T>
        {
            private Dictionary<int, T>.Enumerator LocalEnumerator;
            private readonly Repository<T> Repo;
            private volatile bool IsDisposed;

            public RepositoryEnumerator(Repository<T> repo)
            {
                IsDisposed = false;
                Repo = repo;
                repo.DataLock.EnterReadLock();
                LocalEnumerator = Repo.Data.GetEnumerator();
            }

            public T Current
            {
                get
                {
                    return LocalEnumerator.Current.Value;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return LocalEnumerator.Current.Value;
                }
            }

            public bool MoveNext()
            {
                return LocalEnumerator.MoveNext();
            }

            public void Dispose()
            {
                lock (Repo.Data)
                {
                    if (IsDisposed)
                    {
                        throw new InvalidOperationException("Can not dispose an enumeration more than once!");
                    }
                    LocalEnumerator.Dispose();
                    IsDisposed = true;
                }
                Thread.MemoryBarrier();
                Repo.DataLock.ExitReadLock();
            }

            public void Reset()
            {
                ((IEnumerator)LocalEnumerator).Reset();
            }
        }

        public struct MultipleAccessContext : IDisposable
        {
            private readonly Repository<T> Repo;
            private readonly Dictionary<int, T> Data;
            private volatile bool IsDisposed;
            private readonly int Count;

            public MultipleAccessContext(Repository<T> repo)
            {
                IsDisposed = false;
                Repo = repo;
                Data = repo.Data;
                Count = Data.Count;
                Repo.DataLock.EnterReadLock();
                Thread.MemoryBarrier();
            }

            public bool TryGet(int index, out T data)
            {
                return Data.TryGetValue(index, out data);
            }

            public T this[int i]
            {
                get
                {
                    return Data[i];
                }
                set
                {
                    Data[i] = value;
                }
            }

            public static implicit operator List<int>(MultipleAccessContext context)
            {
                return context.GetKeys();
            }

            public List<int> GetKeys()
            {
                return Data.Keys.ToList();
            }

            public void Dispose()
            {
                lock (Data)
                {
                    if (IsDisposed)
                    {
                        throw new InvalidOperationException("Can not dispose a disposed context!");
                    }
                    IsDisposed = true;
                }
                Thread.MemoryBarrier();
                Repo.DataLock.ExitReadLock();
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
    }
}
