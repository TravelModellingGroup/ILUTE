using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMG.Ilute.Data;
using TMG.Ilute.Data.Housing;
using TMG.Ilute.Model.Utilities;
using TMG.Input;
using XTMF;

namespace TMG.Ilute.Model.Housing.Validation
{
    public sealed class SaveAvgDwellingPriceByZone : IExecuteYearly, IDisposable
    {
        public string Name { get; set; }

        public float Progress => throw new NotImplementedException();

        public Tuple<byte, byte, byte> ProgressColour => throw new NotImplementedException();

        [SubModelInformation(Required = true, Description = "The dwelling repository")]
        public IDataSource<Repository<Dwelling>> Dwellings;

        [SubModelInformation(Required = true, Description = "Currency converter between years")]
        public IDataSource<CurrencyManager> CurrencyManager;

        [SubModelInformation(Required = true, Description = "The location to save the results to.")]
        public FileLocation SaveTo;

        private StreamWriter _writer;

        public void AfterYearlyExecute(int currentYear)
        {
        }

        public void BeforeFirstYear(int firstYear)
        {
            _writer = new StreamWriter(SaveTo);
            _writer.WriteLine("Year,Zone,NumberOfDwellings,AvgPrice,MinPrice,MaxPrice");
        }

        public void BeforeYearlyExecute(int currentYear)
        {
        }

        public void Execute(int currentYear)
        {
            var currencyManager = Repository.GetRepository(CurrencyManager);
            foreach(var zoneData in from dwelling in Repository.GetRepository(Dwellings)
                                    group dwelling by dwelling.Zone into g
                                    orderby g.Key ascending
                                    select new
                                    {
                                        Zone = g.Key,
                                        AvgPrice = g.Average(d => currencyManager.ConvertToYear(d.Value, new Date(currentYear, 0)).Amount),
                                        NumberOfDwellings = g.Count(),
                                        MinPrice = g.Min(d => currencyManager.ConvertToYear(d.Value, new Date(currentYear, 0)).Amount),
                                        MaxPrice = g.Max(d => currencyManager.ConvertToYear(d.Value, new Date(currentYear, 0)).Amount)
                                    })
            {
                _writer.WriteLine($"{currentYear},{zoneData.Zone},{zoneData.NumberOfDwellings},{zoneData.AvgPrice},{zoneData.MinPrice},{zoneData.MaxPrice}");
            }
        }

        public void RunFinished(int finalYear)
        {
        }

        public bool RuntimeValidation(ref string error)
        {
            return true;
        }

        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                _writer?.Dispose();
                _writer = null;
                disposedValue = true;
            }
        }

         ~SaveAvgDwellingPriceByZone()
        {
           Dispose(false);
         }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
