using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterCommon.Domain
{
    public interface IBaseEntity
    {
        void Validate();
    }

    public abstract class BaseEntity : IBaseEntity
    {
        public virtual void Validate() { }
    }
}
