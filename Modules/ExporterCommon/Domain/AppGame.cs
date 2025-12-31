using ExporterCommon.Error;
using System;
using System.Collections.Generic;

namespace ExporterCommon.Domain
{
    public class AppGame : BaseEntity
    {
        private Guid id;
        private string name;
        private List<AppPlatform> platforms = new List<AppPlatform>();
        private List<AppGenre> genres = new List<AppGenre>();
        private List<AppCompany> developers = new List<AppCompany>();
        private List<AppCompany> publishers = new List<AppCompany>();
        private DateTime? releaseDate = null;
        private ulong playtime = 0;
        private DateTime? lastActivity = null;
        private DateTime? added = null;
        private string installDirectory = null;
        private bool isInstalled = false;
        private string backgroundImage = null;
        private string coverImage = null;
        private string icon = null;
        private string description = null;
        private bool isHidden = false;
        private AppCompletionStatus appCompletionStatus;
        private string contentHash;

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
            get { return name; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentNullException(nameof(Name));

                name = value;
            }
        }

        public List<AppPlatform> Platforms
        {
            get
            {
                return platforms;
            }
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(Platforms));
                platforms = value;
            }
        }

        public List<AppGenre> Genres
        {
            get
            {
                return genres;
            }
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(Genres));
                genres = value;
            }
        }

        public List<AppCompany> Developers
        {
            get
            {
                return developers;
            }
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(Developers));
                developers = value;
            }
        }

        public List<AppCompany> Publishers
        {
            get
            {
                return publishers;
            }
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(Publishers));
                publishers = value;
            }
        }

        public DateTime? ReleaseDate
        {
            get
            {
                return releaseDate;
            }
            set
            {
                releaseDate = value;
            }
        }

        public ulong Playtime
        {
            get
            {
                return playtime;
            }
            set
            {
                if(value < 0)
                    throw new ArgumentOutOfRangeException($"Value given to {nameof(Playtime)} must be a positive integer");
                playtime = value;
            }
        }
        public DateTime? LastActivity
        {
            get
            {
                return lastActivity;
            }
            set
            {
                lastActivity = value;
            }
        }

        public DateTime? Added
        {
            get
            {
                return added;
            }
            set
            {
                added = value;
            }
        }

        public string InstallDirectory
        {
            get
            {
                return installDirectory;
            }
            set
            {
                if(value != null && value == string.Empty)
                {
                    throw new ArgumentException($"Value provided to {nameof(InstallDirectory)} must be either null or a non-empty string");
                }
                installDirectory = value;
            }
        }

        public bool IsInstalled
        {
            get
            {
                return isInstalled;
            }
            set
            {
                isInstalled = value;
            }
        }

        public string BackgroundImage
        {
            get
            {
                return backgroundImage;
            }
            set
            {
                if (value != null && value == string.Empty)
                {
                    throw new ArgumentException($"Value provided to {nameof(BackgroundImage)} must be either null or a non-empty string");
                }
                backgroundImage = value;
            }
        }

        public string CoverImage
        {
            get
            {
                return coverImage;
            }
            set
            {
                if (value != null && value == string.Empty)
                {
                    throw new ArgumentException($"Value provided to {nameof(CoverImage)} must be either null or a non-empty string");
                }
                coverImage = value;
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
                if (value != null && value == string.Empty)
                {
                    throw new ArgumentException($"Value provided to {nameof(Icon)} must be either null or a non-empty string");
                }
                icon = value;
            }
        }

        public string Description
        {
            get
            {
                return description;
            }
            set
            {
                description = value;
            }
        }

        public bool IsHidden
        {
            get
            {
                return isHidden;
            }
            set
            {
                isHidden = value;
            }
        }

        public AppCompletionStatus CompletionStatus
        {
            get
            {
                return appCompletionStatus;
            }
            set
            {
                if (appCompletionStatus is null)
                    throw new ArgumentNullException(nameof(CompletionStatus));
                appCompletionStatus = value;
            }
        }

        public string ContentHash
        {
            get
            {
                return contentHash;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentNullException(nameof(ContentHash));
                contentHash = value;
            }
        }

        public AppGame() { }

        public override void Validate()
        {
            if(!IsInstalled && string.IsNullOrEmpty(InstallDirectory))
            {
                throw new InvalidStateException($"{nameof(InstallDirectory)} must not be empty when game is intalled");
            }
        }

        public void Install()
        {
            isInstalled = true;
        }

        public void Uninstall()
        {
            isInstalled = false;
        }

        public void Hide()
        {
            isHidden = true;
        }

        public void Unhide()
        {
            isHidden = false;
        }
    }
}
