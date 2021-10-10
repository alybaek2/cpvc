using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public interface ITextFile
    {
        void WriteLine(string line);
        string ReadLine();
    }
}
