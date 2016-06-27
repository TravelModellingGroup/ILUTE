using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMG.Ilute.Data;
using TMG.Ilute.Data.Housing;
using TMG.Ilute.Data.Spatial;
using TMG.Ilute.Model.Utilities;
using TMG.Input;
using XTMF;

namespace TMG.Ilute.Model.Housing.Validation
{

    public sealed class CountDwellingsByZone : IExecuteYearly, IDisposable
    {
        [SubModelInformation(Required = true, Description = "The zone system that the dwellings are referencing.")]
        public IDataSource<ZoneSystem> ZoneSystem;

        [SubModelInformation(Required = true, Description = "The dwellings currently in the simulation.")]
        public IDataSource<Repository<Dwelling>> Dwellings;

        [SubModelInformation(Required = true, Description = "The location to save the report to.")]
        public FileLocation SaveTo;

        private StreamWriter Writer;

        public string Name { get; set; }

        public float Progress { get; set; }

        public Tuple<byte, byte, byte> ProgressColour { get { return new Tuple<byte, byte, byte>(50, 150, 50); } }

        private void Dispose(bool managed)
        {
            if (managed)
            {
                GC.SuppressFinalize(this);
            }
            if (Writer != null)
            {
                Writer.Dispose();
                Writer = null;
            }
        }

        ~CountDwellingsByZone()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void AfterYearlyExecute(int currentYear)
        {
        }

        public void BeforeFirstYear(int firstYear)
        {
        }

        public void BeforeYearlyExecute(int currentYear)
        {
        }

        public void Execute(int currentYear)
        {
            var zones = Repository.GetRepository(ZoneSystem).ZoneNumber;
            EnsureWriter(zones);
            // aggregate 
            int[] acc = AggregateData(zones);
            // now that all of the dwellings have been counted write to disk
            WriteData(currentYear, acc);
        }

        private void WriteData(int currentYear, int[] acc)
        {
            Writer.Write(currentYear);
            Writer.Write(',');
            Writer.WriteLine(string.Join(",", acc));
        }

        private void EnsureWriter(int[] zones)
        {
            if (Writer == null)
            {
                Writer = new StreamWriter(SaveTo);
                Writer.WriteLine(GetHeader(zones));
            }
        }

        private int[] AggregateData(int[] zones)
        {
            int[] acc = new int[zones.Length];
            foreach (var dwelling in Repository.GetRepository(Dwellings))
            {
                var z = dwelling.Zone;
                if (z >= 0 && z < acc.Length)
                {
                    acc[z]++;
                }
            }
            return acc;
        }

        private string GetHeader(int[] zones)
        {
            return "Year," + string.Join(",", zones);
        }

        public void RunFinished(int finalYear)
        {
            // once the simulation has finished clean up in case we are in a multi-run environment
            if (Writer != null)
            {
                Writer.Dispose();
                Writer = null;
            }
        }

        public bool RuntimeValidation(ref string error)
        {
            return true;
        }
    }
}
