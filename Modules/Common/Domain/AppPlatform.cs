using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Domain
{
    public class AppPlatform : BaseEntity
    {
        private Guid id;
        private string name;
        private string specificationId;
        private string icon;
        private string cover;
        private string background;

        public Guid Id
        {
            get 
            { 
                return id; 
            }
            set
            {
                if (value == Guid.Empty)
                    throw new ArgumentNullException(nameof(Id));
                id = value;
            }
        }

        public string Name
        {
            get 
            { 
                return name; 
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentNullException(nameof(Name));

                name = value;
            }
        }

        public string SpecificationId
        {
            get
            {
                return specificationId;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentNullException(nameof(SpecificationId));
                specificationId = value;
            }
        }

        public string Icon
        {
            get
            {
                return icon;
            }
            set
            {
                icon = value;
            }
        }

        public string Cover
        {
            get
            {
                return cover;
            }
            set
            {
                cover = value;
            }
        }

        public string Background
        {
            get
            {
                return background;
            }
            set
            {
                background = value;
            }
        }

        public AppPlatform()
        {
        }
    }
}
