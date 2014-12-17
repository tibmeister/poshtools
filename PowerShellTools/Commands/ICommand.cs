using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerShellTools.Commands
{
    public interface ICommand
    {
        CommandID CommandId { get; }
        void Execute(object sender, EventArgs args);
        void QueryStatus(object sender, EventArgs args);
    }
}
