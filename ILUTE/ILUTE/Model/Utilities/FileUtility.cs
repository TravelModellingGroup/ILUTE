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
using Datastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMG.Ilute.Model.Utilities
{
    /// <summary>
    /// This class contains helper methods for loading in data for
    /// the different models.
    /// </summary>
    internal static class FileUtility
    {
        /// <summary>
        /// Load the data from each given text file and load in all of the data as a
        /// separate data point in the array.
        /// </summary>
        /// <param name="fileName">The name of the file to load in</param>
        /// <param name="header">Does the file contain a header?</param>
        /// <returns>Each column converted into an integer</returns>
        internal static int[] LoadAllDataToInt(string fileName, bool header)
        {
            using (var reader = new CsvReader(fileName, true))
            {
                int columns;
                var data = new List<int>();
                while (reader.LoadLine(out columns))
                {
                    for (int i = 0; i < columns; i++)
                    {
                        int temp;
                        reader.Get(out temp, i);
                        data.Add(temp);
                    }
                }
                return data.ToArray();
            }
        }

        /// <summary>
        /// Load the data from each given text file and load in all of the data as a
        /// separate data point in the array.
        /// </summary>
        /// <param name="fileName">The name of the file to load in</param>
        /// <param name="header">Does the file contain a header?</param>
        /// <returns>Each column converted into an float</returns>
        internal static float[] LoadAllDataToFloat(string fileName, bool header)
        {
            using (var reader = new CsvReader(fileName, true))
            {
                int columns;
                var data = new List<float>();
                while (reader.LoadLine(out columns))
                {
                    for (int i = 0; i < columns; i++)
                    {
                        float temp;
                        reader.Get(out temp, i);
                        data.Add(temp);
                    }
                }
                return data.ToArray();
            }
        }
    }
}
