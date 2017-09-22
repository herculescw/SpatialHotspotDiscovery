using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using QuickGraph.Graphviz.Dot;

namespace QuickGraph.Graphviz
{
    /// <summary>
    /// Default dot engine implementation, writes dot code to disk
    /// </summary>
    public sealed class FileDotEngine : IDotEngine
    {
        public string Run(GraphvizImageType imageType, string dot, string outputFileName)
        {
            string output = outputFileName + ".dot";
            File.WriteAllText(output, dot);
            return output;
        }
    }
}
