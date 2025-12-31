using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExporterCommon.Domain
{
    public class AppGenre : BaseEntity
    {
        private Guid id;
        private string name;

        public Guid Id
        {
            get { return id; }
            set
            {
                if (value == Guid.Empty)
                    throw new ArgumentNullException(nameof(Id));
                id = value;
            }
        }

        public string Name
        {
            get { return name; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentNullException(nameof(Name));

                name = value;
            }
        }

        public AppGenre() { }

        public AppGenre(Guid id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
