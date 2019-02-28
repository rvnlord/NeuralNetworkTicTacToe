using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreLinq;
using TipTacToe.Common;
using TipTacToe.Common.UtilityClasses;

namespace TipTacToe.Models
{
    public class TrainSet : CustomList<TrainRow>
    {
        public double Accuracy { get; set; }

        public TrainSet(double[][] inputs, double[][] outputs)
        {
            if (inputs.Length != outputs.Length)
                throw new Exception("Liczba wejść nie pasuje do liczby wyjść.");
            for (var i = 0; i < inputs.Length; i++)
                _customList.Add(new TrainRow(inputs[i], outputs[i]));
        }

        public TrainSet() { }

        public TrainSet(IEnumerable<TrainRow> trainRows)
        {
            _customList = trainRows.ToList();
        }
    }

    public class TrainRow
    {
        public double[] Inputs { get; set; }
        public double[] Outputs { get; set; }
        public double[] Predictions { get; set; }

        public TrainRow(double[] inputs, double[] outputs)
        {
            Inputs = inputs;
            Outputs = outputs;
        }
    }
}
