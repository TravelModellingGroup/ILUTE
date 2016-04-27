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
        internal abstract void MakeNew();
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
            ListLock.EnterWriteLock();
            Thread.MemoryBarrier();
            DataList.Clear();
            Loaded = false;
            Thread.MemoryBarrier();
            ListLock.ExitWriteLock();
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
        private ReaderWriterLockSlim ListLock = new ReaderWriterLockSlim();

        /// <summary>
        /// The data storage, get the ListLock before accessing
        /// </summary>
        private List<T> DataList = new List<T>();

        /// <summary>
        /// Add a new entry
        /// </summary>
        /// <param name="data">The information to add to the repository</param>
        /// <returns>The new index assigned for this element</returns>
        public int AddNew(T data)
        {
            ListLock.EnterWriteLock();
            Thread.MemoryBarrier();
            int index = DataList.Count;
            DataList.Add(data);
            data.Id = index;
            Thread.MemoryBarrier();
            for (int i = 0; i < Dependents.Length; i++)
            {
                Dependents[i].MakeNew();
            }
            ListLock.ExitWriteLock();
            return index;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="index"></param>
        public void SetByID(T data, int index)
        {
            DataList[index] = data;
        }

        /// <summary>
        /// 
        /// </summary>
        sealed override internal void MakeNew()
        {
            ListLock.EnterWriteLock();
            Thread.MemoryBarrier();
            var index = DataList.Count;
            var data = default(T);
            if (data != null)
            {
                data.Id = index;
            }
            DataList.Add(data);
            Thread.MemoryBarrier();
            for (int i = 0; i < Dependents.Length; i++)
            {
                Dependents[i].MakeNew();
            }
            ListLock.ExitWriteLock();
        }

        /// <summary>
        /// Get a data element given its ID
        /// </summary>
        /// <param name="id">The ID to look up</param>
        /// <returns>The data at the given address</returns>
        public T GetByID(int id)
        {
            T ret = null;
            ListLock.EnterReadLock();
            Thread.MemoryBarrier();
#if DEBUG
            try
            {
#endif
            ret = DataList[id];
#if DEBUG
            }
            finally
            {
#endif
            Thread.MemoryBarrier();
            ListLock.ExitReadLock();
#if DEBUG
            }
#endif
            return ret;
        }

        public struct RepositoryEnumerator : IEnumerator<T>
        {
            private readonly List<T> Data;
            private readonly int Length;
            private readonly Repository<T> Repo;
            private int Index;

            public RepositoryEnumerator(Repository<T> repo)
            {
                Repo = repo;
                Data = repo.DataList;
                Length = Data.Count;
                Index = 0;
                repo.ListLock.EnterReadLock();
            }

            public T Current
            {
                get { return Data[Index]; }
            }

            object IEnumerator.Current
            {
                get { return Data[Index]; }
            }

            public bool MoveNext()
            {
                return (++Index < Length);
            }

            public void Reset()
            {
                Index = 0;
            }

            public void Dispose()
            {
                Repo.ListLock.ExitReadLock();
            }
        }

        public struct MultipleAccessContext : IDisposable
        {
            private readonly Repository<T> Repo;
            private readonly List<T> Data;
            public readonly int Length;
            public MultipleAccessContext(Repository<T> repo)
            {
                Repo = repo;
                Data = repo.DataList;
                Repo.ListLock.EnterReadLock();
                Thread.MemoryBarrier();
                Length = Data.Count;
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

            public void Dispose()
            {
                Thread.MemoryBarrier();
                Repo.ListLock.ExitReadLock();
            }
        }

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
