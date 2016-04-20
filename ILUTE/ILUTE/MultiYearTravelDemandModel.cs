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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMG;
using XTMF;

namespace TMG.Ilute
{
    public class MultiYearTravelDemandModel : ITravelDemandModel, IResourceSource
    {
        public string InputBaseDirectory { get; set; }

        public string Name { get; set; }

        [SubModelInformation(Description = "Network Information")]
        public IList<INetworkData> NetworkData { get; set; }

        public string OutputBaseDirectory { get; set; }

        private volatile bool _Exit = false;

        public int StartYear;

        public int NumberOfYears;

        public float Progress { get; set; }


        public Tuple<byte, byte, byte> ProgressColour
        {
            get
            {
                return new Tuple<byte, byte, byte>(50, 150, 50);
            }
        }

        [SubModelInformation(Required = true, Description = "The zone system the model will use.")]
        public IZoneSystem ZoneSystem { get; set; }

        [SubModelInformation(Description = "Model Data Storage")]
        public List<IResource> Resources { get; set; }
        

        public bool ExitRequest()
        {
            return true;
        }

        public bool RuntimeValidation(ref string error)
        {
            return true;
        }

        [SubModelInformation(Description = "Models that get run every year")]
        public IExecuteYearly[] RunYearly;

        public void Start()
        {
            for (int year = 0; year < NumberOfYears && !_Exit; year++)
            {
                for (int i = 0; i < RunYearly.Length && !_Exit; i++)
                {
                    RunYearly[i].Execute(StartYear + year);
                }
            }
        }
    }
}
