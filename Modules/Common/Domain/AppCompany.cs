using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Domain
{
    public class AppCompany
    {
        private Guid _id;
        public Guid Id
        { 
            get { return _id; }
            set
            {
                if (value == Guid.Empty)
                    throw new ArgumentNullException(nameof(Id));
                _id = value;
            }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentNullException(nameof(Name));

                _name = value;
            }
        }


        public AppCompany(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        public void Validate()
        {
        }
    }
}
