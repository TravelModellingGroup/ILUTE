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
    [ModuleInformation(Description = "This is the base model system template for Ilute.")]
    public class MultiYearTravelDemandModel : ITravelDemandModel, IResourceSource
    {
        [RunParameter("Input Base Directory", "../../Input", "The directory were the input files are located.")]
        public string InputBaseDirectory { get; set; }

        public string Name { get; set; }

        [SubModelInformation(Description = "Network Information")]
        public IList<INetworkData> NetworkData { get; set; }

        public string OutputBaseDirectory { get; set; }

        private volatile bool _exit = false;

        [RunParameter("Start Year", 1986, "The first year of the simulation.")]
        public int StartYear;

        [RunParameter("Number of Years", 20, "The number of years to execute for.")]
        public int NumberOfYears;

        public float Progress { get; set; }

        public Tuple<byte, byte, byte> ProgressColour => new Tuple<byte, byte, byte>(50, 150, 50);

        [SubModelInformation(Required = true, Description = "The zone system the model will use.")]
        public IZoneSystem ZoneSystem { get; set; }

        [SubModelInformation(Description = "Model Data Storage")]
        public List<IResource> Resources { get; set; }


        public bool ExitRequest()
        {
            _exit = true;
            return true;
        }

        public bool RuntimeValidation(ref string error)
        {
            if (NumberOfYears <= 0)
            {
                error = "In '" + Name + "' the number of years to execute the model for must be greater than zero!";
                return false;
            }
            return true;
        }

        [SubModelInformation(Description = "Execute before the main model run")]
        public ISelfContainedModule[] PreRun;

        [SubModelInformation(Description = "Models that get run every year")]
        public IExecuteYearly[] RunYearly;

        [SubModelInformation(Description = "Execute after the main model run")]
        public ISelfContainedModule[] PostRun;

        public void Start()
        {
            ZoneSystem.LoadData();
            for (int i = 0; i < PreRun.Length; i++)
            {
                _status = () => PreRun[i].ToString();
                PreRun[i].Start();
            }
            foreach (var model in RunYearly)
            {
                _status = () => model.ToString();
                model.BeforeFirstYear(StartYear);
            }
            for (int year = 0; year < NumberOfYears && !_exit; year++)
            {
                for (int i = 0; i < RunYearly.Length && !_exit; i++)
                {
                    _status = () => RunYearly[i].ToString();
                    RunYearly[i].BeforeYearlyExecute(StartYear + year);
                }
                for (int i = 0; i < RunYearly.Length && !_exit; i++)
                {
                    _status = () => (year + this.StartYear) + ": " + RunYearly[i].ToString();
                    Progress = (float)year / NumberOfYears + (1.0f / NumberOfYears) * ((float)i / RunYearly.Length);
                    RunYearly[i].Execute(StartYear + year);
                }
                for (int i = 0; i < RunYearly.Length && !_exit; i++)
                {
                    _status = () => RunYearly[i].ToString();
                    RunYearly[i].AfterYearlyExecute(StartYear + year);
                }
            }
            foreach (var model in RunYearly)
            {
                _status = () => model.ToString();
                model.RunFinished(StartYear + NumberOfYears - 1);
            }
            for (int i = 0; i < PostRun.Length; i++)
            {
                _status = () => PostRun[i].ToString();
                PostRun[i].Start();
            }
            ZoneSystem.UnloadData();
        }

        private Func<string> _status;

        public override string ToString()
        {
            var s = _status;
            if (s != null) return s();
            return base.ToString();
        }
    }
}
