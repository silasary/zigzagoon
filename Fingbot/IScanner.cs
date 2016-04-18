using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Fingbot
{
    interface IScanner
    {
        void Refresh();
        void Merge(Host host, XElement data);
        bool IsValidTool();
    }
}
