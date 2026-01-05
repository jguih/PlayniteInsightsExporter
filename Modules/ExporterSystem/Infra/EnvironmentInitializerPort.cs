using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterSystem.Infra
{
    public interface IEnvironmentInitializerPort
    {
        void EnsureDirectories();
        void EnsureIdentity();
    }
}
